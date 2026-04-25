using System.Linq;

namespace BnpCashClaudeApp.Application.Settings
{
    /// <summary>
    /// تنظیمات سرویس فایل پیوست
    /// ============================================
    /// قابل تنظیم از appsettings.json
    /// ============================================
    /// </summary>
    public class AttachmentSettings
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

        // ============================================
        // متدهای کمکی
        // ============================================

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

        /// <summary>
        /// بررسی مجاز بودن پسوند
        /// </summary>
        public bool IsExtensionAllowed(string extension)
        {
            var ext = extension.TrimStart('.').ToLowerInvariant();
            return GetAllowedExtensionsArray().Any(e => e.TrimStart('.') == ext);
        }

        /// <summary>
        /// بررسی مجاز بودن پسوند تصویر
        /// </summary>
        public bool IsImageExtensionAllowed(string extension)
        {
            var ext = extension.TrimStart('.').ToLowerInvariant();
            return GetAllowedImageExtensionsArray().Any(e => e.TrimStart('.') == ext);
        }
    }
}
