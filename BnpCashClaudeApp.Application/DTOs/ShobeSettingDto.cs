using System;

namespace BnpCashClaudeApp.Application.DTOs
{
    /// <summary>
    /// DTO تنظیمات شعبه برای نمایش اطلاعات
    /// </summary>
    public class ShobeSettingDto
    {
        /// <summary>
        /// شناسه عمومی (GUID) - برای استفاده در API
        /// </summary>
        public Guid PublicId { get; set; }

        /// <summary>
        /// شناسه شعبه (PublicId) - nullable برای تنظیمات عمومی
        /// </summary>
        public Guid? ShobePublicId { get; set; }

        /// <summary>
        /// نام شعبه (در صورت وجود)
        /// </summary>
        public string? ShobeTitle { get; set; }

        /// <summary>
        /// کلید تنظیم
        /// </summary>
        public string SettingKey { get; set; }

        /// <summary>
        /// نام تنظیم برای نمایش
        /// </summary>
        public string SettingName { get; set; }

        /// <summary>
        /// توضیحات تنظیم
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// مقدار تنظیم به صورت JSON
        /// </summary>
        public string SettingValue { get; set; }

        /// <summary>
        /// نوع تنظیم
        /// </summary>
        public int SettingType { get; set; }

        /// <summary>
        /// آیا این تنظیم فعال است
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// آیا این تنظیم قابل ویرایش توسط ادمین است
        /// </summary>
        public bool IsEditable { get; set; }

        /// <summary>
        /// ترتیب نمایش
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// تاریخ ایجاد رکورد (شمسی)
        /// </summary>
        public string ZamanInsert { get; set; }

        /// <summary>
        /// تاریخ آخرین ویرایش (شمسی)
        /// </summary>
        public string? ZamanLastEdit { get; set; }
    }

    /// <summary>
    /// DTO برای ایجاد تنظیمات شعبه جدید
    /// </summary>
    public class CreateShobeSettingDto
    {
        /// <summary>
        /// شناسه شعبه (PublicId) - nullable برای تنظیمات عمومی
        /// </summary>
        public Guid? ShobePublicId { get; set; }

        /// <summary>
        /// کلید تنظیم
        /// </summary>
        public string SettingKey { get; set; }

        /// <summary>
        /// نام تنظیم برای نمایش
        /// </summary>
        public string SettingName { get; set; }

        /// <summary>
        /// توضیحات تنظیم
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// مقدار تنظیم به صورت JSON
        /// </summary>
        public string SettingValue { get; set; } = "{}";

        /// <summary>
        /// نوع تنظیم
        /// </summary>
        public int SettingType { get; set; }

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
    /// DTO برای ویرایش تنظیمات شعبه
    /// </summary>
    public class UpdateShobeSettingDto
    {
        /// <summary>
        /// نام تنظیم برای نمایش
        /// </summary>
        public string SettingName { get; set; }

        /// <summary>
        /// توضیحات تنظیم
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// مقدار تنظیم به صورت JSON
        /// </summary>
        public string SettingValue { get; set; }

        /// <summary>
        /// نوع تنظیم
        /// </summary>
        public int SettingType { get; set; }

        /// <summary>
        /// آیا این تنظیم فعال است
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// آیا این تنظیم قابل ویرایش توسط ادمین است
        /// </summary>
        public bool IsEditable { get; set; }

        /// <summary>
        /// ترتیب نمایش
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}
