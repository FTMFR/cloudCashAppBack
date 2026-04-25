using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس مدیریت خروجی داده‌ها با ویژگی‌های امنیتی
    /// پیاده‌سازی الزامات FDP_ETC.2.1, FDP_ETC.2.2, FDP_ETC.2.4 از استاندارد ISO 15408
    /// 
    /// FDP_ETC.2.1: خروجی داده‌ها با ویژگی‌های امنیتی مرتبط
    /// FDP_ETC.2.2: ارتباط بدون ابهام ویژگی‌های امنیتی با داده‌های خروجی
    /// FDP_ETC.2.4: اعمال قوانین کنترل خروجی اضافی
    /// </summary>
    public interface IDataExportService
    {
        #region FDP_ETC.2.1 - Export with Security Attributes

        /// <summary>
        /// پوشش داده با ویژگی‌های امنیتی مرتبط
        /// FDP_ETC.2.1: خروجی داده با ویژگی‌های امنیتی
        /// </summary>
        Task<SecureExportResponse<T>> WrapWithSecurityAttributesAsync<T>(
            T data,
            ExportContext context,
            CancellationToken ct = default) where T : class;

        /// <summary>
        /// دریافت ویژگی‌های امنیتی برای یک نوع داده
        /// </summary>
        Task<SecurityAttributes> GetSecurityAttributesAsync(
            string entityType,
            string entityId,
            CancellationToken ct = default);

        #endregion

        #region FDP_ETC.2.2 - Unambiguous Association

        /// <summary>
        /// امضای داده‌های خروجی برای تضمین ارتباط ویژگی‌های امنیتی
        /// FDP_ETC.2.2: ارتباط بدون ابهام
        /// </summary>
        Task<string> SignExportDataAsync(
            object data,
            SecurityAttributes attributes,
            CancellationToken ct = default);

        /// <summary>
        /// تایید صحت امضای داده‌های خروجی
        /// </summary>
        Task<bool> VerifyExportSignatureAsync(
            string signature,
            object data,
            SecurityAttributes attributes,
            CancellationToken ct = default);

        #endregion

        #region FDP_ETC.2.4 - Additional Export Control Rules

        /// <summary>
        /// اعمال قوانین کنترل خروجی
        /// FDP_ETC.2.4: قوانین کنترل اضافی
        /// </summary>
        Task<ExportRuleResult> ApplyExportRulesAsync<T>(
            T data,
            ExportContext context,
            CancellationToken ct = default) where T : class;

        /// <summary>
        /// ماسک کردن داده‌های حساس
        /// </summary>
        Task<T> MaskSensitiveDataAsync<T>(
            T data,
            long userId,
            CancellationToken ct = default) where T : class;

        /// <summary>
        /// فیلتر کردن فیلدها بر اساس دسترسی کاربر
        /// </summary>
        Task<T> FilterFieldsByPermissionAsync<T>(
            T data,
            long userId,
            CancellationToken ct = default) where T : class;

        #endregion

        #region Settings & Rules Management

        /// <summary>
        /// دریافت تنظیمات خروجی داده‌ها
        /// </summary>
        Task<DataExportSettings> GetSettingsAsync(CancellationToken ct = default);

        /// <summary>
        /// به‌روزرسانی تنظیمات خروجی داده‌ها
        /// </summary>
        Task<bool> UpdateSettingsAsync(
            DataExportSettings settings,
            long updatedBy,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت لیست قوانین خروجی
        /// </summary>
        Task<IEnumerable<ExportRule>> GetExportRulesAsync(CancellationToken ct = default);

        /// <summary>
        /// ایجاد قانون خروجی جدید
        /// </summary>
        Task<ExportRule> CreateExportRuleAsync(
            ExportRule rule,
            long createdBy,
            CancellationToken ct = default);

        /// <summary>
        /// به‌روزرسانی قانون خروجی
        /// </summary>
        Task<bool> UpdateExportRuleAsync(
            ExportRule rule,
            long updatedBy,
            CancellationToken ct = default);

        /// <summary>
        /// حذف قانون خروجی
        /// </summary>
        Task<bool> DeleteExportRuleAsync(
            Guid ruleId,
            long deletedBy,
            CancellationToken ct = default);

        #endregion

        #region Masking Rules Management

        /// <summary>
        /// دریافت قوانین ماسک داده‌ها
        /// </summary>
        Task<IEnumerable<DataMaskingRule>> GetMaskingRulesAsync(CancellationToken ct = default);

        /// <summary>
        /// ایجاد قانون ماسک جدید
        /// </summary>
        Task<DataMaskingRule> CreateMaskingRuleAsync(
            DataMaskingRule rule,
            long createdBy,
            CancellationToken ct = default);

        /// <summary>
        /// به‌روزرسانی قانون ماسک
        /// </summary>
        Task<bool> UpdateMaskingRuleAsync(
            DataMaskingRule rule,
            long updatedBy,
            CancellationToken ct = default);

        /// <summary>
        /// حذف قانون ماسک
        /// </summary>
        Task<bool> DeleteMaskingRuleAsync(
            Guid ruleId,
            long deletedBy,
            CancellationToken ct = default);

        #endregion

        #region Sensitivity Levels Management

        /// <summary>
        /// دریافت سطوح حساسیت تعریف شده
        /// </summary>
        Task<IEnumerable<SensitivityLevel>> GetSensitivityLevelsAsync(CancellationToken ct = default);

        /// <summary>
        /// دریافت سطح حساسیت یک Entity
        /// </summary>
        Task<SensitivityLevel> GetEntitySensitivityLevelAsync(
            string entityType,
            string entityId,
            CancellationToken ct = default);

        #endregion

        #region Export Audit Log

        /// <summary>
        /// ثبت لاگ خروجی داده
        /// </summary>
        Task LogExportAsync(
            ExportAuditEntry entry,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت لاگ خروجی داده‌ها
        /// </summary>
        Task<IEnumerable<ExportAuditEntry>> GetExportAuditLogAsync(
            ExportAuditFilter filter,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت آمار خروجی داده‌ها
        /// </summary>
        Task<ExportStatistics> GetExportStatisticsAsync(CancellationToken ct = default);

        #endregion
    }

    #region DTOs

    /// <summary>
    /// پاسخ خروجی امن با ویژگی‌های امنیتی
    /// FDP_ETC.2.1 & FDP_ETC.2.2
    /// </summary>
    public class SecureExportResponse<T> where T : class
    {
        /// <summary>
        /// داده‌های خروجی
        /// </summary>
        public T Data { get; set; } = default!;

        /// <summary>
        /// ویژگی‌های امنیتی مرتبط با داده
        /// </summary>
        public SecurityAttributes SecurityContext { get; set; } = new SecurityAttributes();

        /// <summary>
        /// متادیتای خروجی
        /// </summary>
        public ExportMetadata Metadata { get; set; } = new ExportMetadata();

        /// <summary>
        /// امضای دیجیتال (FDP_ETC.2.2)
        /// </summary>
        public string? Signature { get; set; }

        /// <summary>
        /// قوانین اعمال شده (FDP_ETC.2.4)
        /// </summary>
        public List<string> AppliedRules { get; set; } = new List<string>();
    }

    /// <summary>
    /// ویژگی‌های امنیتی داده
    /// </summary>
    public class SecurityAttributes
    {
        /// <summary>
        /// سطح حساسیت داده
        /// </summary>
        public string SensitivityLevel { get; set; } = "Internal";

        /// <summary>
        /// طبقه‌بندی داده
        /// </summary>
        public string Classification { get; set; } = "Standard";

        /// <summary>
        /// مالک داده
        /// </summary>
        public string DataOwner { get; set; } = string.Empty;

        /// <summary>
        /// شناسه مالک
        /// </summary>
        public long? DataOwnerId { get; set; }

        /// <summary>
        /// هش صحت داده
        /// </summary>
        public string IntegrityHash { get; set; } = string.Empty;

        /// <summary>
        /// الگوریتم هش
        /// </summary>
        public string HashAlgorithm { get; set; } = "HMAC-SHA256";

        /// <summary>
        /// زمان ایجاد داده
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// زمان آخرین تغییر
        /// </summary>
        public DateTime? LastModifiedAt { get; set; }

        /// <summary>
        /// برچسب‌های امنیتی
        /// </summary>
        public List<string> SecurityLabels { get; set; } = new List<string>();

        /// <summary>
        /// محدودیت‌های دسترسی
        /// </summary>
        public List<string> AccessRestrictions { get; set; } = new List<string>();
    }

    /// <summary>
    /// متادیتای خروجی
    /// </summary>
    public class ExportMetadata
    {
        /// <summary>
        /// شناسه درخواست
        /// </summary>
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// زمان خروجی
        /// </summary>
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// شناسه کاربر صادرکننده
        /// </summary>
        public long? ExportedBy { get; set; }

        /// <summary>
        /// نام کاربر صادرکننده
        /// </summary>
        public string? ExportedByName { get; set; }

        /// <summary>
        /// IP Address صادرکننده
        /// </summary>
        public string? ExporterIpAddress { get; set; }

        /// <summary>
        /// نسخه API
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// فرمت خروجی
        /// </summary>
        public string Format { get; set; } = "JSON";

        /// <summary>
        /// تعداد رکوردها
        /// </summary>
        public int? RecordCount { get; set; }
    }

    /// <summary>
    /// Context خروجی داده
    /// </summary>
    public class ExportContext
    {
        /// <summary>
        /// نوع Entity
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// شناسه Entity
        /// </summary>
        public string? EntityId { get; set; }

        /// <summary>
        /// شناسه کاربر درخواست‌کننده
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// نام کاربر درخواست‌کننده
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// IP Address درخواست‌کننده
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User Agent
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// مسیر درخواست
        /// </summary>
        public string? RequestPath { get; set; }

        /// <summary>
        /// شناسه درخواست
        /// </summary>
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// فرمت خروجی درخواستی
        /// </summary>
        public string RequestedFormat { get; set; } = "JSON";
    }

    /// <summary>
    /// نتیجه اعمال قوانین خروجی
    /// </summary>
    public class ExportRuleResult
    {
        /// <summary>
        /// آیا خروجی مجاز است
        /// </summary>
        public bool IsAllowed { get; set; } = true;

        /// <summary>
        /// دلیل عدم مجوز
        /// </summary>
        public string? DenialReason { get; set; }

        /// <summary>
        /// قوانین اعمال شده
        /// </summary>
        public List<string> AppliedRules { get; set; } = new List<string>();

        /// <summary>
        /// هشدارها
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// آیا نیاز به ماسک داده‌ها است
        /// </summary>
        public bool RequiresMasking { get; set; }

        /// <summary>
        /// فیلدهایی که باید ماسک شوند
        /// </summary>
        public List<string> FieldsToMask { get; set; } = new List<string>();

        /// <summary>
        /// فیلدهایی که باید حذف شوند
        /// </summary>
        public List<string> FieldsToRemove { get; set; } = new List<string>();
    }

    /// <summary>
    /// تنظیمات خروجی داده‌ها
    /// </summary>
    public class DataExportSettings
    {
        /// <summary>
        /// آیا خروجی داده فعال است
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// آیا امضای دیجیتال فعال است
        /// </summary>
        public bool EnableDigitalSignature { get; set; } = true;

        /// <summary>
        /// آیا ماسک داده‌ها فعال است
        /// </summary>
        public bool EnableDataMasking { get; set; } = true;

        /// <summary>
        /// آیا لاگ خروجی فعال است
        /// </summary>
        public bool EnableExportAudit { get; set; } = true;

        /// <summary>
        /// حداکثر تعداد رکورد در هر خروجی
        /// </summary>
        public int MaxRecordsPerExport { get; set; } = 10000;

        /// <summary>
        /// حداکثر حجم خروجی (بایت)
        /// </summary>
        public long MaxExportSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB

        /// <summary>
        /// فرمت‌های مجاز خروجی
        /// </summary>
        public List<string> AllowedFormats { get; set; } = new List<string> { "JSON"};

        /// <summary>
        /// سطح حساسیت پیش‌فرض
        /// </summary>
        public string DefaultSensitivityLevel { get; set; } = "Internal";

        /// <summary>
        /// الگوریتم امضا
        /// </summary>
        public string SignatureAlgorithm { get; set; } = "HMAC-SHA256";

        /// <summary>
        /// مدت زمان نگهداری لاگ (روز)
        /// </summary>
        public int AuditLogRetentionDays { get; set; } = 365;
    }

    /// <summary>
    /// قانون خروجی داده
    /// </summary>
    public class ExportRule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ExportRuleType RuleType { get; set; }
        public string EntityType { get; set; } = "*";
        public string Condition { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int Priority { get; set; } = 100;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public long? UpdatedBy { get; set; }
    }

    /// <summary>
    /// نوع قانون خروجی
    /// </summary>
    public enum ExportRuleType
    {
        /// <summary>
        /// محدودیت حجم
        /// </summary>
        VolumeLimit = 1,

        /// <summary>
        /// محدودیت تعداد
        /// </summary>
        RecordLimit = 2,

        /// <summary>
        /// محدودیت زمانی
        /// </summary>
        TimeRestriction = 3,

        /// <summary>
        /// فیلتر بر اساس نقش
        /// </summary>
        RoleBasedFilter = 4,

        /// <summary>
        /// فیلتر بر اساس سطح حساسیت
        /// </summary>
        SensitivityFilter = 5,

        /// <summary>
        /// ماسک داده
        /// </summary>
        DataMasking = 6,

        /// <summary>
        /// حذف فیلد
        /// </summary>
        FieldRemoval = 7,

        /// <summary>
        /// تایید گیرنده
        /// </summary>
        RecipientVerification = 8
    }

    /// <summary>
    /// قانون ماسک داده
    /// </summary>
    public class DataMaskingRule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EntityType { get; set; } = "*";
        public string FieldName { get; set; } = string.Empty;
        public MaskingType MaskingType { get; set; }
        public string MaskPattern { get; set; } = "****";
        public int VisibleCharsStart { get; set; } = 0;
        public int VisibleCharsEnd { get; set; } = 4;
        public string? ExcludePermissions { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
    }

    /// <summary>
    /// نوع ماسک
    /// </summary>
    public enum MaskingType
    {
        /// <summary>
        /// جایگزینی کامل
        /// </summary>
        FullReplacement = 1,

        /// <summary>
        /// ماسک جزئی (نمایش تعدادی کاراکتر)
        /// </summary>
        PartialMask = 2,

        /// <summary>
        /// هش
        /// </summary>
        Hash = 3,

        /// <summary>
        /// رمزنگاری
        /// </summary>
        Encryption = 4,

        /// <summary>
        /// ایمیل
        /// </summary>
        EmailMask = 5,

        /// <summary>
        /// شماره تلفن
        /// </summary>
        PhoneMask = 6,

        /// <summary>
        /// کد ملی
        /// </summary>
        NationalIdMask = 7,

        /// <summary>
        /// شماره کارت
        /// </summary>
        CardNumberMask = 8
    }

    /// <summary>
    /// سطح حساسیت داده
    /// </summary>
    public class SensitivityLevel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Color { get; set; } = "#808080";
        public bool RequiresEncryption { get; set; }
        public bool RequiresAudit { get; set; } = true;
        public bool RequiresApproval { get; set; }
        public string? RequiredPermission { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// ورودی لاگ خروجی داده
    /// </summary>
    public class ExportAuditEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public int RecordCount { get; set; }
        public long DataSizeBytes { get; set; }
        public string Format { get; set; } = "JSON";
        public string SensitivityLevel { get; set; } = string.Empty;
        public bool WasMasked { get; set; }
        public List<string> AppliedRules { get; set; } = new List<string>();
        public string RequestPath { get; set; } = string.Empty;
        public string? RequestId { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// فیلتر لاگ خروجی
    /// </summary>
    public class ExportAuditFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? UserId { get; set; }
        public string? EntityType { get; set; }
        public string? SensitivityLevel { get; set; }
        public bool? WasMasked { get; set; }
        public bool? IsSuccess { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// آمار خروجی داده‌ها
    /// </summary>
    public class ExportStatistics
    {
        public long TotalExports { get; set; }
        public long TodayExports { get; set; }
        public long Last7DaysExports { get; set; }
        public long Last30DaysExports { get; set; }
        public long TotalRecordsExported { get; set; }
        public long TotalDataSizeBytes { get; set; }
        public Dictionary<string, long> ExportsByEntityType { get; set; } = new Dictionary<string, long>();
        public Dictionary<string, long> ExportsBySensitivityLevel { get; set; } = new Dictionary<string, long>();
        public Dictionary<string, long> ExportsByUser { get; set; } = new Dictionary<string, long>();
        public int MaskedExportsCount { get; set; }
        public int FailedExportsCount { get; set; }
    }

    #endregion
}

