using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس ارسال پیامک
    /// استفاده برای ارسال هشدارها و اعلان‌های سیستم
    /// </summary>
    public interface ISmsService
    {
        /// <summary>
        /// ارسال پیامک به یک شماره موبایل
        /// </summary>
        /// <param name="mobileNumber">شماره موبایل گیرنده</param>
        /// <param name="message">متن پیامک</param>
        /// <param name="ct">توکن لغو</param>
        /// <returns>نتیجه ارسال</returns>
        Task<SmsSendResult> SendAsync(string mobileNumber, string message, CancellationToken ct = default);

        /// <summary>
        /// ارسال پیامک به چند شماره موبایل
        /// </summary>
        /// <param name="mobileNumbers">لیست شماره‌های موبایل گیرندگان</param>
        /// <param name="message">متن پیامک</param>
        /// <param name="ct">توکن لغو</param>
        /// <returns>نتیجه ارسال برای هر شماره</returns>
        Task<Dictionary<string, SmsSendResult>> SendBulkAsync(
            IEnumerable<string> mobileNumbers, 
            string message, 
            CancellationToken ct = default);
    }
}

