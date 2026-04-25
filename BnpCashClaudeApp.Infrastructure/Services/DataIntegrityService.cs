using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Entities.SecuritySubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// پیاده‌سازی سرویس بررسی صحت داده‌های ذخیره شده
    /// پیاده‌سازی الزام FDP_SDI.2.1 و FDP_SDI.2.2 از استاندارد ISO 15408
    /// </summary>
    public class DataIntegrityService : IDataIntegrityService
    {
        private readonly IConfiguration _configuration;
        private readonly IAuditLogService _auditLogService;
        private readonly ICryptographicAlgorithmPolicyService _cryptographicAlgorithmPolicyService;
        private readonly ILogger<DataIntegrityService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly byte[] _integrityKey;

        public DataIntegrityService(
            IConfiguration configuration,
            IAuditLogService auditLogService,
            ICryptographicAlgorithmPolicyService cryptographicAlgorithmPolicyService,
            ILogger<DataIntegrityService> logger,
            IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _auditLogService = auditLogService;
            _cryptographicAlgorithmPolicyService = cryptographicAlgorithmPolicyService;
            _logger = logger;
            _serviceProvider = serviceProvider;

            // بارگذاری کلید Integrity از Configuration
            // در Production باید از Key Vault یا HSM استفاده شود
            var keyString = _configuration["Security:IntegrityKey"];
            if (string.IsNullOrEmpty(keyString))
            {
                _logger.LogWarning("IntegrityKey not found in configuration. Generating a new one.");
                _integrityKey = GenerateTemporaryIntegrityKey();
                _logger.LogWarning("Generated temporary IntegrityKey. Please configure Security:IntegrityKey in appsettings.json");
            }
            else
            {
                try
                {
                    _integrityKey = Convert.FromBase64String(keyString);
                    _cryptographicAlgorithmPolicyService.ValidateIntegrityKey(_integrityKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "IntegrityKey is invalid or violates policy. Generating a new temporary key.");
                    _integrityKey = GenerateTemporaryIntegrityKey();
                }
            }
        }
        /// <summary>
        /// محاسبه Integrity Hash با استفاده از HMAC-SHA256
        /// </summary>
        public string ComputeIntegrityHash(object entity, string[] sensitiveFields)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (sensitiveFields == null || sensitiveFields.Length == 0)
                return string.Empty;

            try
            {
                // استخراج مقادیر فیلدهای حساس
                var fieldValues = new Dictionary<string, object?>();
                var entityType = entity.GetType();

                foreach (var fieldName in sensitiveFields)
                {
                    var property = entityType.GetProperty(fieldName, 
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    
                    if (property != null && property.CanRead)
                    {
                        var value = property.GetValue(entity);
                        // تبدیل null به string.Empty برای ثبات در Hash
                        fieldValues[fieldName] = value ?? string.Empty;
                    }
                }

                // اگر هیچ فیلدی پیدا نشد، Hash خالی برمی‌گردانیم
                if (fieldValues.Count == 0)
                    return string.Empty;

                // تبدیل به JSON برای محاسبه Hash (با ترتیب الفبایی برای ثبات)
                var json = JsonSerializer.Serialize(fieldValues.OrderBy(kv => kv.Key));
                var jsonBytes = Encoding.UTF8.GetBytes(json);

                // محاسبه HMAC-SHA256
                using var hmac = _cryptographicAlgorithmPolicyService.CreateIntegrityHmac(_integrityKey);
                var hashBytes = hmac.ComputeHash(jsonBytes);
                return Convert.ToBase64String(hashBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در محاسبه Integrity Hash برای Entity: {EntityType}", entity.GetType().Name);
                throw;
            }
        }

        /// <summary>
        /// بررسی صحت Integrity Hash
        /// </summary>
        public bool VerifyIntegrityHash(object entity, string storedHash, string[] sensitiveFields)
        {
            if (entity == null || string.IsNullOrEmpty(storedHash))
                return false;

            try
            {
                var computedHash = ComputeIntegrityHash(entity, sensitiveFields);
                
                // استفاده از Constant-time comparison برای جلوگیری از Timing Attack
                return ConstantTimeEquals(computedHash, storedHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در بررسی Integrity Hash");
                return false;
            }
        }

        /// <summary>
        /// Constant-time string comparison برای جلوگیری از Timing Attack
        /// استفاده از CryptographicOperations.FixedTimeEquals برای امنیت بیشتر
        /// </summary>
        private static bool ConstantTimeEquals(string a, string b)
        {
            if (a == null && b == null)
                return true;
            
            if (a == null || b == null)
                return false;

            // تبدیل به byte array برای استفاده از CryptographicOperations.FixedTimeEquals
            var aBytes = Encoding.UTF8.GetBytes(a);
            var bBytes = Encoding.UTF8.GetBytes(b);

            // اگر طول متفاوت باشد، باز هم مقایسه کامل انجام می‌شود
            // برای جلوگیری از Timing Attack
            if (aBytes.Length != bBytes.Length)
            {
                // مقایسه با آرایه هم‌طول برای جلوگیری از Timing Attack
                var paddedB = new byte[aBytes.Length];
                Array.Copy(bBytes, paddedB, Math.Min(bBytes.Length, aBytes.Length));
                CryptographicOperations.FixedTimeEquals(aBytes, paddedB);
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }

        /// <summary>
        /// بررسی صحت تمام Entityهای حساس
        /// </summary>
        public async Task<DataIntegrityVerificationResult> VerifyAllEntitiesIntegrityAsync()
        {
            var result = new DataIntegrityVerificationResult
            {
                ViolationsByEntityType = new Dictionary<string, int>()
            };

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<NavigationDbContext>();

                _logger.LogInformation("شروع بررسی صحت تمام Entityهای حساس");

                // بررسی tblUser
                var users = await dbContext.tblUsers.ToListAsync();
                var userSensitiveFields = new[] { "UserName", "Password", "Email", "MobileNumber", "IsMfaEnabled", "MfaSecretKey" };
                var userViolations = 0;
                foreach (var user in users)
                {
                    if (!string.IsNullOrEmpty(user.IntegrityHash))
                    {
                        if (!VerifyIntegrityHash(user, user.IntegrityHash, userSensitiveFields))
                        {
                            userViolations++;
                            await LogIntegrityViolationAsync(
                                nameof(tblUser),
                                user.PublicId.ToString(),
                                $"Integrity hash mismatch for user: {user.UserName}");
                        }
                    }
                }
                if (userViolations > 0)
                {
                    result.ViolationsByEntityType[nameof(tblUser)] = userViolations;
                    result.TotalViolations += userViolations;
                }

                // بررسی SecuritySetting
                var securitySettings = await dbContext.SecuritySettings.ToListAsync();
                var settingSensitiveFields = new[] { "SettingKey", "SettingValue" };
                var settingViolations = 0;
                foreach (var setting in securitySettings)
                {
                    if (!string.IsNullOrEmpty(setting.IntegrityHash))
                    {
                        if (!VerifyIntegrityHash(setting, setting.IntegrityHash, settingSensitiveFields))
                        {
                            settingViolations++;
                            await LogIntegrityViolationAsync(
                                nameof(SecuritySetting),
                                setting.PublicId.ToString(),
                                $"Integrity hash mismatch for setting: {setting.SettingKey}");
                        }
                    }
                }
                if (settingViolations > 0)
                {
                    result.ViolationsByEntityType[nameof(SecuritySetting)] = settingViolations;
                    result.TotalViolations += settingViolations;
                }

                // بررسی CryptographicKeyEntity
                var cryptoKeys = await dbContext.CryptographicKeys.ToListAsync();
                var keySensitiveFields = new[] { "EncryptedKeyValue", "EncryptionIV", "Purpose" };
                var keyViolations = 0;
                foreach (var key in cryptoKeys)
                {
                    if (!string.IsNullOrEmpty(key.IntegrityHash))
                    {
                        if (!VerifyIntegrityHash(key, key.IntegrityHash, keySensitiveFields))
                        {
                            keyViolations++;
                            await LogIntegrityViolationAsync(
                                nameof(CryptographicKeyEntity),
                                key.PublicId.ToString(),
                                $"Integrity hash mismatch for key: {key.Purpose}");
                        }
                    }
                }
                if (keyViolations > 0)
                {
                    result.ViolationsByEntityType[nameof(CryptographicKeyEntity)] = keyViolations;
                    result.TotalViolations += keyViolations;
                }

                // بررسی tblPermission
                var permissions = await dbContext.tblPermissions.ToListAsync();
                var permissionSensitiveFields = new[] { "Name", "Resource", "Action" };
                var permissionViolations = 0;
                foreach (var permission in permissions)
                {
                    if (!string.IsNullOrEmpty(permission.IntegrityHash))
                    {
                        if (!VerifyIntegrityHash(permission, permission.IntegrityHash, permissionSensitiveFields))
                        {
                            permissionViolations++;
                            await LogIntegrityViolationAsync(
                                nameof(tblPermission),
                                permission.PublicId.ToString(),
                                $"Integrity hash mismatch for permission: {permission.Name}");
                        }
                    }
                }
                if (permissionViolations > 0)
                {
                    result.ViolationsByEntityType[nameof(tblPermission)] = permissionViolations;
                    result.TotalViolations += permissionViolations;
                }

                // بررسی PasswordHistory
                var passwordHistories = await dbContext.PasswordHistories.ToListAsync();
                var passwordHistorySensitiveFields = new[] { "PasswordHash", "UserId" };
                var passwordHistoryViolations = 0;
                foreach (var history in passwordHistories)
                {
                    if (!string.IsNullOrEmpty(history.IntegrityHash))
                    {
                        if (!VerifyIntegrityHash(history, history.IntegrityHash, passwordHistorySensitiveFields))
                        {
                            passwordHistoryViolations++;
                            await LogIntegrityViolationAsync(
                                nameof(PasswordHistory),
                                history.PublicId.ToString(),
                                $"Integrity hash mismatch for password history");
                        }
                    }
                }
                if (passwordHistoryViolations > 0)
                {
                    result.ViolationsByEntityType[nameof(PasswordHistory)] = passwordHistoryViolations;
                    result.TotalViolations += passwordHistoryViolations;
                }

                _logger.LogInformation(
                    "بررسی صحت تمام Entityها انجام شد. تعداد کل معیوب: {TotalCount}. تفکیک: {Breakdown}",
                    result.TotalViolations,
                    result.ViolationsByEntityType.Count > 0 
                        ? string.Join(", ", result.ViolationsByEntityType.Select(kv => $"{kv.Key}: {kv.Value}"))
                        : "هیچ نقضی شناسایی نشد");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در بررسی صحت Entityها");
            }

            return result;
        }

        /// <summary>
        /// ثبت رویداد Integrity Violation
        /// </summary>
        public async Task LogIntegrityViolationAsync(string entityType, string entityId, string reason)
        {
            await _auditLogService.LogEventAsync(
                eventType: "IntegrityViolation",
                entityType: entityType,
                entityId: entityId,
                isSuccess: false,
                description: $"Integrity violation detected: {reason}",
                ct: default);

            _logger.LogWarning(
                "Integrity violation detected - EntityType: {EntityType}, EntityId: {EntityId}, Reason: {Reason}",
                entityType, entityId, reason);
        }

        /// <summary>
        /// محاسبه و به‌روزرسانی Integrity Hash برای تمام Entityهای موجود
        /// این متد برای داده‌های قدیمی که Hash ندارند استفاده می‌شود
        /// </summary>
        public async Task<int> ComputeHashForExistingEntitiesAsync()
        {
            int updatedCount = 0;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<NavigationDbContext>();

                _logger.LogInformation("شروع محاسبه Integrity Hash برای Entityهای موجود");

                // به‌روزرسانی tblUser
                var users = await dbContext.tblUsers.Where(u => u.IntegrityHash == null || u.IntegrityHash == "").ToListAsync();
                var userSensitiveFields = new[] { "UserName", "Password", "Email", "MobileNumber", "IsMfaEnabled", "MfaSecretKey" };
                foreach (var user in users)
                {
                    user.IntegrityHash = ComputeIntegrityHash(user, userSensitiveFields);
                    updatedCount++;
                }

                // به‌روزرسانی SecuritySetting
                var securitySettings = await dbContext.SecuritySettings.Where(s => s.IntegrityHash == null || s.IntegrityHash == "").ToListAsync();
                var settingSensitiveFields = new[] { "SettingKey", "SettingValue" };
                foreach (var setting in securitySettings)
                {
                    setting.IntegrityHash = ComputeIntegrityHash(setting, settingSensitiveFields);
                    updatedCount++;
                }

                // به‌روزرسانی CryptographicKeyEntity
                var cryptoKeys = await dbContext.CryptographicKeys.Where(k => k.IntegrityHash == null || k.IntegrityHash == "").ToListAsync();
                var keySensitiveFields = new[] { "EncryptedKeyValue", "EncryptionIV", "Purpose" };
                foreach (var key in cryptoKeys)
                {
                    key.IntegrityHash = ComputeIntegrityHash(key, keySensitiveFields);
                    updatedCount++;
                }

                // به‌روزرسانی tblPermission
                var permissions = await dbContext.tblPermissions.Where(p => p.IntegrityHash == null || p.IntegrityHash == "").ToListAsync();
                var permissionSensitiveFields = new[] { "Name", "Resource", "Action" };
                foreach (var permission in permissions)
                {
                    permission.IntegrityHash = ComputeIntegrityHash(permission, permissionSensitiveFields);
                    updatedCount++;
                }

                // به‌روزرسانی PasswordHistory
                var passwordHistories = await dbContext.PasswordHistories.Where(h => h.IntegrityHash == null || h.IntegrityHash == "").ToListAsync();
                var passwordHistorySensitiveFields = new[] { "PasswordHash", "UserId" };
                foreach (var history in passwordHistories)
                {
                    history.IntegrityHash = ComputeIntegrityHash(history, passwordHistorySensitiveFields);
                    updatedCount++;
                }

                // ذخیره تغییرات بدون محاسبه مجدد Hash (چون قبلاً محاسبه شده)
                await dbContext.Database.ExecuteSqlRawAsync("SET CONTEXT_INFO 0x01"); // Flag برای Skip کردن محاسبه مجدد
                await dbContext.SaveChangesAsync();
                await dbContext.Database.ExecuteSqlRawAsync("SET CONTEXT_INFO 0x00");

                _logger.LogInformation("محاسبه Integrity Hash برای {Count} Entity انجام شد", updatedCount);

                await _auditLogService.LogEventAsync(
                    eventType: "IntegrityHashComputed",
                    entityType: "Multiple",
                    isSuccess: true,
                    description: $"Computed integrity hash for {updatedCount} existing entities");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در محاسبه Integrity Hash برای Entityهای موجود");
            }

            return updatedCount;
        }

        /// <summary>
        /// تولید کلید Integrity امن برای استفاده در Configuration
        /// </summary>
        public string GenerateSecureIntegrityKey()
        {
            using var rng = RandomNumberGenerator.Create();
            var keyBytes = new byte[64]; // 512 bits
            rng.GetBytes(keyBytes);
            return Convert.ToBase64String(keyBytes);
        }
        private byte[] GenerateTemporaryIntegrityKey()
        {
            using var rng = RandomNumberGenerator.Create();
            var keyBytes = new byte[64]; // 512 bits
            rng.GetBytes(keyBytes);
            _cryptographicAlgorithmPolicyService.ValidateIntegrityKey(keyBytes);
            return keyBytes;
        }
    }
}


