using System;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// اینترفیس سرویس Fail-Secure
    /// ============================================
    /// پیاده‌سازی الزام FPT_FLS.1.1 (الزام 46)
    /// حفظ وضعیت امن در زمان شکست
    /// ============================================
    /// 
    /// این سرویس تضمین می‌کند که:
    /// 1. عملیات حساس در صورت خطا به حالت امن می‌روند
    /// 2. مقادیر پیش‌فرض امن برگردانده می‌شوند
    /// 3. تمام شکست‌ها ثبت و مانیتور می‌شوند
    /// </summary>
    public interface IFailSecureService
    {
        /// <summary>
        /// اجرای عملیات با حفاظت Fail-Secure
        /// در صورت خطا، مقدار امن پیش‌فرض برگردانده می‌شود
        /// </summary>
        /// <typeparam name="T">نوع خروجی</typeparam>
        /// <param name="operation">عملیات اصلی</param>
        /// <param name="failSafeDefault">مقدار امن پیش‌فرض در صورت خطا</param>
        /// <param name="operationName">نام عملیات برای لاگ</param>
        /// <param name="ct">توکن لغو</param>
        /// <returns>نتیجه عملیات یا مقدار امن پیش‌فرض</returns>
        Task<T> ExecuteSecureAsync<T>(
            Func<Task<T>> operation,
            T failSafeDefault,
            string operationName,
            CancellationToken ct = default);

        /// <summary>
        /// اجرای عملیات void با حفاظت Fail-Secure
        /// در صورت خطا، عملیات به صورت امن نادیده گرفته می‌شود
        /// </summary>
        /// <param name="operation">عملیات اصلی</param>
        /// <param name="operationName">نام عملیات برای لاگ</param>
        /// <param name="ct">توکن لغو</param>
        Task ExecuteSecureAsync(
            Func<Task> operation,
            string operationName,
            CancellationToken ct = default);

        /// <summary>
        /// بررسی فعال بودن حالت امن سیستم
        /// در حالت امن، تمام عملیات حساس محدود می‌شوند
        /// </summary>
        bool IsSystemInSecureMode();

        /// <summary>
        /// فعال‌سازی حالت امن سیستم
        /// در صورت تشخیص تهدید یا شکست‌های متوالی
        /// </summary>
        /// <param name="reason">دلیل فعال‌سازی</param>
        /// <param name="ct">توکن لغو</param>
        Task ActivateSecureModeAsync(string reason, CancellationToken ct = default);

        /// <summary>
        /// غیرفعال‌سازی حالت امن سیستم
        /// فقط توسط Admin مجاز است
        /// </summary>
        /// <param name="deactivatedBy">شناسه کاربر غیرفعال‌کننده</param>
        /// <param name="ct">توکن لغو</param>
        Task DeactivateSecureModeAsync(long deactivatedBy, CancellationToken ct = default);

        /// <summary>
        /// ثبت رویداد شکست
        /// برای مانیتورینگ و تشخیص الگوهای حمله
        /// </summary>
        /// <param name="failureType">نوع شکست</param>
        /// <param name="operationName">نام عملیات</param>
        /// <param name="details">جزئیات شکست</param>
        /// <param name="ct">توکن لغو</param>
        Task RecordFailureAsync(
            string failureType,
            string operationName,
            string details,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت تعداد شکست‌های اخیر
        /// برای تشخیص نیاز به فعال‌سازی حالت امن
        /// </summary>
        /// <param name="timeWindow">بازه زمانی بررسی</param>
        int GetRecentFailureCount(TimeSpan timeWindow);

        /// <summary>
        /// دریافت وضعیت سلامت سیستم از نظر Fail-Secure
        /// </summary>
        FailSecureHealthStatus GetHealthStatus();

        /// <summary>
        /// ثبت شکست در فایل (وقتی دیتابیس در دسترس نیست)
        /// این متد برای استفاده در سرویس‌های دیگر است
        /// </summary>
        /// <param name="failureType">نوع شکست</param>
        /// <param name="operationName">نام عملیات</param>
        /// <param name="details">جزئیات شکست</param>
        Task LogFailureToFileAsync(string failureType, string operationName, string details);
    }

    /// <summary>
    /// وضعیت سلامت سیستم از نظر Fail-Secure
    /// </summary>
    public class FailSecureHealthStatus
    {
        /// <summary>
        /// آیا سیستم در حالت امن است؟
        /// </summary>
        public bool IsInSecureMode { get; set; }

        /// <summary>
        /// تعداد شکست‌های اخیر (در 5 دقیقه گذشته)
        /// </summary>
        public int RecentFailureCount { get; set; }

        /// <summary>
        /// زمان آخرین شکست
        /// </summary>
        public DateTime? LastFailureTime { get; set; }

        /// <summary>
        /// نوع آخرین شکست
        /// </summary>
        public string? LastFailureType { get; set; }

        /// <summary>
        /// زمان فعال‌سازی حالت امن (اگر فعال است)
        /// </summary>
        public DateTime? SecureModeActivatedAt { get; set; }

        /// <summary>
        /// دلیل فعال‌سازی حالت امن
        /// </summary>
        public string? SecureModeReason { get; set; }

        /// <summary>
        /// وضعیت کلی سلامت
        /// </summary>
        public string HealthLevel
        {
            get
            {
                if (RecentFailureCount == 0)
                    return "Healthy";
                else if (RecentFailureCount < 5)
                    return "Warning";
                else if (RecentFailureCount < 10)
                    return "Critical";
                else
                    return "Emergency";
            }
        }
    }
}

