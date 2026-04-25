using BnpCashClaudeApp.Domain.Common;
using System;

namespace BnpCashClaudeApp.Domain.Entities.SecuritySubsystem
{
    /// <summary>
    /// موجودیت Refresh Token
    /// ============================================
    /// استفاده: تمدید خودکار Access Token بدون نیاز به لاگین مجدد
    /// مدت اعتبار: 7-30 روز (بر اساس تنظیمات)
    /// ============================================
    /// </summary>
    public class RefreshToken : BaseEntity
    {
        /// <summary>
        /// شناسه داخلی کاربر (long)
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// مقدار توکن (رمزنگاری شده)
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// تاریخ انقضا (UTC)
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// آیا باطل شده است؟
        /// </summary>
        public bool IsRevoked { get; set; } = false;

        /// <summary>
        /// تاریخ باطل شدن (UTC)
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// دلیل باطل شدن
        /// مثال: "User logout", "Password changed", "Security policy"
        /// </summary>
        public string? RevokedReason { get; set; }

        /// <summary>
        /// IP Address کاربر هنگام ایجاد
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// User Agent مرورگر
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// سیستم عامل
        /// </summary>
        public string? OperatingSystem { get; set; }

        /// <summary>
        /// توکنی که این توکن را جایگزین کرده (در صورت تمدید)
        /// </summary>
        public string? ReplacedByToken { get; set; }

        /// <summary>
        /// آیا استفاده شده است؟
        /// </summary>
        public bool IsUsed { get; set; } = false;

        /// <summary>
        /// تاریخ استفاده (UTC)
        /// </summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// بررسی اینکه توکن فعال است یا نه
        /// </summary>
        public bool IsActive => !IsRevoked && !IsUsed && ExpiresAt > DateTime.UtcNow;
    }
}
