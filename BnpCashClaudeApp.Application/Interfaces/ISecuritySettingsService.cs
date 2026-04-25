using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس مدیریت تنظیمات امنیتی از دیتابیس
    /// تنظیمات با Caching مدیریت می‌شوند برای کارایی بهتر
    /// </summary>
    public interface ISecuritySettingsService
    {
        /// <summary>
        /// دریافت تنظیمات قفل حساب کاربری
        /// </summary>
        Task<AccountLockoutSettings> GetAccountLockoutSettingsAsync(CancellationToken ct = default);

        /// <summary>
        /// دریافت تنظیمات سیاست رمز عبور
        /// </summary>
        Task<PasswordPolicySettings> GetPasswordPolicySettingsAsync(CancellationToken ct = default);

        /// <summary>
        /// دریافت تنظیمات CAPTCHA
        /// </summary>
        Task<CaptchaSettings> GetCaptchaSettingsAsync(CancellationToken ct = default);

        /// <summary>
        /// دریافت تنظیمات MFA
        /// </summary>
        Task<MfaSettings> GetMfaSettingsAsync(CancellationToken ct = default);

        /// <summary>
        /// ذخیره تنظیمات قفل حساب کاربری
        /// </summary>
        Task SaveAccountLockoutSettingsAsync(
            AccountLockoutSettings settings,
            long userId,
            CancellationToken ct = default);

        /// <summary>
        /// ذخیره تنظیمات سیاست رمز عبور
        /// </summary>
        Task SavePasswordPolicySettingsAsync(
            PasswordPolicySettings settings,
            long userId,
            CancellationToken ct = default);

        /// <summary>
        /// ذخیره تنظیمات CAPTCHA
        /// </summary>
        Task SaveCaptchaSettingsAsync(
            CaptchaSettings settings,
            long userId,
            CancellationToken ct = default);

        /// <summary>
        /// ذخیره تنظیمات MFA
        /// </summary>
        Task SaveMfaSettingsAsync(
            MfaSettings settings,
            long userId,
            CancellationToken ct = default);

        /// <summary>
        /// پاکسازی کش تنظیمات (برای بارگذاری مجدد از دیتابیس)
        /// </summary>
        void InvalidateCache();

        /// <summary>
        /// پاکسازی کش یک نوع تنظیم خاص
        /// </summary>
        void InvalidateCache(string settingKey);

        /// <summary>
        /// دریافت مقدار یک تنظیم عددی
        /// </summary>
        Task<int> GetIntSettingAsync(string settingKey, int defaultValue, CancellationToken ct = default);

        /// <summary>
        /// ذخیره مقدار یک تنظیم عددی
        /// </summary>
        Task SaveIntSettingAsync(string settingKey, int value, long userId, CancellationToken ct = default);

        /// <summary>
        /// دریافت تنظیمات کنترل دسترسی مبتنی بر Context
        /// الزام FDP_ACF.1.4 - عملیات کنترل دسترسی 4
        /// </summary>
        Task<ContextAccessControlSettings> GetContextAccessControlSettingsAsync(CancellationToken ct = default);

        /// <summary>
        /// ذخیره تنظیمات کنترل دسترسی مبتنی بر Context
        /// </summary>
        Task SaveContextAccessControlSettingsAsync(
            ContextAccessControlSettings settings,
            long userId,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت تنظیمات حفاظت از داده‌های ممیزی
        /// الزامات FAU_STG.3.1 و FAU_STG.4.1
        /// </summary>
        Task<AuditLogProtectionSettings> GetAuditLogProtectionSettingsAsync(CancellationToken ct = default);

        /// <summary>
        /// ذخیره تنظیمات حفاظت از داده‌های ممیزی
        /// </summary>
        Task SaveAuditLogProtectionSettingsAsync(
            AuditLogProtectionSettings settings,
            long userId,
            CancellationToken ct = default);
    }

    /// <summary>
    /// تنظیمات CAPTCHA
    /// الزام FIA_UAU.5 - لایه امنیتی اضافی برای MFA
    /// پیش‌فرض: غیرفعال - از جدول SecuritySettings خوانده می‌شود
    /// </summary>
    public class CaptchaSettings
    {
        /// <summary>
        /// آیا CAPTCHA فعال است؟
        /// پیش‌فرض: غیرفعال
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// تعداد کاراکترهای کد CAPTCHA
        /// </summary>
        public int CodeLength { get; set; } = 5;

        /// <summary>
        /// زمان انقضای CAPTCHA به دقیقه
        /// </summary>
        public int ExpiryMinutes { get; set; } = 2;

        /// <summary>
        /// تعداد خطوط نویز
        /// </summary>
        public int NoiseLineCount { get; set; } = 10;

        /// <summary>
        /// تعداد نقاط نویز
        /// </summary>
        public int NoiseDotCount { get; set; } = 50;

        /// <summary>
        /// عرض تصویر (پیکسل)
        /// </summary>
        public int ImageWidth { get; set; } = 130;

        /// <summary>
        /// ارتفاع تصویر (پیکسل)
        /// </summary>
        public int ImageHeight { get; set; } = 40;

        /// <summary>
        /// آیا در MFA نیاز به CAPTCHA دارد؟
        /// پیش‌فرض: غیرفعال
        /// </summary>
        public bool RequireOnMfa { get; set; } = false;
    }

    /// <summary>
    /// تنظیمات احراز هویت دو مرحله‌ای (MFA)
    /// الزام FIA_UAU.5 - سازوکار احرازهویت چندگانه
    /// </summary>
    public class MfaSettings
    {
        /// <summary>
        /// آیا MFA برای کل سیستم فعال است؟
        /// اگر غیرفعال باشد، هیچ کاربری نمی‌تواند از MFA استفاده کند
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// آیا MFA برای همه کاربران اجباری است؟
        /// اگر true باشد، تمام کاربران باید MFA را فعال کنند
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// طول کد OTP (پیامک)
        /// </summary>
        public int OtpLength { get; set; } = 6;

        /// <summary>
        /// زمان انقضای کد OTP به ثانیه
        /// </summary>
        public int OtpExpirySeconds { get; set; } = 120;

        /// <summary>
        /// تعداد کدهای بازیابی
        /// </summary>
        public int RecoveryCodesCount { get; set; } = 10;

        /// <summary>
        /// حداکثر تعداد تلاش ناموفق برای OTP
        /// </summary>
        public int MaxFailedOtpAttempts { get; set; } = 3;

        /// <summary>
        /// زمان قفل شدن پس از تلاش‌های ناموفق (دقیقه)
        /// </summary>
        public int LockoutDurationMinutes { get; set; } = 5;
    }

    /// <summary>
    /// تنظیمات کنترل دسترسی مبتنی بر Context
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF.1.4 از استاندارد ISO 15408
    /// کنترل دسترسی بر اساس: IP، زمان، مکان، نوع دستگاه
    /// ============================================
    /// </summary>
    public class ContextAccessControlSettings
    {
        /// <summary>
        /// آیا کنترل دسترسی مبتنی بر Context فعال است؟
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        #region IP-based Access Control

        /// <summary>
        /// آیا محدودیت IP فعال است؟
        /// </summary>
        public bool EnableIpRestriction { get; set; } = false;

        /// <summary>
        /// لیست IP های مجاز (Whitelist)
        /// فرمت: "192.168.1.1", "10.0.0.0/24"
        /// </summary>
        public List<string> AllowedIpAddresses { get; set; } = new List<string>();

        /// <summary>
        /// لیست IP های غیرمجاز (Blacklist)
        /// فرمت: "192.168.1.100", "10.0.0.0/8"
        /// </summary>
        public List<string> BlockedIpAddresses { get; set; } = new List<string>();

        /// <summary>
        /// حالت IP: Whitelist (فقط IP های مجاز) یا Blacklist (همه به جز IP های ممنوع)
        /// </summary>
        public IpRestrictionMode IpRestrictionMode { get; set; } = IpRestrictionMode.Blacklist;

        #endregion

        #region Time-based Access Control

        /// <summary>
        /// آیا محدودیت زمانی فعال است؟
        /// </summary>
        public bool EnableTimeRestriction { get; set; } = false;

        /// <summary>
        /// ساعت شروع دسترسی مجاز (فرمت: HH:mm)
        /// </summary>
        public string AllowedStartTime { get; set; } = "00:00";

        /// <summary>
        /// ساعت پایان دسترسی مجاز (فرمت: HH:mm)
        /// </summary>
        public string AllowedEndTime { get; set; } = "23:59";

        /// <summary>
        /// روزهای هفته مجاز برای دسترسی
        /// 0=یکشنبه، 1=دوشنبه، ..., 6=شنبه
        /// </summary>
        public List<int> AllowedDaysOfWeek { get; set; } = new List<int> { 0, 1, 2, 3, 4, 5, 6 };

        /// <summary>
        /// منطقه زمانی برای بررسی (پیش‌فرض: Asia/Tehran)
        /// </summary>
        public string TimeZoneId { get; set; } = "false";

        #endregion

        #region Geographic Access Control

        /// <summary>
        /// آیا محدودیت جغرافیایی فعال است؟
        /// </summary>
        public bool EnableGeoRestriction { get; set; } = false;

        /// <summary>
        /// کشورهای مجاز (کد ISO دو حرفی)
        /// مثال: "IR", "AE", "TR"
        /// </summary>
        public List<string> AllowedCountries { get; set; } = new List<string> { "IR" };

        /// <summary>
        /// کشورهای غیرمجاز (کد ISO دو حرفی)
        /// </summary>
        public List<string> BlockedCountries { get; set; } = new List<string>();

        #endregion

        #region Device-based Access Control

        /// <summary>
        /// آیا محدودیت نوع دستگاه فعال است؟
        /// </summary>
        public bool EnableDeviceRestriction { get; set; } = false;

        /// <summary>
        /// آیا دسترسی از دستگاه موبایل مجاز است؟
        /// </summary>
        public bool AllowMobileDevices { get; set; } = true;

        /// <summary>
        /// آیا دسترسی از دستگاه دسکتاپ مجاز است؟
        /// </summary>
        public bool AllowDesktopDevices { get; set; } = true;

        /// <summary>
        /// آیا دسترسی از تبلت مجاز است؟
        /// </summary>
        public bool AllowTabletDevices { get; set; } = true;

        /// <summary>
        /// User Agent های ممنوع (regex patterns)
        /// </summary>
        public List<string> BlockedUserAgentPatterns { get; set; } = new List<string>();

        #endregion

        #region Concurrent Session Control

        /// <summary>
        /// آیا محدودیت نشست همزمان فعال است؟
        /// </summary>
        public bool EnableConcurrentSessionLimit { get; set; } = true;

        /// <summary>
        /// حداکثر تعداد نشست همزمان برای هر کاربر
        /// </summary>
        public int MaxConcurrentSessions { get; set; } = 10;

        /// <summary>
        /// رفتار در صورت رسیدن به حداکثر نشست
        /// </summary>
        public ConcurrentSessionAction ConcurrentSessionAction { get; set; } = ConcurrentSessionAction.DenyNew;

        #endregion

        #region Risk-based Access Control

        /// <summary>
        /// آیا ارزیابی ریسک فعال است؟
        /// </summary>
        public bool EnableRiskAssessment { get; set; } = false;

        /// <summary>
        /// حداکثر امتیاز ریسک مجاز (0-100)
        /// </summary>
        public int MaxAllowedRiskScore { get; set; } = 70;

        /// <summary>
        /// آیا در ریسک بالا نیاز به MFA است؟
        /// </summary>
        public bool RequireMfaOnHighRisk { get; set; } = true;

        /// <summary>
        /// آستانه ریسک برای نیاز به MFA
        /// </summary>
        public int MfaRequiredRiskThreshold { get; set; } = 50;

        #endregion

        #region Audit Settings

        /// <summary>
        /// آیا رویدادهای رد شده ثبت شوند؟
        /// </summary>
        public bool LogDeniedAccess { get; set; } = true;

        /// <summary>
        /// آیا رویدادهای مجاز ثبت شوند؟
        /// </summary>
        public bool LogAllowedAccess { get; set; } = false;

        /// <summary>
        /// آیا هشدار برای دسترسی‌های مشکوک ارسال شود؟
        /// </summary>
        public bool AlertOnSuspiciousAccess { get; set; } = true;

        #endregion
    }

    /// <summary>
    /// حالت محدودیت IP
    /// </summary>
    public enum IpRestrictionMode
    {
        /// <summary>
        /// حالت Whitelist: فقط IP های مشخص شده مجازند
        /// </summary>
        Whitelist = 1,

        /// <summary>
        /// حالت Blacklist: همه مجازند به جز IP های ممنوع
        /// </summary>
        Blacklist = 2
    }

    /// <summary>
    /// رفتار در صورت رسیدن به حداکثر نشست همزمان
    /// </summary>
    public enum ConcurrentSessionAction
    {
        /// <summary>
        /// رد درخواست جدید
        /// </summary>
        DenyNew = 1,

        /// <summary>
        /// خاتمه قدیمی‌ترین نشست
        /// </summary>
        TerminateOldest = 2,

        /// <summary>
        /// خاتمه تمام نشست‌های قبلی
        /// </summary>
        TerminateAll = 3
    }

    /// <summary>
    /// تنظیمات حفاظت از داده‌های ممیزی
    /// ============================================
    /// پیاده‌سازی الزامات FAU_STG.3.1 و FAU_STG.4.1 از استاندارد ISO 15408
    /// FAU_STG.3.1: اقدامات لازم در زمان از دست رفتن داده ممیزی
    /// FAU_STG.4.1: پیشگیری از اتلاف و از بین رفتن داده ممیزی
    /// ============================================
    /// </summary>
    public class AuditLogProtectionSettings
    {
        #region General Settings

        /// <summary>
        /// آیا حفاظت از داده‌های ممیزی فعال است؟
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        #endregion

        #region FAU_STG.3.1 - Fallback & Alert Settings

        /// <summary>
        /// حداکثر تعداد تلاش مجدد برای ذخیره در دیتابیس
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// آیا ارسال هشدار در صورت شکست فعال است؟
        /// </summary>
        public bool EnableAlertOnFailure { get; set; } = true;

        /// <summary>
        /// ایمیل‌های مقصد برای ارسال هشدار (جدا شده با کاما)
        /// </summary>
        public string AlertEmailAddresses { get; set; } = string.Empty;

        /// <summary>
        /// شماره‌های موبایل برای ارسال پیامک هشدار (جدا شده با کاما)
        /// </summary>
        public string AlertSmsNumbers { get; set; } = string.Empty;

        #endregion

        #region FAU_STG.4.1 - Backup & Retention Settings

        /// <summary>
        /// تعداد روزهای نگهداری داده‌ها (Retention)
        /// </summary>
        public int RetentionDays { get; set; } = 365;

        /// <summary>
        /// تعداد روزهایی که پس از آن لاگ‌ها آرشیو می‌شوند
        /// </summary>
        public int ArchiveAfterDays { get; set; } = 90;

        /// <summary>
        /// فاصله زمانی پشتیبان‌گیری خودکار (ساعت)
        /// </summary>
        public int BackupIntervalHours { get; set; } = 24;

        /// <summary>
        /// فاصله زمانی بررسی سیاست نگهداری (ساعت)
        /// </summary>
        public int RetentionCheckIntervalHours { get; set; } = 24;

        /// <summary>
        /// فاصله زمانی بازیابی لاگ‌های Fallback (دقیقه)
        /// </summary>
        public int FallbackRecoveryIntervalMinutes { get; set; } = 5;

        /// <summary>
        /// فاصله زمانی بررسی سلامت سیستم (دقیقه)
        /// </summary>
        public int HealthCheckIntervalMinutes { get; set; } = 10;

        #endregion

        #region Directory Settings

        /// <summary>
        /// مسیر دایرکتوری Fallback (خالی = پیش‌فرض)
        /// </summary>
        public string FallbackDirectory { get; set; } = string.Empty;

        /// <summary>
        /// مسیر دایرکتوری پشتیبان‌ها (خالی = پیش‌فرض)
        /// </summary>
        public string BackupDirectory { get; set; } = string.Empty;

        /// <summary>
        /// مسیر دایرکتوری آرشیو (خالی = پیش‌فرض)
        /// </summary>
        public string ArchiveDirectory { get; set; } = string.Empty;

        #endregion
    }
}
