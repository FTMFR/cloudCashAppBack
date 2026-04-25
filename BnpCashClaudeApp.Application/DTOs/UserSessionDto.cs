using System;

namespace BnpCashClaudeApp.Application.DTOs
{
    /// <summary>
    /// DTO برای نمایش نشست‌های فعال کاربر
    /// </summary>
    public class UserSessionDto
    {
        /// <summary>
        /// شناسه عمومی نشست (GUID) - برای استفاده در API
        /// </summary>
        public Guid PublicId { get; set; }

        /// <summary>
        /// IP Address کاربر
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// User Agent مرورگر
        /// </summary>
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// سیستم عامل
        /// </summary>
        public string? OperatingSystem { get; set; }

        /// <summary>
        /// زمان ایجاد نشست
        /// </summary>
        public string CreatedAt { get; set; } = string.Empty;

        /// <summary>
        /// زمان انقضا
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// آیا این نشست فعلی است؟
        /// </summary>
        public bool IsCurrentSession { get; set; }

        /// <summary>
        /// آیا این آخرین لاگین کاربر است؟
        /// </summary>
        public bool IsLastLogin { get; set; }

        /// <summary>
        /// نام مرورگر (استخراج شده از UserAgent)
        /// </summary>
        public string BrowserName { get; set; } = string.Empty;

        /// <summary>
        /// نوع دستگاه (Desktop, Mobile, Tablet)
        /// </summary>
        public string DeviceType { get; set; } = string.Empty;
    }
}
