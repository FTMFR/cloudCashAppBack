using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.SecuritySubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// سرویس مدیریت تنظیمات امنیتی از دیتابیس
    /// تنظیمات با Caching مدیریت می‌شوند برای کارایی بهتر
    /// </summary>
    public class SecuritySettingsService : ISecuritySettingsService
    {
        private readonly NavigationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<SecuritySettingsService> _logger;

        // ============================================
        // کلیدهای Cache و تنظیمات
        // ============================================
        private const string AccountLockoutCacheKey = "SecuritySettings_AccountLockout";
        private const string PasswordPolicyCacheKey = "SecuritySettings_PasswordPolicy";
        private const string CaptchaCacheKey = "SecuritySettings_Captcha";
        private const string MfaCacheKey = "SecuritySettings_Mfa";
        private const string ContextAccessControlCacheKey = "SecuritySettings_ContextAccessControl";
        private const string AuditLogProtectionCacheKey = "SecuritySettings_AuditLogProtection";
        private const string AccountLockoutSettingKey = "AccountLockout";
        private const string PasswordPolicySettingKey = "PasswordPolicy";
        private const string CaptchaSettingKey = "Captcha";
        private const string MfaSettingKey = "Mfa";
        private const string ContextAccessControlSettingKey = "ContextAccessControl";
        private const string AuditLogProtectionSettingKey = "AuditLogProtection";

        // مدت زمان Cache: 5 دقیقه
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public SecuritySettingsService(
            NavigationDbContext context,
            IMemoryCache cache,
            IAuditLogService auditLogService,
            ILogger<SecuritySettingsService> logger)
        {
            _context = context;
            _cache = cache;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// دریافت تنظیمات قفل حساب کاربری
        /// </summary>
        public async Task<AccountLockoutSettings> GetAccountLockoutSettingsAsync(CancellationToken ct = default)
        {
            // ============================================
            // ابتدا از Cache می‌خوانیم
            // ============================================
            if (_cache.TryGetValue(AccountLockoutCacheKey, out AccountLockoutSettings? cachedSettings) && cachedSettings != null)
            {
                _logger.LogDebug("AccountLockout settings loaded from cache");
                return cachedSettings;
            }

            // ============================================
            // خواندن از دیتابیس
            // ============================================
            var setting = await _context.SecuritySettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == AccountLockoutSettingKey && s.IsActive, ct);

            AccountLockoutSettings settings;

            if (setting != null && !string.IsNullOrWhiteSpace(setting.SettingValue))
            {
                try
                {
                    settings = JsonSerializer.Deserialize<AccountLockoutSettings>(setting.SettingValue)
                        ?? GetDefaultAccountLockoutSettings();

                    _logger.LogDebug("AccountLockout settings loaded from database");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing AccountLockout settings, using defaults");
                    settings = GetDefaultAccountLockoutSettings();
                }
            }
            else
            {
                _logger.LogWarning("AccountLockout settings not found in database, using defaults");
                settings = GetDefaultAccountLockoutSettings();
            }

            // ============================================
            // ذخیره در Cache
            // ============================================
            _cache.Set(AccountLockoutCacheKey, settings, CacheDuration);

            return settings;
        }

        /// <summary>
        /// دریافت تنظیمات سیاست رمز عبور
        /// </summary>
        public async Task<PasswordPolicySettings> GetPasswordPolicySettingsAsync(CancellationToken ct = default)
        {
            // ============================================
            // ابتدا از Cache می‌خوانیم
            // ============================================
            if (_cache.TryGetValue(PasswordPolicyCacheKey, out PasswordPolicySettings? cachedSettings) && cachedSettings != null)
            {
                _logger.LogDebug("PasswordPolicy settings loaded from cache");
                return cachedSettings;
            }

            // ============================================
            // خواندن از دیتابیس
            // ============================================
            var setting = await _context.SecuritySettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == PasswordPolicySettingKey && s.IsActive, ct);

            PasswordPolicySettings settings;

            if (setting != null && !string.IsNullOrWhiteSpace(setting.SettingValue))
            {
                try
                {
                    settings = JsonSerializer.Deserialize<PasswordPolicySettings>(setting.SettingValue)
                        ?? GetDefaultPasswordPolicySettings();

                    _logger.LogDebug("PasswordPolicy settings loaded from database");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing PasswordPolicy settings, using defaults");
                    settings = GetDefaultPasswordPolicySettings();
                }
            }
            else
            {
                _logger.LogWarning("PasswordPolicy settings not found in database, using defaults");
                settings = GetDefaultPasswordPolicySettings();
            }

            // ============================================
            // ذخیره در Cache
            // ============================================
            _cache.Set(PasswordPolicyCacheKey, settings, CacheDuration);

            return settings;
        }

        /// <summary>
        /// ذخیره تنظیمات قفل حساب کاربری
        /// </summary>
        public async Task SaveAccountLockoutSettingsAsync(
            AccountLockoutSettings settings,
            long userId,
            CancellationToken ct = default)
        {
            var settingJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var existingSetting = await _context.SecuritySettings
                .FirstOrDefaultAsync(s => s.SettingKey == AccountLockoutSettingKey, ct);

            if (existingSetting != null)
            {
                var oldValue = existingSetting.SettingValue;
                existingSetting.SettingValue = settingJson;
                existingSetting.SetZamanLastEdit(DateTime.Now);
                existingSetting.TblUserGrpIdLastEdit = userId;

                _logger.LogInformation("AccountLockout settings updated by user {UserId}", userId);

                // ثبت در Audit Log
                await _auditLogService.LogEntityChangeAsync(
                    eventType: "SecuritySettingsUpdated",
                    entityType: "SecuritySetting",
                    entityId: AccountLockoutSettingKey,
                    changes: new Dictionary<string, (object?, object?)>
                    {
                        { "SettingValue", (oldValue, settingJson) }
                    },
                    userId: userId,
                    ct: ct);
            }
            else
            {
                var newSetting = new SecuritySetting
                {
                    SettingKey = AccountLockoutSettingKey,
                    SettingName = "تنظیمات قفل حساب کاربری",
                    Description = "تنظیمات مربوط به قفل شدن حساب پس از تلاش‌های ناموفق ورود",
                    SettingValue = settingJson,
                    SettingType = SecuritySettingType.AccountLockout,
                    IsActive = true,
                    IsEditable = true,
                    DisplayOrder = 1,
                    TblUserGrpIdInsert = userId
                };
                newSetting.SetZamanInsert(DateTime.Now);

                _context.SecuritySettings.Add(newSetting);

                _logger.LogInformation("AccountLockout settings created by user {UserId}", userId);

                await _auditLogService.LogEventAsync(
                    eventType: "SecuritySettingsCreated",
                    entityType: "SecuritySetting",
                    entityId: AccountLockoutSettingKey,
                    isSuccess: true,
                    userId: userId,
                    description: "تنظیمات قفل حساب کاربری ایجاد شد",
                    ct: ct);
            }

            await _context.SaveChangesAsync(ct);

            // پاکسازی Cache
            InvalidateCache(AccountLockoutSettingKey);
        }

        /// <summary>
        /// ذخیره تنظیمات سیاست رمز عبور
        /// </summary>
        public async Task SavePasswordPolicySettingsAsync(
            PasswordPolicySettings settings,
            long userId,
            CancellationToken ct = default)
        {
            var settingJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var existingSetting = await _context.SecuritySettings
                .FirstOrDefaultAsync(s => s.SettingKey == PasswordPolicySettingKey, ct);

            if (existingSetting != null)
            {
                var oldValue = existingSetting.SettingValue;
                existingSetting.SettingValue = settingJson;
                existingSetting.SetZamanLastEdit(DateTime.Now);
                existingSetting.TblUserGrpIdLastEdit = userId;

                _logger.LogInformation("PasswordPolicy settings updated by user {UserId}", userId);

                await _auditLogService.LogEntityChangeAsync(
                    eventType: "SecuritySettingsUpdated",
                    entityType: "SecuritySetting",
                    entityId: PasswordPolicySettingKey,
                    changes: new Dictionary<string, (object?, object?)>
                    {
                        { "SettingValue", (oldValue, settingJson) }
                    },
                    userId: userId,
                    ct: ct);
            }
            else
            {
                var newSetting = new SecuritySetting
                {
                    SettingKey = PasswordPolicySettingKey,
                    SettingName = "تنظیمات سیاست رمز عبور",
                    Description = "تنظیمات مربوط به پیچیدگی و سیاست‌های رمز عبور",
                    SettingValue = settingJson,
                    SettingType = SecuritySettingType.PasswordPolicy,
                    IsActive = true,
                    IsEditable = true,
                    DisplayOrder = 2,
                    TblUserGrpIdInsert = userId
                };
                newSetting.SetZamanInsert(DateTime.Now);

                _context.SecuritySettings.Add(newSetting);

                _logger.LogInformation("PasswordPolicy settings created by user {UserId}", userId);

                await _auditLogService.LogEventAsync(
                    eventType: "SecuritySettingsCreated",
                    entityType: "SecuritySetting",
                    entityId: PasswordPolicySettingKey,
                    isSuccess: true,
                    userId: userId,
                    description: "تنظیمات سیاست رمز عبور ایجاد شد",
                    ct: ct);
            }

            await _context.SaveChangesAsync(ct);

            // پاکسازی Cache
            InvalidateCache(PasswordPolicySettingKey);
        }

        /// <summary>
        /// دریافت تنظیمات CAPTCHA
        /// همیشه از جدول SecuritySettings خوانده می‌شود (بدون کش)
        /// </summary>
        public async Task<CaptchaSettings> GetCaptchaSettingsAsync(CancellationToken ct = default)
        {
            // ============================================
            // خواندن مستقیم از دیتابیس (بدون کش)
            // ============================================
            var setting = await _context.SecuritySettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == CaptchaSettingKey && s.IsActive, ct);

            CaptchaSettings settings;

            if (setting != null && !string.IsNullOrWhiteSpace(setting.SettingValue))
            {
                try
                {
                    settings = JsonSerializer.Deserialize<CaptchaSettings>(setting.SettingValue)
                        ?? GetDefaultCaptchaSettings();

                    _logger.LogDebug("Captcha settings loaded from database (SecuritySettings table)");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing Captcha settings, using defaults");
                    settings = GetDefaultCaptchaSettings();
                }
            }
            else
            {
                _logger.LogWarning("Captcha settings not found in database, using defaults");
                settings = GetDefaultCaptchaSettings();
            }

            return settings;
        }

        /// <summary>
        /// ذخیره تنظیمات CAPTCHA
        /// </summary>
        public async Task SaveCaptchaSettingsAsync(
            CaptchaSettings settings,
            long userId,
            CancellationToken ct = default)
        {
            var settingJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var existingSetting = await _context.SecuritySettings
                .FirstOrDefaultAsync(s => s.SettingKey == CaptchaSettingKey, ct);

            if (existingSetting != null)
            {
                var oldValue = existingSetting.SettingValue;
                existingSetting.SettingValue = settingJson;
                existingSetting.SetZamanLastEdit(DateTime.Now);
                existingSetting.TblUserGrpIdLastEdit = userId;

                _logger.LogInformation("Captcha settings updated by user {UserId}", userId);

                await _auditLogService.LogEntityChangeAsync(
                    eventType: "SecuritySettingsUpdated",
                    entityType: "SecuritySetting",
                    entityId: CaptchaSettingKey,
                    changes: new Dictionary<string, (object?, object?)>
                    {
                        { "SettingValue", (oldValue, settingJson) }
                    },
                    userId: userId,
                    ct: ct);
            }
            else
            {
                var newSetting = new SecuritySetting
                {
                    SettingKey = CaptchaSettingKey,
                    SettingName = "تنظیمات CAPTCHA",
                    Description = "تنظیمات مربوط به کپچا برای احرازهویت چندگانه",
                    SettingValue = settingJson,
                    SettingType = SecuritySettingType.Captcha,
                    IsActive = true,
                    IsEditable = true,
                    DisplayOrder = 5,
                    TblUserGrpIdInsert = userId
                };
                newSetting.SetZamanInsert(DateTime.Now);

                _context.SecuritySettings.Add(newSetting);

                _logger.LogInformation("Captcha settings created by user {UserId}", userId);

                await _auditLogService.LogEventAsync(
                    eventType: "SecuritySettingsCreated",
                    entityType: "SecuritySetting",
                    entityId: CaptchaSettingKey,
                    isSuccess: true,
                    userId: userId,
                    description: "تنظیمات CAPTCHA ایجاد شد",
                    ct: ct);
            }

            await _context.SaveChangesAsync(ct);

            // پاکسازی Cache
            InvalidateCache(CaptchaSettingKey);
        }

        /// <summary>
        /// دریافت تنظیمات MFA
        /// همیشه از جدول SecuritySettings خوانده می‌شود (بدون کش)
        /// </summary>
        public async Task<MfaSettings> GetMfaSettingsAsync(CancellationToken ct = default)
        {
            // ============================================
            // خواندن مستقیم از دیتابیس (بدون کش)
            // ============================================
            var setting = await _context.SecuritySettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == MfaSettingKey && s.IsActive, ct);

            MfaSettings settings;

            if (setting != null && !string.IsNullOrWhiteSpace(setting.SettingValue))
            {
                try
                {
                    settings = JsonSerializer.Deserialize<MfaSettings>(setting.SettingValue)
                        ?? GetDefaultMfaSettings();

                    _logger.LogDebug("MFA settings loaded from database (SecuritySettings table)");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing MFA settings, using defaults");
                    settings = GetDefaultMfaSettings();
                }
            }
            else
            {
                _logger.LogWarning("MFA settings not found in database, using defaults");
                settings = GetDefaultMfaSettings();
            }

            return settings;
        }

        /// <summary>
        /// ذخیره تنظیمات MFA
        /// </summary>
        public async Task SaveMfaSettingsAsync(
            MfaSettings settings,
            long userId,
            CancellationToken ct = default)
        {
            var settingJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var existingSetting = await _context.SecuritySettings
                .FirstOrDefaultAsync(s => s.SettingKey == MfaSettingKey, ct);

            if (existingSetting != null)
            {
                var oldValue = existingSetting.SettingValue;
                existingSetting.SettingValue = settingJson;
                existingSetting.SetZamanLastEdit(DateTime.Now);
                existingSetting.TblUserGrpIdLastEdit = userId;

                _logger.LogInformation("MFA settings updated by user {UserId}", userId);

                await _auditLogService.LogEntityChangeAsync(
                    eventType: "SecuritySettingsUpdated",
                    entityType: "SecuritySetting",
                    entityId: MfaSettingKey,
                    changes: new Dictionary<string, (object?, object?)>
                    {
                        { "SettingValue", (oldValue, settingJson) }
                    },
                    userId: userId,
                    ct: ct);
            }
            else
            {
                var newSetting = new SecuritySetting
                {
                    SettingKey = MfaSettingKey,
                    SettingName = "تنظیمات احراز هویت دو مرحله‌ای",
                    Description = "تنظیمات مربوط به احراز هویت چندگانه (MFA)",
                    SettingValue = settingJson,
                    SettingType = SecuritySettingType.Mfa,
                    IsActive = true,
                    IsEditable = true,
                    DisplayOrder = 6,
                    TblUserGrpIdInsert = userId
                };
                newSetting.SetZamanInsert(DateTime.Now);

                _context.SecuritySettings.Add(newSetting);

                _logger.LogInformation("MFA settings created by user {UserId}", userId);

                await _auditLogService.LogEventAsync(
                    eventType: "SecuritySettingsCreated",
                    entityType: "SecuritySetting",
                    entityId: MfaSettingKey,
                    isSuccess: true,
                    userId: userId,
                    description: "تنظیمات احراز هویت دو مرحله‌ای ایجاد شد",
                    ct: ct);
            }

            await _context.SaveChangesAsync(ct);

            // پاکسازی Cache
            InvalidateCache(MfaSettingKey);
        }

        /// <summary>
        /// پاکسازی تمام Cache تنظیمات
        /// </summary>
        public void InvalidateCache()
        {
            _cache.Remove(AccountLockoutCacheKey);
            _cache.Remove(PasswordPolicyCacheKey);
            _cache.Remove(CaptchaCacheKey);
            _cache.Remove(MfaCacheKey);
            _cache.Remove(ContextAccessControlCacheKey);
            _cache.Remove(AuditLogProtectionCacheKey);
            _logger.LogDebug("All security settings cache invalidated");
        }

        /// <summary>
        /// پاکسازی Cache یک تنظیم خاص
        /// </summary>
        public void InvalidateCache(string settingKey)
        {
            switch (settingKey)
            {
                case AccountLockoutSettingKey:
                    _cache.Remove(AccountLockoutCacheKey);
                    break;
                case PasswordPolicySettingKey:
                    _cache.Remove(PasswordPolicyCacheKey);
                    break;
                case CaptchaSettingKey:
                    _cache.Remove(CaptchaCacheKey);
                    break;
                case MfaSettingKey:
                    _cache.Remove(MfaCacheKey);
                    break;
                case ContextAccessControlSettingKey:
                    _cache.Remove(ContextAccessControlCacheKey);
                    break;
                case AuditLogProtectionSettingKey:
                    _cache.Remove(AuditLogProtectionCacheKey);
                    break;
            }
            _logger.LogDebug("Security settings cache invalidated for key: {SettingKey}", settingKey);
        }

        /// <summary>
        /// دریافت مقدار یک تنظیم عددی
        /// </summary>
        public async Task<int> GetIntSettingAsync(string settingKey, int defaultValue, CancellationToken ct = default)
        {
            var cacheKey = $"SecuritySettings_Int_{settingKey}";

            // بررسی Cache
            if (_cache.TryGetValue(cacheKey, out int cachedValue))
            {
                return cachedValue;
            }

            // خواندن از دیتابیس
            var setting = await _context.SecuritySettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == settingKey && s.IsActive, ct);

            int value;
            if (setting != null && int.TryParse(setting.SettingValue, out int parsedValue))
            {
                value = parsedValue;
            }
            else
            {
                value = defaultValue;
            }

            // ذخیره در Cache
            _cache.Set(cacheKey, value, CacheDuration);

            return value;
        }

        /// <summary>
        /// ذخیره مقدار یک تنظیم عددی
        /// </summary>
        public async Task SaveIntSettingAsync(string settingKey, int value, long userId, CancellationToken ct = default)
        {
            var setting = await _context.SecuritySettings
                .FirstOrDefaultAsync(s => s.SettingKey == settingKey, ct);

            if (setting == null)
            {
                // ایجاد تنظیم جدید
                setting = new SecuritySetting
                {
                    SettingKey = settingKey,
                    SettingName = settingKey,
                    Description = $"تنظیم عددی: {settingKey}",
                    SettingValue = value.ToString(),
                    IsActive = true,
                    TblUserGrpIdInsert = userId
                };
                setting.SetZamanInsert(DateTime.Now);
                _context.SecuritySettings.Add(setting);
            }
            else
            {
                // به‌روزرسانی تنظیم موجود
                setting.SettingValue = value.ToString();
                setting.TblUserGrpIdLastEdit = userId;
                setting.SetZamanLastEdit(DateTime.Now);
            }

            await _context.SaveChangesAsync(ct);

            // پاکسازی Cache
            var cacheKey = $"SecuritySettings_Int_{settingKey}";
            _cache.Remove(cacheKey);

            // لاگ تغییرات
            await _auditLogService.LogEventAsync(
                eventType: "SecuritySettings",
                entityType: "SecuritySetting",
                entityId: setting.Id.ToString(),
                isSuccess: true,
                userId: userId,
                description: $"به‌روزرسانی تنظیم {settingKey} به مقدار {value}",
                ct: ct);
        }

        // ============================================
        // مقادیر پیش‌فرض
        // ============================================

        private static AccountLockoutSettings GetDefaultAccountLockoutSettings()
        {
            return new AccountLockoutSettings
            {
                MaxFailedAttempts = 5,
                LockoutDurationMinutes = 15,
                EnablePermanentLockout = false,
                PermanentLockoutThreshold = 10,
                FailedAttemptResetMinutes = 30
            };
        }

        private static PasswordPolicySettings GetDefaultPasswordPolicySettings()
        {
            return new PasswordPolicySettings
            {
                MinimumLength = 8,
                MaximumLength = 128,
                RequireUppercase = true,
                RequireLowercase = true,
                RequireDigit = true,
                RequireSpecialCharacter = true,
                SpecialCharacters = "!@#$%^&*()_+-=[]{}|;':\",./<>?",
                DisallowUsername = true,
                PasswordHistoryCount = 5,
                PasswordExpirationDays = 90
            };
        }

        private static CaptchaSettings GetDefaultCaptchaSettings()
        {
            return new CaptchaSettings
            {
                IsEnabled = false, // پیش‌فرض: کپچا غیرفعال
                CodeLength = 5,
                ExpiryMinutes = 2,
                NoiseLineCount = 10,
                NoiseDotCount = 50,
                ImageWidth = 130,
                ImageHeight = 40,
                RequireOnMfa = false
            };
        }

        private static MfaSettings GetDefaultMfaSettings()
        {
            return new MfaSettings
            {
                IsEnabled = true,
                IsRequired = false,
                OtpLength = 6,
                OtpExpirySeconds = 120,
                RecoveryCodesCount = 10,
                MaxFailedOtpAttempts = 3,
                LockoutDurationMinutes = 5
            };
        }

        /// <summary>
        /// دریافت تنظیمات کنترل دسترسی مبتنی بر Context
        /// الزام FDP_ACF.1.4 - عملیات کنترل دسترسی 4
        /// </summary>
        public async Task<ContextAccessControlSettings> GetContextAccessControlSettingsAsync(CancellationToken ct = default)
        {
            // ============================================
            // ابتدا از Cache می‌خوانیم
            // ============================================
            if (_cache.TryGetValue(ContextAccessControlCacheKey, out ContextAccessControlSettings? cachedSettings) && cachedSettings != null)
            {
                _logger.LogDebug("ContextAccessControl settings loaded from cache");
                return cachedSettings;
            }

            // ============================================
            // خواندن از دیتابیس
            // ============================================
            var setting = await _context.SecuritySettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == ContextAccessControlSettingKey && s.IsActive, ct);

            ContextAccessControlSettings settings;

            if (setting != null && !string.IsNullOrWhiteSpace(setting.SettingValue))
            {
                try
                {
                    settings = JsonSerializer.Deserialize<ContextAccessControlSettings>(setting.SettingValue)
                        ?? GetDefaultContextAccessControlSettings();

                    _logger.LogDebug("ContextAccessControl settings loaded from database");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing ContextAccessControl settings, using defaults");
                    settings = GetDefaultContextAccessControlSettings();
                }
            }
            else
            {
                _logger.LogWarning("ContextAccessControl settings not found in database, using defaults");
                settings = GetDefaultContextAccessControlSettings();
            }

            // ============================================
            // ذخیره در Cache
            // ============================================
            _cache.Set(ContextAccessControlCacheKey, settings, CacheDuration);

            return settings;
        }

        /// <summary>
        /// ذخیره تنظیمات کنترل دسترسی مبتنی بر Context
        /// </summary>
        public async Task SaveContextAccessControlSettingsAsync(
            ContextAccessControlSettings settings,
            long userId,
            CancellationToken ct = default)
        {
            var settingJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var existingSetting = await _context.SecuritySettings
                .FirstOrDefaultAsync(s => s.SettingKey == ContextAccessControlSettingKey, ct);

            if (existingSetting != null)
            {
                var oldValue = existingSetting.SettingValue;
                existingSetting.SettingValue = settingJson;
                existingSetting.SetZamanLastEdit(DateTime.Now);
                existingSetting.TblUserGrpIdLastEdit = userId;

                _logger.LogInformation("ContextAccessControl settings updated by user {UserId}", userId);

                await _auditLogService.LogEntityChangeAsync(
                    eventType: "SecuritySettingsUpdated",
                    entityType: "SecuritySetting",
                    entityId: ContextAccessControlSettingKey,
                    changes: new Dictionary<string, (object?, object?)>
                    {
                        { "SettingValue", (oldValue, settingJson) }
                    },
                    userId: userId,
                    ct: ct);
            }
            else
            {
                var newSetting = new SecuritySetting
                {
                    SettingKey = ContextAccessControlSettingKey,
                    SettingName = "تنظیمات کنترل دسترسی مبتنی بر Context",
                    Description = "تنظیمات مربوط به کنترل دسترسی بر اساس IP، زمان، مکان و دستگاه (FDP_ACF.1.4)",
                    SettingValue = settingJson,
                    SettingType = SecuritySettingType.ContextAccessControl,
                    IsActive = true,
                    IsEditable = true,
                    DisplayOrder = 7,
                    TblUserGrpIdInsert = userId
                };
                newSetting.SetZamanInsert(DateTime.Now);

                _context.SecuritySettings.Add(newSetting);

                _logger.LogInformation("ContextAccessControl settings created by user {UserId}", userId);

                await _auditLogService.LogEventAsync(
                    eventType: "SecuritySettingsCreated",
                    entityType: "SecuritySetting",
                    entityId: ContextAccessControlSettingKey,
                    isSuccess: true,
                    userId: userId,
                    description: "تنظیمات کنترل دسترسی مبتنی بر Context ایجاد شد",
                    ct: ct);
            }

            await _context.SaveChangesAsync(ct);

            // پاکسازی Cache
            InvalidateCache(ContextAccessControlSettingKey);
        }

        private static ContextAccessControlSettings GetDefaultContextAccessControlSettings()
        {
            return new ContextAccessControlSettings
            {
                IsEnabled = true,
                EnableIpRestriction = false,
                AllowedIpAddresses = new List<string>(),
                BlockedIpAddresses = new List<string>(),
                IpRestrictionMode = IpRestrictionMode.Blacklist,
                EnableTimeRestriction = false,
                AllowedStartTime = "00:00",
                AllowedEndTime = "23:59",
                AllowedDaysOfWeek = new List<int> { 0, 1, 2, 3, 4, 5, 6 },
                TimeZoneId = "Asia/Tehran",
                EnableGeoRestriction = false,
                AllowedCountries = new List<string> { "IR" },
                BlockedCountries = new List<string>(),
                EnableDeviceRestriction = false,
                AllowMobileDevices = true,
                AllowDesktopDevices = true,
                AllowTabletDevices = true,
                BlockedUserAgentPatterns = new List<string>(),
                EnableConcurrentSessionLimit = true,
                MaxConcurrentSessions = 3,
                ConcurrentSessionAction = ConcurrentSessionAction.DenyNew,
                EnableRiskAssessment = false,
                MaxAllowedRiskScore = 70,
                RequireMfaOnHighRisk = true,
                MfaRequiredRiskThreshold = 50,
                LogDeniedAccess = true,
                LogAllowedAccess = false,
                AlertOnSuspiciousAccess = true
            };
        }

        /// <summary>
        /// دریافت تنظیمات حفاظت از داده‌های ممیزی
        /// الزامات FAU_STG.3.1 و FAU_STG.4.1
        /// </summary>
        public async Task<AuditLogProtectionSettings> GetAuditLogProtectionSettingsAsync(CancellationToken ct = default)
        {
            // ============================================
            // ابتدا از Cache می‌خوانیم
            // ============================================
            if (_cache.TryGetValue(AuditLogProtectionCacheKey, out AuditLogProtectionSettings? cachedSettings) && cachedSettings != null)
            {
                _logger.LogDebug("AuditLogProtection settings loaded from cache");
                return cachedSettings;
            }

            // ============================================
            // خواندن از دیتابیس
            // ============================================
            var setting = await _context.SecuritySettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == AuditLogProtectionSettingKey && s.IsActive, ct);

            AuditLogProtectionSettings settings;

            if (setting != null && !string.IsNullOrWhiteSpace(setting.SettingValue))
            {
                try
                {
                    settings = JsonSerializer.Deserialize<AuditLogProtectionSettings>(setting.SettingValue)
                        ?? GetDefaultAuditLogProtectionSettings();

                    _logger.LogDebug("AuditLogProtection settings loaded from database");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing AuditLogProtection settings, using defaults");
                    settings = GetDefaultAuditLogProtectionSettings();
                }
            }
            else
            {
                _logger.LogWarning("AuditLogProtection settings not found in database, using defaults");
                settings = GetDefaultAuditLogProtectionSettings();
            }

            // ============================================
            // ذخیره در Cache
            // ============================================
            _cache.Set(AuditLogProtectionCacheKey, settings, CacheDuration);

            return settings;
        }

        /// <summary>
        /// ذخیره تنظیمات حفاظت از داده‌های ممیزی
        /// </summary>
        public async Task SaveAuditLogProtectionSettingsAsync(
            AuditLogProtectionSettings settings,
            long userId,
            CancellationToken ct = default)
        {
            var settingJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var existingSetting = await _context.SecuritySettings
                .FirstOrDefaultAsync(s => s.SettingKey == AuditLogProtectionSettingKey, ct);

            if (existingSetting != null)
            {
                var oldValue = existingSetting.SettingValue;
                existingSetting.SettingValue = settingJson;
                existingSetting.SetZamanLastEdit(DateTime.Now);
                existingSetting.TblUserGrpIdLastEdit = userId;

                _logger.LogInformation("[FAU_STG] AuditLogProtection settings updated by user {UserId}", userId);

                await _auditLogService.LogEntityChangeAsync(
                    eventType: "SecuritySettingsUpdated",
                    entityType: "SecuritySetting",
                    entityId: AuditLogProtectionSettingKey,
                    changes: new Dictionary<string, (object?, object?)>
                    {
                        { "SettingValue", (oldValue, settingJson) }
                    },
                    userId: userId,
                    ct: ct);
            }
            else
            {
                var newSetting = new SecuritySetting
                {
                    SettingKey = AuditLogProtectionSettingKey,
                    SettingName = "تنظیمات حفاظت از داده‌های ممیزی",
                    Description = "تنظیمات مربوط به پشتیبان‌گیری، آرشیو و حفاظت از داده‌های ممیزی (FAU_STG.3.1, FAU_STG.4.1)",
                    SettingValue = settingJson,
                    SettingType = SecuritySettingType.AuditLogProtection,
                    IsActive = true,
                    IsEditable = true,
                    DisplayOrder = 8,
                    TblUserGrpIdInsert = userId
                };
                newSetting.SetZamanInsert(DateTime.Now);

                _context.SecuritySettings.Add(newSetting);

                _logger.LogInformation("[FAU_STG] AuditLogProtection settings created by user {UserId}", userId);

                await _auditLogService.LogEventAsync(
                    eventType: "SecuritySettingsCreated",
                    entityType: "SecuritySetting",
                    entityId: AuditLogProtectionSettingKey,
                    isSuccess: true,
                    userId: userId,
                    description: "تنظیمات حفاظت از داده‌های ممیزی ایجاد شد",
                    ct: ct);
            }

            await _context.SaveChangesAsync(ct);

            // پاکسازی Cache
            InvalidateCache(AuditLogProtectionSettingKey);
        }

        private static AuditLogProtectionSettings GetDefaultAuditLogProtectionSettings()
        {
            return new AuditLogProtectionSettings
            {
                IsEnabled = true,
                MaxRetryAttempts = 3,
                EnableAlertOnFailure = true,
                AlertEmailAddresses = string.Empty,
                AlertSmsNumbers = string.Empty,
                RetentionDays = 365,
                ArchiveAfterDays = 90,
                BackupIntervalHours = 24,
                RetentionCheckIntervalHours = 24,
                FallbackRecoveryIntervalMinutes = 5,
                HealthCheckIntervalMinutes = 10,
                FallbackDirectory = string.Empty,
                BackupDirectory = string.Empty,
                ArchiveDirectory = string.Empty
            };
        }
    }
}

