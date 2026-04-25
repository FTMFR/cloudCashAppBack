using BnpCashClaudeApp.Domain.Common;

namespace BnpCashClaudeApp.Domain.Entities.SecuritySubsystem
{
    /// <summary>
    /// تنظیمات امنیتی سیستم
    /// ذخیره تنظیمات AccountLockout و PasswordPolicy در دیتابیس
    /// پیاده‌سازی الزامات پروفایل حفاظتی (ISO 15408)
    /// </summary>
    public class SecuritySetting : BaseEntity
    {
        /// <summary>
        /// کلید تنظیم - مثلاً "AccountLockout" یا "PasswordPolicy"
        /// </summary>
        public string SettingKey { get; set; } = string.Empty;

        /// <summary>
        /// نام تنظیم برای نمایش
        /// </summary>
        public string SettingName { get; set; } = string.Empty;

        /// <summary>
        /// توضیحات تنظیم
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// مقدار تنظیم به صورت JSON
        /// </summary>
        public string SettingValue { get; set; } = "{}";

        /// <summary>
        /// نوع تنظیم برای دسته‌بندی
        /// </summary>
        public SecuritySettingType SettingType { get; set; }

        /// <summary>
        /// آیا این تنظیم فعال است
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// آیا این تنظیم قابل ویرایش توسط ادمین است
        /// </summary>
        public bool IsEditable { get; set; } = true;

        /// <summary>
        /// ترتیب نمایش
        /// </summary>
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// انواع تنظیمات امنیتی
    /// </summary>
    public enum SecuritySettingType
    {
        /// <summary>
        /// تنظیمات قفل حساب کاربری
        /// </summary>
        AccountLockout = 1,

        /// <summary>
        /// تنظیمات سیاست رمز عبور
        /// </summary>
        PasswordPolicy = 2,

        /// <summary>
        /// تنظیمات احراز هویت
        /// </summary>
        Authentication = 3,

        /// <summary>
        /// تنظیمات جلسه کاربری
        /// </summary>
        Session = 4,

        /// <summary>
        /// تنظیمات CAPTCHA
        /// </summary>
        Captcha = 5,

        /// <summary>
        /// تنظیمات احراز هویت دو مرحله‌ای (MFA)
        /// الزام FIA_UAU.5 - سازوکار احرازهویت چندگانه
        /// </summary>
        Mfa = 6,

        /// <summary>
        /// تنظیمات کنترل دسترسی مبتنی بر Context
        /// الزام FDP_ACF.1.4 - عملیات کنترل دسترسی 4
        /// </summary>
        ContextAccessControl = 7,

        /// <summary>
        /// تنظیمات حفاظت از داده‌های ممیزی
        /// الزامات FAU_STG.3.1 و FAU_STG.4.1
        /// </summary>
        AuditLogProtection = 8,

        /// <summary>
        /// تنظیمات خروجی داده‌ها با ویژگی‌های امنیتی
        /// الزامات FDP_ETC.2.1, FDP_ETC.2.2, FDP_ETC.2.4
        /// </summary>
        DataProtection = 9,

        /// <summary>
        /// سایر تنظیمات امنیتی
        /// </summary>
        Other = 99
    }
}

