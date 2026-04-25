using System;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// نتیجه بررسی وضعیت قفل حساب
    /// </summary>
    public class LockoutStatus
    {
        /// <summary>
        /// آیا حساب قفل است
        /// </summary>
        public bool IsLockedOut { get; set; }

        /// <summary>
        /// تعداد تلاش‌های ناموفق
        /// </summary>
        public int FailedAttempts { get; set; }

        /// <summary>
        /// زمان پایان قفل (اگر قفل باشد)
        /// </summary>
        public DateTime? LockoutEndTime { get; set; }

        /// <summary>
        /// تعداد تلاش‌های باقی‌مانده تا قفل شدن
        /// </summary>
        public int RemainingAttempts { get; set; }

        /// <summary>
        /// زمان باقی‌مانده تا باز شدن قفل (به ثانیه)
        /// </summary>
        public int? RemainingLockoutSeconds { get; set; }
    }

    /// <summary>
    /// تنظیمات قفل حساب کاربری
    /// </summary>
    public class AccountLockoutSettings
    {
        /// <summary>
        /// حداکثر تعداد تلاش‌های ناموفق قبل از قفل شدن
        /// پیش‌فرض: 5 تلاش
        /// </summary>
        public int MaxFailedAttempts { get; set; } = 5;

        /// <summary>
        /// مدت زمان قفل حساب به دقیقه
        /// پیش‌فرض: 15 دقیقه
        /// </summary>
        public int LockoutDurationMinutes { get; set; } = 15;

        /// <summary>
        /// آیا قفل دائمی فعال است (نیاز به باز کردن توسط مدیر)
        /// </summary>
        public bool EnablePermanentLockout { get; set; } = false;

        /// <summary>
        /// تعداد تلاش‌های ناموفق برای قفل دائمی
        /// </summary>
        public int PermanentLockoutThreshold { get; set; } = 10;

        /// <summary>
        /// مدت زمان ریست شدن شمارنده تلاش‌های ناموفق (به دقیقه)
        /// </summary>
        public int FailedAttemptResetMinutes { get; set; } = 30;
    }

    /// <summary>
    /// سرویس قفل حساب کاربری
    /// پیاده‌سازی الزامات پروفایل حفاظتی برنامه‌های کاربردی تحت شبکه (ISO 15408)
    /// </summary>
    public interface IAccountLockoutService
    {
        /// <summary>
        /// بررسی وضعیت قفل حساب کاربر
        /// </summary>
        /// <param name="username">نام کاربری</param>
        /// <returns>وضعیت قفل</returns>
        Task<LockoutStatus> GetLockoutStatusAsync(string username);

        /// <summary>
        /// ثبت تلاش ورود ناموفق
        /// </summary>
        /// <param name="username">نام کاربری</param>
        /// <param name="ipAddress">آدرس IP</param>
        /// <param name="ct">توکن لغو عملیات</param>
        /// <returns>وضعیت جدید قفل</returns>
        Task<LockoutStatus> RecordFailedAttemptAsync(
            string username, 
            string? ipAddress = null,
            CancellationToken ct = default);

        /// <summary>
        /// ریست کردن شمارنده تلاش‌های ناموفق (پس از ورود موفق)
        /// </summary>
        /// <param name="username">نام کاربری</param>
        /// <param name="ct">توکن لغو عملیات</param>
        Task ResetFailedAttemptsAsync(string username, CancellationToken ct = default);

        /// <summary>
        /// باز کردن قفل حساب (توسط مدیر)
        /// </summary>
        /// <param name="username">نام کاربری</param>
        /// <param name="adminUsername">نام کاربری مدیر</param>
        /// <param name="ct">توکن لغو عملیات</param>
        Task UnlockAccountAsync(
            string username, 
            string adminUsername,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت تنظیمات قفل حساب
        /// </summary>
        AccountLockoutSettings GetSettings();
    }
}

