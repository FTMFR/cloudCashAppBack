using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Entities.AuditLogSubsystem;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// سرویس مدیریت خروجی داده‌ها با ویژگی‌های امنیتی
    /// پیاده‌سازی الزامات FDP_ETC.2.1, FDP_ETC.2.2, FDP_ETC.2.4 از استاندارد ISO 15408
    /// </summary>
    public class DataExportService : IDataExportService
    {
        private const string DataExportEventType = "DataExport";

        private readonly NavigationDbContext _context;
        private readonly IDbContextFactory<LogDbContext> _logContextFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DataExportService> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly IPermissionService _permissionService;

        private const string SettingsKey = "DataExport";
        private readonly byte[] _signingKey;

        public DataExportService(
            NavigationDbContext context,
            IDbContextFactory<LogDbContext> logContextFactory,
            IConfiguration configuration,
            ILogger<DataExportService> logger,
            IAuditLogService auditLogService,
            IPermissionService permissionService)
        {
            _context = context;
            _logContextFactory = logContextFactory;
            _configuration = configuration;
            _logger = logger;
            _auditLogService = auditLogService;
            _permissionService = permissionService;

            // SECURITY HARDENING: no insecure default/fallback signing key is allowed.
            var keyString = _configuration["DataExport:SigningKey"];
            if (string.IsNullOrWhiteSpace(keyString))
            {
                throw new InvalidOperationException(
                    "DataExport:SigningKey is required. Configure it via environment variable DataExport__SigningKey.");
            }

            _signingKey = Encoding.UTF8.GetBytes(keyString);

            // SECURITY HARDENING: enforce minimum key length for HMAC signing key.
            if (_signingKey.Length < 32)
            {
                throw new InvalidOperationException(
                    "DataExport:SigningKey must be at least 32 bytes.");
            }
        }

        #region FDP_ETC.2.1 - Export with Security Attributes

        /// <inheritdoc />
        public async Task<SecureExportResponse<T>> WrapWithSecurityAttributesAsync<T>(
            T data,
            ExportContext context,
            CancellationToken ct = default) where T : class
        {
            _logger.LogInformation(
                "[FDP_ETC.2.1] Wrapping data with security attributes. EntityType: {EntityType}, User: {UserId}",
                context.EntityType, context.UserId);

            var settings = await GetSettingsAsync(ct);

            // دریافت ویژگی‌های امنیتی
            var securityAttributes = await GetSecurityAttributesAsync(
                context.EntityType,
                context.EntityId ?? "",
                ct);

            // اعمال قوانین خروجی (FDP_ETC.2.4)
            var ruleResult = await ApplyExportRulesAsync(data, context, ct);

            if (!ruleResult.IsAllowed)
            {
                _logger.LogWarning(
                    "[FDP_ETC.2.4] Export denied. EntityType: {EntityType}, Reason: {Reason}",
                    context.EntityType, ruleResult.DenialReason);

                throw new InvalidOperationException(
                    $"خروجی داده مجاز نیست: {ruleResult.DenialReason}");
            }

            // ماسک داده‌های حساس در صورت نیاز
            var processedData = data;
            if (settings.EnableDataMasking && ruleResult.RequiresMasking)
            {
                processedData = await MaskSensitiveDataAsync(data, context.UserId, ct);
            }

            // FDP_ETC.2.4: field-level permission filtering for sensitive output fields.
            processedData = await FilterFieldsByPermissionAsync(processedData, context.UserId, ct);

            // FDP_ETC.2.2: canonical output encoding to avoid unsafe string emission.
            processedData = EncodeOutputForTransport(processedData);

            // محاسبه هش صحت داده
            var dataJson = JsonSerializer.Serialize(processedData);
            securityAttributes.IntegrityHash = ComputeHash(dataJson);

            // ایجاد متادیتا
            var metadata = new ExportMetadata
            {
                RequestId = context.RequestId,
                ExportedAt = DateTime.UtcNow,
                ExportedBy = context.UserId,
                ExportedByName = context.UserName,
                ExporterIpAddress = context.IpAddress,
                Format = context.RequestedFormat,
                RecordCount = GetRecordCount(processedData)
            };

            var response = new SecureExportResponse<T>
            {
                Data = processedData,
                SecurityContext = securityAttributes,
                Metadata = metadata,
                AppliedRules = ruleResult.AppliedRules
            };

            // امضای دیجیتال (FDP_ETC.2.2)
            if (settings.EnableDigitalSignature)
            {
                response.Signature = await SignExportDataAsync(processedData, securityAttributes, ct);
            }

            // ثبت لاگ خروجی
            if (settings.EnableExportAudit)
            {
                await LogExportAsync(new ExportAuditEntry
                {
                    UserId = context.UserId,
                    UserName = context.UserName ?? "Unknown",
                    IpAddress = context.IpAddress ?? "Unknown",
                    EntityType = context.EntityType,
                    EntityId = context.EntityId,
                    RecordCount = metadata.RecordCount ?? 0,
                    DataSizeBytes = Encoding.UTF8.GetByteCount(dataJson),
                    Format = context.RequestedFormat,
                    SensitivityLevel = securityAttributes.SensitivityLevel,
                    WasMasked = ruleResult.RequiresMasking,
                    AppliedRules = ruleResult.AppliedRules,
                    RequestPath = context.RequestPath ?? "",
                    RequestId = context.RequestId,
                    IsSuccess = true
                }, ct);
            }

            _logger.LogInformation(
                "[FDP_ETC.2.1] Data export completed. EntityType: {EntityType}, Records: {Count}, Masked: {Masked}",
                context.EntityType, metadata.RecordCount, ruleResult.RequiresMasking);

            return response;
        }

        /// <inheritdoc />
        public async Task<SecurityAttributes> GetSecurityAttributesAsync(
            string entityType,
            string entityId,
            CancellationToken ct = default)
        {
            var sensitivityLevel = await GetEntitySensitivityLevelAsync(entityType, entityId, ct);

            return new SecurityAttributes
            {
                SensitivityLevel = sensitivityLevel?.Code ?? "Internal",
                Classification = GetClassificationForEntity(entityType),
                DataOwner = "System",
                CreatedAt = DateTime.UtcNow,
                SecurityLabels = GetSecurityLabelsForEntity(entityType),
                AccessRestrictions = new List<string> { "Authenticated" }
            };
        }

        #endregion

        #region FDP_ETC.2.2 - Unambiguous Association

        /// <inheritdoc />
        public Task<string> SignExportDataAsync(
            object data,
            SecurityAttributes attributes,
            CancellationToken ct = default)
        {
            _logger.LogDebug("[FDP_ETC.2.2] Signing export data for unambiguous association");

            // ایجاد رشته برای امضا شامل داده و ویژگی‌های امنیتی
            var dataToSign = new
            {
                Data = data,
                SecurityContext = attributes,
                Timestamp = DateTime.UtcNow.Ticks
            };

            var json = JsonSerializer.Serialize(dataToSign);
            var signature = ComputeHmac(json);

            return Task.FromResult(signature);
        }

        /// <inheritdoc />
        public Task<bool> VerifyExportSignatureAsync(
            string signature,
            object data,
            SecurityAttributes attributes,
            CancellationToken ct = default)
        {
            // Note: در یک پیاده‌سازی کامل باید timestamp را نیز بررسی کرد
            var dataToVerify = new
            {
                Data = data,
                SecurityContext = attributes
            };

            var json = JsonSerializer.Serialize(dataToVerify);
            var expectedSignature = ComputeHmac(json);

            // مقایسه امن با زمان ثابت
            return Task.FromResult(CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(expectedSignature)));
        }

        #endregion

        #region FDP_ETC.2.4 - Additional Export Control Rules

        /// <inheritdoc />
        public async Task<ExportRuleResult> ApplyExportRulesAsync<T>(
            T data,
            ExportContext context,
            CancellationToken ct = default) where T : class
        {
            _logger.LogDebug("[FDP_ETC.2.4] Applying export rules for EntityType: {EntityType}", context.EntityType);

            var result = new ExportRuleResult { IsAllowed = true };
            var settings = await GetSettingsAsync(ct);
            var rules = await GetExportRulesAsync(ct);

            foreach (var rule in rules.Where(r => r.IsActive).OrderBy(r => r.Priority))
            {
                if (!MatchesEntityType(rule.EntityType, context.EntityType))
                    continue;

                switch (rule.RuleType)
                {
                    case ExportRuleType.RecordLimit:
                        var recordCount = GetRecordCount(data);
                        if (recordCount > settings.MaxRecordsPerExport)
                        {
                            result.IsAllowed = false;
                            result.DenialReason = $"تعداد رکوردها ({recordCount}) بیش از حد مجاز ({settings.MaxRecordsPerExport}) است";
                            return result;
                        }
                        result.AppliedRules.Add($"RecordLimit: {settings.MaxRecordsPerExport}");
                        break;

                    case ExportRuleType.TimeRestriction:
                        if (!IsWithinAllowedTime(rule.Condition))
                        {
                            result.IsAllowed = false;
                            result.DenialReason = "خروجی داده در این ساعت مجاز نیست";
                            return result;
                        }
                        result.AppliedRules.Add("TimeRestriction");
                        break;

                    case ExportRuleType.SensitivityFilter:
                        var sensitivity = await GetEntitySensitivityLevelAsync(context.EntityType, context.EntityId ?? "", ct);
                        if (sensitivity != null && sensitivity.Level > 2) // Confidential or higher
                        {
                            if (!await _permissionService.HasPermissionAsync(context.UserId, "DataExport.Admin", ct))
                            {
                                result.IsAllowed = false;
                                result.DenialReason = "دسترسی به داده‌های با سطح حساسیت بالا مجاز نیست";
                                return result;
                            }
                        }
                        result.AppliedRules.Add("SensitivityFilter");
                        break;

                    case ExportRuleType.DataMasking:
                        result.RequiresMasking = true;
                        result.FieldsToMask.AddRange(ParseFieldList(rule.Condition));
                        result.AppliedRules.Add("DataMasking");
                        break;

                    case ExportRuleType.FieldRemoval:
                        result.FieldsToRemove.AddRange(ParseFieldList(rule.Condition));
                        result.AppliedRules.Add("FieldRemoval");
                        break;
                }
            }

            // اضافه کردن قانون ماسک پیش‌فرض برای فیلدهای حساس
            var maskingRules = await GetMaskingRulesAsync(ct);
            if (maskingRules.Any(m => m.IsActive && MatchesEntityType(m.EntityType, context.EntityType)))
            {
                result.RequiresMasking = true;
                result.AppliedRules.Add("DefaultMasking");
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<T> MaskSensitiveDataAsync<T>(
            T data,
            long userId,
            CancellationToken ct = default) where T : class
        {
            if (data == null) return data;

            var maskingRules = await GetMaskingRulesAsync(ct);
            var activeRules = maskingRules.Where(r => r.IsActive).ToList();

            if (!activeRules.Any()) return data;

            // اگر داده لیست است
            if (data is System.Collections.IList list && !(data is string))
            {
                // ماسک کردن هر آیتم در لیست اصلی (in-place)
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null)
                    {
                        await MaskObjectAsync(list[i], activeRules, userId, ct);
                    }
                }
                return data; // همان لیست اصلی را برمی‌گرداند
            }

            // اگر IEnumerable است ولی IList نیست
            if (data is System.Collections.IEnumerable enumerable && !(data is string))
            {
                foreach (var item in enumerable)
                {
                    if (item != null)
                    {
                        await MaskObjectAsync(item, activeRules, userId, ct);
                    }
                }
                return data;
            }

            await MaskObjectAsync(data, activeRules, userId, ct);
            return data;
        }

        /// <inheritdoc />
        public async Task<T> FilterFieldsByPermissionAsync<T>(
            T data,
            long userId,
            CancellationToken ct = default) where T : class
        {
            // در این پیاده‌سازی ساده، داده را بدون تغییر برمی‌گرداند
            // در پیاده‌سازی پیشرفته‌تر می‌توان فیلدهای خاص را بر اساس Permission حذف کرد
            if (data == null) return data;

            // Users with explicit admin export permission can view full payload.
            if (await _permissionService.HasPermissionAsync(userId, "DataExport.Admin", ct))
            {
                return data;
            }

            FilterSensitiveFields(data, new HashSet<object>());
            return data;
        }

        #endregion

        #region Settings & Rules Management

        /// <inheritdoc />
        public async Task<DataExportSettings> GetSettingsAsync(CancellationToken ct = default)
        {
            var setting = await _context.tblShobeSettings
                .FirstOrDefaultAsync(s => s.SettingKey == SettingsKey && s.TblShobeId == null && s.IsActive, ct);

            if (setting == null)
            {
                return GetDefaultSettings();
            }

            try
            {
                return JsonSerializer.Deserialize<DataExportSettings>(setting.SettingValue)
                    ?? GetDefaultSettings();
            }
            catch
            {
                return GetDefaultSettings();
            }
        }

        /// <inheritdoc />
        public async Task<bool> UpdateSettingsAsync(
            DataExportSettings settings,
            long updatedBy,
            CancellationToken ct = default)
        {
            var existing = await _context.tblShobeSettings
                .FirstOrDefaultAsync(s => s.SettingKey == SettingsKey && s.TblShobeId == null, ct);

            var json = JsonSerializer.Serialize(settings);

            if (existing == null)
            {
                var newSetting = new tblShobeSetting
                {
                    TblShobeId = null,
                    SettingKey = SettingsKey,
                    SettingName = "تنظیمات خروجی داده‌ها",
                    Description = "تنظیمات مربوط به FDP_ETC.2 - خروجی داده با ویژگی‌های امنیتی",
                    SettingValue = json,
                    SettingType = ShobeSettingType.DataExport,
                    IsActive = true,
                    IsEditable = true,
                    DisplayOrder = 3,
                    TblUserGrpIdInsert = updatedBy
                };
                newSetting.SetZamanInsert(DateTime.UtcNow);
                _context.tblShobeSettings.Add(newSetting);
            }
            else
            {
                existing.SettingValue = json;
                existing.TblUserGrpIdLastEdit = updatedBy;
                existing.SetZamanLastEdit(DateTime.UtcNow);
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("[FDP_ETC.2] Data export settings updated by user {UserId}", updatedBy);

            await _auditLogService.LogEventAsync(
                eventType: "DataExportSettingsUpdated",
                entityType: "DataExportSettings",
                entityId: SettingsKey,
                isSuccess: true,
                userId: updatedBy,
                description: "تنظیمات خروجی داده‌ها به‌روزرسانی شد",
                ct: ct);

            return true;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ExportRule>> GetExportRulesAsync(CancellationToken ct = default)
        {
            var setting = await _context.tblShobeSettings
                .FirstOrDefaultAsync(s => s.SettingKey == $"{SettingsKey}:Rules" && s.TblShobeId == null && s.IsActive, ct);

            if (setting == null)
            {
                return GetDefaultExportRules();
            }

            try
            {
                return JsonSerializer.Deserialize<List<ExportRule>>(setting.SettingValue)
                    ?? GetDefaultExportRules();
            }
            catch
            {
                return GetDefaultExportRules();
            }
        }

        /// <inheritdoc />
        public async Task<ExportRule> CreateExportRuleAsync(
            ExportRule rule,
            long createdBy,
            CancellationToken ct = default)
        {
            rule.Id = Guid.NewGuid();
            rule.CreatedAt = DateTime.UtcNow;
            rule.CreatedBy = createdBy;

            var rules = (await GetExportRulesAsync(ct)).ToList();
            rules.Add(rule);

            await SaveExportRulesAsync(rules, createdBy, ct);

            _logger.LogInformation("[FDP_ETC.2.4] Export rule created: {RuleName}", rule.Name);

            return rule;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateExportRuleAsync(
            ExportRule rule,
            long updatedBy,
            CancellationToken ct = default)
        {
            var rules = (await GetExportRulesAsync(ct)).ToList();
            var index = rules.FindIndex(r => r.Id == rule.Id);

            if (index < 0) return false;

            rule.UpdatedAt = DateTime.UtcNow;
            rule.UpdatedBy = updatedBy;
            rules[index] = rule;

            await SaveExportRulesAsync(rules, updatedBy, ct);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteExportRuleAsync(
            Guid ruleId,
            long deletedBy,
            CancellationToken ct = default)
        {
            var rules = (await GetExportRulesAsync(ct)).ToList();
            var removed = rules.RemoveAll(r => r.Id == ruleId);

            if (removed == 0) return false;

            await SaveExportRulesAsync(rules, deletedBy, ct);

            return true;
        }

        #endregion

        #region Masking Rules Management

        /// <inheritdoc />
        public async Task<IEnumerable<DataMaskingRule>> GetMaskingRulesAsync(CancellationToken ct = default)
        {
            var setting = await _context.tblShobeSettings
                .FirstOrDefaultAsync(s => s.SettingKey == $"{SettingsKey}:MaskingRules" && s.TblShobeId == null && s.IsActive, ct);

            if (setting == null)
            {
                return GetDefaultMaskingRules();
            }

            try
            {
                return JsonSerializer.Deserialize<List<DataMaskingRule>>(setting.SettingValue)
                    ?? GetDefaultMaskingRules();
            }
            catch
            {
                return GetDefaultMaskingRules();
            }
        }

        /// <inheritdoc />
        public async Task<DataMaskingRule> CreateMaskingRuleAsync(
            DataMaskingRule rule,
            long createdBy,
            CancellationToken ct = default)
        {
            rule.Id = Guid.NewGuid();
            rule.CreatedAt = DateTime.UtcNow;
            rule.CreatedBy = createdBy;

            var rules = (await GetMaskingRulesAsync(ct)).ToList();
            rules.Add(rule);

            await SaveMaskingRulesAsync(rules, createdBy, ct);

            _logger.LogInformation("[FDP_ETC.2.4] Masking rule created: {RuleName}", rule.Name);

            return rule;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateMaskingRuleAsync(
            DataMaskingRule rule,
            long updatedBy,
            CancellationToken ct = default)
        {
            var rules = (await GetMaskingRulesAsync(ct)).ToList();
            var index = rules.FindIndex(r => r.Id == rule.Id);

            if (index < 0) return false;

            rules[index] = rule;

            await SaveMaskingRulesAsync(rules, updatedBy, ct);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteMaskingRuleAsync(
            Guid ruleId,
            long deletedBy,
            CancellationToken ct = default)
        {
            var rules = (await GetMaskingRulesAsync(ct)).ToList();
            var removed = rules.RemoveAll(r => r.Id == ruleId);

            if (removed == 0) return false;

            await SaveMaskingRulesAsync(rules, deletedBy, ct);

            return true;
        }

        #endregion

        #region Sensitivity Levels Management

        /// <inheritdoc />
        public async Task<IEnumerable<SensitivityLevel>> GetSensitivityLevelsAsync(CancellationToken ct = default)
        {
            var setting = await _context.tblShobeSettings
                .FirstOrDefaultAsync(s => s.SettingKey == $"{SettingsKey}:SensitivityLevels" && s.TblShobeId == null && s.IsActive, ct);

            if (setting == null)
            {
                return GetDefaultSensitivityLevels();
            }

            try
            {
                return JsonSerializer.Deserialize<List<SensitivityLevel>>(setting.SettingValue)
                    ?? GetDefaultSensitivityLevels();
            }
            catch
            {
                return GetDefaultSensitivityLevels();
            }
        }

        /// <inheritdoc />
        public async Task<SensitivityLevel> GetEntitySensitivityLevelAsync(
            string entityType,
            string entityId,
            CancellationToken ct = default)
        {
            var levels = await GetSensitivityLevelsAsync(ct);

            // تعیین سطح حساسیت بر اساس نوع Entity
            var level = entityType.ToLower() switch
            {
                "user" or "tbluser" => levels.FirstOrDefault(l => l.Code == "Confidential"),
                "auditlog" or "auditlogmaster" => levels.FirstOrDefault(l => l.Code == "Internal"),
                "permission" or "tblpermission" => levels.FirstOrDefault(l => l.Code == "Internal"),
                "securitysetting" => levels.FirstOrDefault(l => l.Code == "Secret"),
                "cryptographickey" => levels.FirstOrDefault(l => l.Code == "Secret"),
                _ => levels.FirstOrDefault(l => l.Code == "Internal")
            };

            return level ?? new SensitivityLevel { Code = "Internal", Name = "داخلی", Level = 1 };
        }

        #endregion

        #region Export Audit Log

        /// <inheritdoc />
        public async Task LogExportAsync(
            ExportAuditEntry entry,
            CancellationToken ct = default)
        {
            // ثبت در Audit Log اصلی
            await _auditLogService.LogEventAsync(
                eventType: "DataExport",
                entityType: entry.EntityType,
                entityId: entry.EntityId ?? "Multiple",
                isSuccess: entry.IsSuccess,
                ipAddress: entry.IpAddress,
                userName: entry.UserName,
                userId: entry.UserId,
                description: $"خروجی {entry.RecordCount} رکورد از {entry.EntityType}. " +
                            $"حجم: {entry.DataSizeBytes} بایت. " +
                            $"سطح حساسیت: {entry.SensitivityLevel}. " +
                            $"ماسک شده: {(entry.WasMasked ? "بله" : "خیر")}",
                ct: ct);

            _logger.LogInformation(
                "[FDP_ETC.2] Export audit logged. User: {User}, Entity: {Entity}, Records: {Count}",
                entry.UserName, entry.EntityType, entry.RecordCount);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ExportAuditEntry>> GetExportAuditLogAsync(
            ExportAuditFilter filter,
            CancellationToken ct = default)
        {
            // در پیاده‌سازی واقعی، از جدول مجزا برای لاگ خروجی استفاده می‌شود
            // فعلاً یک لیست خالی برمی‌گرداند
            return await Task.FromResult(new List<ExportAuditEntry>());
        }

        /// <inheritdoc />
        public async Task<ExportStatistics> GetExportStatisticsAsync(CancellationToken ct = default)
        {
            await using var logContext = await _logContextFactory.CreateDbContextAsync(ct);

            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var sevenDaysAgo = todayStart.AddDays(-7);
            var thirtyDaysAgo = todayStart.AddDays(-30);

            var exportEvents = await logContext.AuditLogMasters
                .AsNoTracking()
                .Where(x => x.EventType == DataExportEventType)
                .Select(x => new
                {
                    x.EventDateTime,
                    x.EntityType,
                    x.UserName,
                    x.UserId,
                    x.IsSuccess,
                    x.Description
                })
                .ToListAsync(ct);

            var totalExports = exportEvents.Count;
            var todayExports = exportEvents.Count(x => x.EventDateTime >= todayStart);
            var last7DaysExports = exportEvents.Count(x => x.EventDateTime >= sevenDaysAgo);
            var last30DaysExports = exportEvents.Count(x => x.EventDateTime >= thirtyDaysAgo);
            var failedExportsCount = exportEvents.Count(x => !x.IsSuccess);

            var exportsByEntityType = exportEvents
                .Where(x => !string.IsNullOrEmpty(x.EntityType))
                .GroupBy(x => x.EntityType!)
                .ToDictionary(g => g.Key, g => (long)g.Count());

            var exportsByUser = exportEvents
                .Where(x => x.UserId.HasValue && x.UserId.Value > 0)
                .GroupBy(x => x.UserId!.Value)
                .ToDictionary(g => g.Key.ToString(), g => (long)g.Count());

            // تعداد رکوردها و حجم از روی Description قابل استخراج است (اختیاری)
            long totalRecordsExported = 0;
            long totalDataSizeBytes = 0;
            int maskedCount = 0;
            foreach (var evt in exportEvents.Where(x => !string.IsNullOrEmpty(x.Description)))
            {
                var d = evt.Description!;
                if (TryParseRecordCountFromDescription(d, out var records))
                    totalRecordsExported += records;
                if (TryParseDataSizeFromDescription(d, out var bytes))
                    totalDataSizeBytes += bytes;
                if (d.Contains("ماسک شده: بله", StringComparison.OrdinalIgnoreCase))
                    maskedCount++;
            }

            return new ExportStatistics
            {
                TotalExports = totalExports,
                TodayExports = todayExports,
                Last7DaysExports = last7DaysExports,
                Last30DaysExports = last30DaysExports,
                TotalRecordsExported = totalRecordsExported,
                TotalDataSizeBytes = totalDataSizeBytes,
                ExportsByEntityType = exportsByEntityType,
                ExportsBySensitivityLevel = new Dictionary<string, long>(),
                ExportsByUser = exportsByUser,
                MaskedExportsCount = maskedCount,
                FailedExportsCount = failedExportsCount
            };
        }

        private static bool TryParseRecordCountFromDescription(string description, out long count)
        {
            count = 0;
            var idx = description.IndexOf("خروجی ", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;
            var start = idx + "خروجی ".Length;
            var end = description.IndexOf(" رکورد", start, StringComparison.OrdinalIgnoreCase);
            if (end <= start) return false;
            var numStr = description.Substring(start, end - start).Trim();
            return long.TryParse(numStr, out count);
        }

        private static bool TryParseDataSizeFromDescription(string description, out long bytes)
        {
            bytes = 0;
            var idx = description.IndexOf("حجم: ", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;
            var start = idx + "حجم: ".Length;
            var end = description.IndexOf(" بایت", start, StringComparison.OrdinalIgnoreCase);
            if (end <= start) return false;
            var numStr = description.Substring(start, end - start).Trim();
            return long.TryParse(numStr, out bytes);
        }

        #endregion

        #region Private Helper Methods

        private DataExportSettings GetDefaultSettings()
        {
            return new DataExportSettings
            {
                IsEnabled = true,
                EnableDigitalSignature = true,
                EnableDataMasking = true,
                EnableExportAudit = true,
                MaxRecordsPerExport = 10000,
                MaxExportSizeBytes = 50 * 1024 * 1024,
                AllowedFormats = new List<string> { "JSON" },
                DefaultSensitivityLevel = "Internal",
                SignatureAlgorithm = "HMAC-SHA256",
                AuditLogRetentionDays = 365
            };
        }

        private IEnumerable<ExportRule> GetDefaultExportRules()
        {
            return new List<ExportRule>
            {
                new ExportRule
                {
                    Name = "RecordLimit",
                    Description = "محدودیت تعداد رکورد در هر خروجی",
                    RuleType = ExportRuleType.RecordLimit,
                    EntityType = "*",
                    Priority = 10,
                    IsActive = true
                },
                new ExportRule
                {
                    Name = "SensitivityFilter",
                    Description = "فیلتر داده‌های با حساسیت بالا",
                    RuleType = ExportRuleType.SensitivityFilter,
                    EntityType = "*",
                    Priority = 20,
                    IsActive = true
                }
            };
        }

        private IEnumerable<DataMaskingRule> GetDefaultMaskingRules()
        {
            return new List<DataMaskingRule>
            {
                new DataMaskingRule
                {
                    Name = "EmailMask",
                    Description = "ماسک آدرس ایمیل",
                    EntityType = "*",
                    FieldName = "Email",
                    MaskingType = MaskingType.EmailMask,
                    IsActive = true
                },
                new DataMaskingRule
                {
                    Name = "MobileNumberMask",
                    Description = "ماسک شماره موبایل",
                    EntityType = "*",
                    FieldName = "MobileNumber",
                    MaskingType = MaskingType.PhoneMask,
                    VisibleCharsStart = 0,
                    VisibleCharsEnd = 4,
                    IsActive = true
                },
                new DataMaskingRule
                {
                    Name = "PhoneMask",
                    Description = "ماسک شماره تلفن",
                    EntityType = "*",
                    FieldName = "Phone",
                    MaskingType = MaskingType.PhoneMask,
                    VisibleCharsStart = 0,
                    VisibleCharsEnd = 4,
                    IsActive = true
                },
                new DataMaskingRule
                {
                    Name = "IpAddressMask",
                    Description = "ماسک آدرس IP",
                    EntityType = "*",
                    FieldName = "IpAddress",
                    MaskingType = MaskingType.PartialMask,
                    VisibleCharsStart = 0,
                    VisibleCharsEnd = 0,
                    MaskPattern = "***.***.***",
                    IsActive = true,
                    ExcludePermissions = "DataExport.Admin"
                }
            };
        }

        private IEnumerable<SensitivityLevel> GetDefaultSensitivityLevels()
        {
            return new List<SensitivityLevel>
            {
                new SensitivityLevel
                {
                    Code = "Public",
                    Name = "عمومی",
                    Description = "داده‌های قابل انتشار عمومی",
                    Level = 0,
                    Color = "#28a745",
                    RequiresEncryption = false,
                    RequiresAudit = false,
                    RequiresApproval = false
                },
                new SensitivityLevel
                {
                    Code = "Internal",
                    Name = "داخلی",
                    Description = "داده‌های داخلی سازمان",
                    Level = 1,
                    Color = "#17a2b8",
                    RequiresEncryption = false,
                    RequiresAudit = true,
                    RequiresApproval = false
                },
                new SensitivityLevel
                {
                    Code = "Confidential",
                    Name = "محرمانه",
                    Description = "داده‌های محرمانه",
                    Level = 2,
                    Color = "#ffc107",
                    RequiresEncryption = true,
                    RequiresAudit = true,
                    RequiresApproval = false,
                    RequiredPermission = "DataExport.Read"
                },
                new SensitivityLevel
                {
                    Code = "Secret",
                    Name = "سری",
                    Description = "داده‌های سری و حساس",
                    Level = 3,
                    Color = "#dc3545",
                    RequiresEncryption = true,
                    RequiresAudit = true,
                    RequiresApproval = true,
                    RequiredPermission = "DataExport.Admin"
                }
            };
        }

        private async Task SaveExportRulesAsync(List<ExportRule> rules, long updatedBy, CancellationToken ct)
        {
            var settingKey = $"{SettingsKey}:Rules";
            var existing = await _context.tblShobeSettings
                .FirstOrDefaultAsync(s => s.SettingKey == settingKey && s.TblShobeId == null, ct);

            var json = JsonSerializer.Serialize(rules);

            if (existing == null)
            {
                var newSetting = new tblShobeSetting
                {
                    TblShobeId = null,
                    SettingKey = settingKey,
                    SettingName = "قوانین خروجی داده‌ها",
                    Description = "قوانین کنترل خروجی (FDP_ETC.2.4)",
                    SettingValue = json,
                    SettingType = ShobeSettingType.DataExport,
                    IsActive = true,
                    IsEditable = true,
                    DisplayOrder = 4,
                    TblUserGrpIdInsert = updatedBy
                };
                newSetting.SetZamanInsert(DateTime.UtcNow);
                _context.tblShobeSettings.Add(newSetting);
            }
            else
            {
                existing.SettingValue = json;
                existing.TblUserGrpIdLastEdit = updatedBy;
                existing.SetZamanLastEdit(DateTime.UtcNow);
            }

            await _context.SaveChangesAsync(ct);
        }

        private async Task SaveMaskingRulesAsync(List<DataMaskingRule> rules, long updatedBy, CancellationToken ct)
        {
            var settingKey = $"{SettingsKey}:MaskingRules";
            var existing = await _context.tblShobeSettings
                .FirstOrDefaultAsync(s => s.SettingKey == settingKey && s.TblShobeId == null, ct);

            var json = JsonSerializer.Serialize(rules);

            if (existing == null)
            {
                var newSetting = new tblShobeSetting
                {
                    TblShobeId = null,
                    SettingKey = settingKey,
                    SettingName = "قوانین ماسک داده‌ها",
                    Description = "قوانین ماسک کردن داده‌های حساس (FDP_ETC.2.4)",
                    SettingValue = json,
                    SettingType = ShobeSettingType.DataExport,
                    IsActive = true,
                    IsEditable = true,
                    DisplayOrder = 5,
                    TblUserGrpIdInsert = updatedBy
                };
                newSetting.SetZamanInsert(DateTime.UtcNow);
                _context.tblShobeSettings.Add(newSetting);
            }
            else
            {
                existing.SettingValue = json;
                existing.TblUserGrpIdLastEdit = updatedBy;
                existing.SetZamanLastEdit(DateTime.UtcNow);
            }

            await _context.SaveChangesAsync(ct);
        }

        private string ComputeHash(string data)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(bytes);
        }

        private string ComputeHmac(string data)
        {
            using var hmac = new HMACSHA256(_signingKey);
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(bytes);
        }

        private int GetRecordCount<T>(T data) where T : class
        {
            if (data is System.Collections.IEnumerable enumerable && !(data is string))
            {
                return enumerable.Cast<object>().Count();
            }
            return 1;
        }

        private bool MatchesEntityType(string ruleEntityType, string actualEntityType)
        {
            if (ruleEntityType == "*") return true;
            return string.Equals(ruleEntityType, actualEntityType, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsWithinAllowedTime(string condition)
        {
            // فرمت: "08:00-18:00" یا "WorkingHours"
            if (string.IsNullOrEmpty(condition)) return true;
            if (condition == "WorkingHours")
            {
                var now = DateTime.Now;
                return now.Hour >= 8 && now.Hour < 18 && now.DayOfWeek != DayOfWeek.Friday;
            }
            return true;
        }

        private List<string> ParseFieldList(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return new List<string>();
            return condition.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .ToList();
        }

        private string GetClassificationForEntity(string entityType)
        {
            return entityType.ToLower() switch
            {
                "user" or "tbluser" => "Personal Data",
                "auditlog" => "Audit Data",
                "securitysetting" => "Security Configuration",
                _ => "Standard"
            };
        }

        private List<string> GetSecurityLabelsForEntity(string entityType)
        {
            var labels = new List<string> { "Authenticated" };

            switch (entityType.ToLower())
            {
                case "user":
                case "tbluser":
                    labels.Add("PII");
                    labels.Add("GDPR");
                    break;
                case "auditlog":
                    labels.Add("AuditTrail");
                    break;
                case "securitysetting":
                    labels.Add("SecurityConfig");
                    break;
            }

            return labels;
        }

        private async Task<object> MaskObjectAsync(
            object obj,
            List<DataMaskingRule> rules,
            long userId,
            CancellationToken ct)
        {
            if (obj == null) return obj;

            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (!property.CanRead || !property.CanWrite) continue;

                var rule = rules.FirstOrDefault(r =>
                    string.Equals(r.FieldName, property.Name, StringComparison.OrdinalIgnoreCase));

                if (rule == null) continue;

                // بررسی استثنائات
                if (!string.IsNullOrEmpty(rule.ExcludePermissions))
                {
                    var excludePerms = rule.ExcludePermissions.Split(',');
                    var hasExcludePermission = false;
                    foreach (var perm in excludePerms)
                    {
                        if (await _permissionService.HasPermissionAsync(userId, perm.Trim(), ct))
                        {
                            hasExcludePermission = true;
                            break;
                        }
                    }
                    if (hasExcludePermission) continue;
                }

                var value = property.GetValue(obj)?.ToString();
                if (string.IsNullOrEmpty(value)) continue;

                var maskedValue = MaskValue(value, rule);
                property.SetValue(obj, maskedValue);
            }

            return obj;
        }

        private void FilterSensitiveFields(object obj, HashSet<object> visited)
        {
            if (obj == null || obj is string) return;

            var type = obj.GetType();
            if (IsSimpleType(type)) return;
            if (!visited.Add(obj)) return;

            if (obj is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item != null)
                    {
                        FilterSensitiveFields(item, visited);
                    }
                }
                return;
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (!property.CanRead) continue;

                var value = property.GetValue(obj);
                if (value == null) continue;

                if (ShouldRedactField(property.Name))
                {
                    if (property.CanWrite)
                    {
                        if (property.PropertyType == typeof(string))
                        {
                            property.SetValue(obj, "[REDACTED]");
                        }
                        else if (!property.PropertyType.IsValueType ||
                                 Nullable.GetUnderlyingType(property.PropertyType) != null)
                        {
                            property.SetValue(obj, null);
                        }
                    }
                    continue;
                }

                if (!IsSimpleType(property.PropertyType))
                {
                    FilterSensitiveFields(value, visited);
                }
            }
        }

        private static bool ShouldRedactField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) return false;

            return fieldName.Equals("Password", StringComparison.OrdinalIgnoreCase) ||
                   fieldName.Equals("PasswordHash", StringComparison.OrdinalIgnoreCase) ||
                   fieldName.Equals("AccessToken", StringComparison.OrdinalIgnoreCase) ||
                   fieldName.Equals("RefreshToken", StringComparison.OrdinalIgnoreCase) ||
                   fieldName.Equals("Token", StringComparison.OrdinalIgnoreCase) ||
                   fieldName.Equals("ApiKey", StringComparison.OrdinalIgnoreCase) ||
                   fieldName.Equals("SigningKey", StringComparison.OrdinalIgnoreCase) ||
                   fieldName.Equals("SecretKey", StringComparison.OrdinalIgnoreCase) ||
                   fieldName.Equals("PrivateKey", StringComparison.OrdinalIgnoreCase) ||
                   fieldName.Equals("ConnectionString", StringComparison.OrdinalIgnoreCase);
        }

        private T EncodeOutputForTransport<T>(T data) where T : class
        {
            if (data == null) return data;

            EncodeObjectGraph(data, new HashSet<object>());
            return data;
        }

        private void EncodeObjectGraph(object obj, HashSet<object> visited)
        {
            if (obj == null || obj is string) return;

            var type = obj.GetType();
            if (IsSimpleType(type)) return;
            if (!visited.Add(obj)) return;

            if (obj is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item != null)
                    {
                        EncodeObjectGraph(item, visited);
                    }
                }
                return;
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (!property.CanRead) continue;

                var value = property.GetValue(obj);
                if (value == null) continue;

                if (property.PropertyType == typeof(string))
                {
                    if (!property.CanWrite) continue;

                    var encoded = EncodeOutputString(value.ToString() ?? string.Empty);
                    property.SetValue(obj, encoded);
                    continue;
                }

                if (!IsSimpleType(property.PropertyType))
                {
                    EncodeObjectGraph(value, visited);
                }
            }
        }

        private static string EncodeOutputString(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var buffer = new StringBuilder(input.Length);
            foreach (var ch in input)
            {
                if (char.IsControl(ch) && ch != '\r' && ch != '\n' && ch != '\t')
                {
                    continue;
                }

                switch (ch)
                {
                    case '<':
                        buffer.Append("\\u003C");
                        break;
                    case '>':
                        buffer.Append("\\u003E");
                        break;
                    case '&':
                        buffer.Append("\\u0026");
                        break;
                    case '"':
                        buffer.Append("\\u0022");
                        break;
                    case '\'':
                        buffer.Append("\\u0027");
                        break;
                    default:
                        buffer.Append(ch);
                        break;
                }
            }

            return buffer.ToString();
        }

        private static bool IsSimpleType(Type type)
        {
            var actualType = Nullable.GetUnderlyingType(type) ?? type;

            return actualType.IsPrimitive ||
                   actualType.IsEnum ||
                   actualType == typeof(string) ||
                   actualType == typeof(decimal) ||
                   actualType == typeof(DateTime) ||
                   actualType == typeof(DateTimeOffset) ||
                   actualType == typeof(TimeSpan) ||
                   actualType == typeof(Guid);
        }

        private string MaskValue(string value, DataMaskingRule rule)
        {
            if (string.IsNullOrEmpty(value)) return value;

            return rule.MaskingType switch
            {
                MaskingType.FullReplacement => rule.MaskPattern,

                MaskingType.PartialMask => MaskPartial(value, rule.VisibleCharsStart, rule.VisibleCharsEnd),

                MaskingType.EmailMask => MaskEmail(value),

                MaskingType.PhoneMask => MaskPhone(value, rule.VisibleCharsEnd),

                MaskingType.NationalIdMask => MaskNationalId(value),

                MaskingType.CardNumberMask => MaskCardNumber(value),

                MaskingType.Hash => ComputeHash(value),

                _ => rule.MaskPattern
            };
        }

        private string MaskPartial(string value, int visibleStart, int visibleEnd)
        {
            if (value.Length <= visibleStart + visibleEnd)
                return new string('*', value.Length);

            var start = value.Substring(0, visibleStart);
            var end = visibleEnd > 0 ? value.Substring(value.Length - visibleEnd) : "";
            var middle = new string('*', value.Length - visibleStart - visibleEnd);

            return start + middle + end;
        }

        private string MaskEmail(string email)
        {
            var atIndex = email.IndexOf('@');
            if (atIndex <= 0) return "***@***.***";

            var localPart = email.Substring(0, atIndex);
            var domainPart = email.Substring(atIndex);

            var maskedLocal = localPart.Length <= 2
                ? new string('*', localPart.Length)
                : localPart[0] + new string('*', localPart.Length - 2) + localPart[^1];

            return maskedLocal + domainPart;
        }

        private string MaskPhone(string phone, int visibleChars)
        {
            if (phone.Length <= visibleChars)
                return new string('*', phone.Length);

            return new string('*', phone.Length - visibleChars) + phone.Substring(phone.Length - visibleChars);
        }

        private string MaskNationalId(string nationalId)
        {
            if (nationalId.Length != 10) return "**********";
            return nationalId.Substring(0, 3) + "****" + nationalId.Substring(7);
        }

        private string MaskCardNumber(string cardNumber)
        {
            var digits = new string(cardNumber.Where(char.IsDigit).ToArray());
            if (digits.Length < 16) return "**** **** **** ****";
            return digits.Substring(0, 4) + " **** **** " + digits.Substring(12);
        }

        #endregion
    }
}
