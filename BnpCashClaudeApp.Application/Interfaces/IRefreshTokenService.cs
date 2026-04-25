using BnpCashClaudeApp.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس مدیریت Refresh Token
    /// ============================================
    /// هدف: تمدید خودکار Access Token بدون نیاز به لاگین مجدد
    /// امنیت: Rotation + Reuse Detection
    /// ============================================
    /// </summary>
    public interface IRefreshTokenService
    {
        /// <summary>
        /// ایجاد Refresh Token جدید برای کاربر
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="ipAddress">IP Address کاربر</param>
        /// <param name="userAgent">User Agent مرورگر</param>
        /// <param name="operatingSystem">سیستم عامل</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>مقدار توکن (Base64)</returns>
        Task<string> GenerateRefreshTokenAsync(
            long userId,
            string ipAddress,
            string userAgent,
            string operatingSystem,
            CancellationToken ct = default);

        /// <summary>
        /// تایید و استفاده از Refresh Token
        /// Grace Period: اگر توکن در بازه زمانی کوتاه پس از استفاده مجدداً ارسال شود
        /// (مثلاً Race Condition بین تب‌ها)، به جای Revoke همه توکن‌ها، فقط درخواست رد می‌شود
        /// </summary>
        /// <param name="token">مقدار Refresh Token</param>
        /// <param name="ipAddress">IP Address کاربر</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>نتیجه اعتبارسنجی شامل وضعیت دقیق و UserId</returns>
        Task<RefreshTokenValidationResult> ValidateAndUseRefreshTokenAsync(
            string token,
            string ipAddress,
            CancellationToken ct = default);

        /// <summary>
        /// باطل کردن تمام Refresh Tokenهای کاربر
        /// استفاده: خروج از همه دستگاه‌ها، تغییر رمز عبور
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="reason">دلیل باطل کردن</param>
        /// <param name="ct">CancellationToken</param>
        Task RevokeAllUserTokensAsync(
            long userId,
            string reason,
            CancellationToken ct = default);

        /// <summary>
        /// باطل کردن یک Refresh Token خاص
        /// استفاده: خروج از یک دستگاه خاص
        /// </summary>
        /// <param name="token">مقدار Refresh Token</param>
        /// <param name="reason">دلیل باطل کردن</param>
        /// <param name="ct">CancellationToken</param>
        Task RevokeTokenAsync(
            string token,
            string reason,
            CancellationToken ct = default);

        /// <summary>
        /// پاک کردن Refresh Tokenهای منقضی شده و استفاده شده
        /// استفاده: Background Service هر 24 ساعت
        /// </summary>
        /// <param name="ct">CancellationToken</param>
        Task CleanupExpiredTokensAsync(CancellationToken ct = default);

        /// <summary>
        /// دریافت لیست نشست‌های فعال کاربر
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="currentToken">توکن فعلی کاربر (برای مشخص کردن نشست جاری)</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>لیست نشست‌های فعال</returns>
        Task<List<UserSessionDto>> GetUserActiveSessionsAsync(
            long userId,
            string? currentToken = null,
            CancellationToken ct = default);

        /// <summary>
        /// باطل کردن یک نشست خاص با شناسه عمومی
        /// </summary>
        /// <param name="userId">شناسه کاربر (برای امنیت)</param>
        /// <param name="sessionPublicId">شناسه عمومی نشست (RefreshToken PublicId)</param>
        /// <param name="reason">دلیل باطل کردن</param>
        /// <param name="ct">CancellationToken</param>
        Task RevokeSessionAsync(
            long userId,
            Guid sessionPublicId,
            string reason,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت تعداد نشست‌های فعال کاربر
        /// الزام FTA_MCS.1.1: محدودیت نشست همزمان
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>تعداد نشست‌های فعال</returns>
        Task<int> GetUserActiveSessionsCountAsync(
            long userId,
            CancellationToken ct = default);

        /// <summary>
        /// خاتمه قدیمی‌ترین نشست کاربر
        /// الزام FDP_ACF.1.4: رفتار ConcurrentSessionAction.TerminateOldest
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="ct">CancellationToken</param>
        Task TerminateOldestSessionAsync(
            long userId,
            CancellationToken ct = default);

        /// <summary>
        /// باطل کردن تمام نشست‌های کاربر (بدون دلیل)
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="ct">CancellationToken</param>
        Task RevokeAllUserTokensAsync(
            long userId,
            CancellationToken ct = default);
    }
}
