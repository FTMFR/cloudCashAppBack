using System;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس Blacklist توکن‌های JWT
    /// پیاده‌سازی الزامات پروفایل حفاظتی برنامه‌های کاربردی تحت شبکه (ISO 15408)
    /// </summary>
    public interface ITokenBlacklistService
    {
        /// <summary>
        /// افزودن توکن به لیست سیاه (Logout)
        /// </summary>
        /// <param name="token">توکن JWT</param>
        /// <param name="expirationTime">زمان انقضای توکن</param>
        /// <param name="username">نام کاربری</param>
        /// <param name="reason">دلیل Blacklist کردن</param>
        /// <param name="ct">توکن لغو عملیات</param>
        Task BlacklistTokenAsync(
            string token,
            DateTime expirationTime,
            string? username = null,
            string? reason = null,
            CancellationToken ct = default);

        /// <summary>
        /// بررسی اینکه آیا توکن در لیست سیاه است
        /// </summary>
        /// <param name="token">توکن JWT</param>
        /// <returns>آیا توکن Blacklist شده است</returns>
        Task<bool> IsTokenBlacklistedAsync(string token);

        /// <summary>
        /// باطل کردن تمام توکن‌های یک کاربر (Logout from all devices)
        /// </summary>
        /// <param name="username">نام کاربری</param>
        /// <param name="reason">دلیل باطل کردن</param>
        /// <param name="ct">توکن لغو عملیات</param>
        Task BlacklistAllUserTokensAsync(
            string username,
            string? reason = null,
            CancellationToken ct = default);

        /// <summary>
        /// پاکسازی توکن‌های منقضی شده از Blacklist
        /// </summary>
        /// <param name="ct">توکن لغو عملیات</param>
        Task CleanupExpiredTokensAsync(CancellationToken ct = default);
    }
}

