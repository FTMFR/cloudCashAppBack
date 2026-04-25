using BnpCashClaudeApp.Domain.Common;

namespace BnpCashClaudeApp.Domain.Entities.SecuritySubsystem
{
    /// <summary>
    /// موجودیت تاریخچه رمز عبور
    /// ============================================
    /// الزام FDP (User Data Protection) از ISO 15408
    /// ذخیره رمزهای عبور قبلی برای جلوگیری از استفاده مجدد
    /// ============================================
    /// </summary>
    public class PasswordHistory : BaseEntity
    {
        /// <summary>
        /// شناسه داخلی کاربر (long)
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Hash رمز عبور
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// تاریخ تنظیم رمز عبور (شمسی)
        /// </summary>
        public string? SetAt { get; set; }

        /// <summary>
        /// آدرس IP هنگام تغییر رمز عبور
        /// </summary>
        public string? IpAddress { get; set; }
    }
}
