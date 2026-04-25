using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس مدیریت تنظیمات شعبه از دیتابیس
    /// تنظیمات با Caching مدیریت می‌شوند برای کارایی بهتر
    /// </summary>
    public interface IShobeSettingsService
    {
        /// <summary>
        /// دریافت تنظیمات SMS
        /// </summary>
        /// <param name="shobePublicId">شناسه شعبه (اختیاری) - اگر null باشد، تنظیمات عمومی برگردانده می‌شود</param>
        /// <param name="ct">توکن لغو</param>
        /// <returns>تنظیمات SMS</returns>
        Task<SmsSettings> GetSmsSettingsAsync(System.Guid? shobePublicId = null, CancellationToken ct = default);

        /// <summary>
        /// دریافت مقدار یک تنظیم SMS خاص
        /// </summary>
        /// <param name="settingKey">کلید تنظیم (مثلاً "BaseUrl", "ApiKey")</param>
        /// <param name="defaultValue">مقدار پیش‌فرض در صورت عدم وجود</param>
        /// <param name="shobePublicId">شناسه شعبه (اختیاری)</param>
        /// <param name="ct">توکن لغو</param>
        /// <returns>مقدار تنظیم</returns>
        Task<string> GetSmsSettingValueAsync(string settingKey, string defaultValue, System.Guid? shobePublicId = null, CancellationToken ct = default);

        /// <summary>
        /// دریافت مقدار یک تنظیم SMS عددی
        /// </summary>
        /// <param name="settingKey">کلید تنظیم (مثلاً "OtpLength", "OtpExpirySeconds")</param>
        /// <param name="defaultValue">مقدار پیش‌فرض در صورت عدم وجود</param>
        /// <param name="shobePublicId">شناسه شعبه (اختیاری)</param>
        /// <param name="ct">توکن لغو</param>
        /// <returns>مقدار تنظیم</returns>
        Task<int> GetSmsIntSettingValueAsync(string settingKey, int defaultValue, System.Guid? shobePublicId = null, CancellationToken ct = default);

        /// <summary>
        /// پاکسازی کش تنظیمات SMS
        /// </summary>
        /// <param name="shobePublicId">شناسه شعبه (اختیاری)</param>
        void InvalidateCache(System.Guid? shobePublicId = null);

        /// <summary>
        /// دریافت تنظیمات Attachment
        /// </summary>
        /// <param name="shobePublicId">شناسه شعبه (اختیاری) - اگر null باشد، تنظیمات عمومی برگردانده می‌شود</param>
        /// <param name="ct">توکن لغو</param>
        /// <returns>تنظیمات Attachment</returns>
        Task<AttachmentSettingsDto> GetAttachmentSettingsAsync(System.Guid? shobePublicId = null, CancellationToken ct = default);

        /// <summary>
        /// ذخیره تنظیمات Attachment
        /// </summary>
        /// <param name="settings">تنظیمات جدید</param>
        /// <param name="shobePublicId">شناسه شعبه (اختیاری)</param>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="ct">توکن لغو</param>
        Task<bool> SaveAttachmentSettingsAsync(AttachmentSettingsDto settings, System.Guid? shobePublicId = null, long? userId = null, CancellationToken ct = default);

        /// <summary>
        /// پاکسازی کش تنظیمات Attachment
        /// </summary>
        /// <param name="shobePublicId">شناسه شعبه (اختیاری)</param>
        void InvalidateAttachmentCache(System.Guid? shobePublicId = null);
    }

    /// <summary>
    /// تنظیمات SMS
    /// </summary>
    public class SmsSettings
    {
        public string BaseUrl { get; set; } = "http://ssmss.ir/webservice/rest/sms_send";
        public string ApiKey { get; set; } = string.Empty;
        public string SenderNumber { get; set; } = string.Empty;
        public int OtpLength { get; set; } = 6;
        public int OtpExpirySeconds { get; set; } = 120;
        public string MessageTemplate { get; set; } = "کد تایید شما: {0}\nاین کد تا {1} ثانیه معتبر است.";
    }

    /// <summary>
    /// تنظیمات Attachment (قابل ذخیره در دیتابیس)
    /// </summary>
    public class AttachmentSettingsDto
    {
        /// <summary>
        /// مسیر ذخیره‌سازی فایل‌ها
        /// پیش‌فرض: wwwroot/attachments
        /// </summary>
        public string StoragePath { get; set; } = "wwwroot/attachments";

        /// <summary>
        /// حداکثر حجم فایل (مگابایت)
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 50;

        /// <summary>
        /// پسوندهای مجاز (جداشده با کاما)
        /// </summary>
        public string AllowedExtensions { get; set; } = ".pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png,.gif,.webp";

        /// <summary>
        /// فعال بودن بررسی Magic Bytes
        /// </summary>
        public bool ValidateMagicBytes { get; set; } = true;

        /// <summary>
        /// فعال بودن اسکن ویروس
        /// </summary>
        public bool EnableVirusScan { get; set; } = false;

        /// <summary>
        /// فعال بودن رمزنگاری فایل‌ها
        /// </summary>
        public bool EnableEncryption { get; set; } = false;

        /// <summary>
        /// حداکثر حجم فایل برای تصاویر پروفایل (مگابایت)
        /// </summary>
        public int MaxProfileImageSizeMB { get; set; } = 5;

        /// <summary>
        /// پسوندهای مجاز برای تصاویر
        /// </summary>
        public string AllowedImageExtensions { get; set; } = ".jpg,.jpeg,.png,.gif,.webp";

        /// <summary>
        /// آیا فایل‌ها در wwwroot ذخیره شوند (قابل دسترسی از وب)
        /// </summary>
        public bool UseWebRoot { get; set; } = true;

        /// <summary>
        /// دریافت لیست پسوندهای مجاز
        /// </summary>
        public string[] GetAllowedExtensionsArray()
        {
            return AllowedExtensions
                .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLowerInvariant())
                .ToArray();
        }

        /// <summary>
        /// دریافت لیست پسوندهای مجاز تصاویر
        /// </summary>
        public string[] GetAllowedImageExtensionsArray()
        {
            return AllowedImageExtensions
                .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLowerInvariant())
                .ToArray();
        }

        /// <summary>
        /// حداکثر حجم فایل (بایت)
        /// </summary>
        public long MaxFileSizeBytes => MaxFileSizeMB * 1024L * 1024L;

        /// <summary>
        /// حداکثر حجم تصویر پروفایل (بایت)
        /// </summary>
        public long MaxProfileImageSizeBytes => MaxProfileImageSizeMB * 1024L * 1024L;
    }
}
