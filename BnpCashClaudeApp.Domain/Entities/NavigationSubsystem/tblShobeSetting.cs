using BnpCashClaudeApp.Domain.Common;

namespace BnpCashClaudeApp.Domain.Entities.NavigationSubsystem
{
    /// <summary>
    /// تنظیمات شعبه
    /// ذخیره تنظیمات مربوط به شعب در دیتابیس
    /// </summary>
    public class tblShobeSetting : BaseEntity
    {
        /// <summary>
        /// شناسه شعبه (Foreign Key) - nullable برای تنظیمات عمومی
        /// اگر null باشد، تنظیمات عمومی است و برای همه شعب اعمال می‌شود
        /// </summary>
        public long? TblShobeId { get; set; }

        /// <summary>
        /// کلید تنظیم - مثلاً "BranchHours" یا "BranchCapacity"
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
        public ShobeSettingType SettingType { get; set; }

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

        // ============================================
        // Navigation Properties
        // ============================================

        /// <summary>
        /// شعبه مرتبط (در صورت وجود)
        /// </summary>
        public virtual tblShobe? TblShobe { get; set; }
    }

    /// <summary>
    /// انواع تنظیمات شعبه
    /// </summary>
    public enum ShobeSettingType
    {
        /// <summary>
        /// تنظیمات ساعات کاری
        /// </summary>
        WorkingHours = 1,

        /// <summary>
        /// تنظیمات ظرفیت
        /// </summary>
        Capacity = 2,

        /// <summary>
        /// تنظیمات خدمات
        /// </summary>
        Services = 3,

        /// <summary>
        /// تنظیمات ارتباطی
        /// </summary>
        Communication = 4,

        /// <summary>
        /// تنظیمات SMS
        /// </summary>
        Sms = 5,

        /// <summary>
        /// تنظیمات فایل پیوست
        /// </summary>
        Attachment = 6,

        /// <summary>
        /// تنظیمات خروجی داده‌ها (FDP_ETC.2)
        /// </summary>
        DataExport = 7,

        /// <summary>
        /// سایر تنظیمات
        /// </summary>
        Other = 99
    }
}
