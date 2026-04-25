using System;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس پاکسازی امن اطلاعات باقیمانده در حافظه
    /// پیاده‌سازی الزام FDP_RIP.2 از استاندارد ISO 15408
    /// 
    /// این سرویس برای پاکسازی امن اطلاعات حساس از حافظه استفاده می‌شود:
    /// - رمز عبور
    /// - کلیدهای رمزنگاری
    /// - توکن‌ها
    /// - سایر اطلاعات حساس
    /// </summary>
    public interface ISecureMemoryService
    {
        /// <summary>
        /// پاکسازی امن یک رشته از حافظه
        /// با استفاده از Zeroization (پر کردن با صفر)
        /// </summary>
        /// <param name="sensitiveData">اطلاعات حساس که باید پاک شود</param>
        void ClearString(ref string? sensitiveData);

        /// <summary>
        /// پاکسازی امن یک آرایه بایت از حافظه
        /// </summary>
        /// <param name="sensitiveData">آرایه بایت حساس</param>
        void ClearBytes(byte[]? sensitiveData);

        /// <summary>
        /// پاکسازی امن یک آرایه کاراکتر از حافظه
        /// </summary>
        /// <param name="sensitiveData">آرایه کاراکتر حساس</param>
        void ClearChars(char[]? sensitiveData);

        /// <summary>
        /// پاکسازی امن یک SecureString
        /// </summary>
        /// <param name="secureString">SecureString که باید پاک شود</param>
        void ClearSecureString(System.Security.SecureString? secureString);

        /// <summary>
        /// تبدیل یک رشته به SecureString برای ذخیره‌سازی امن
        /// </summary>
        /// <param name="plainText">متن ساده</param>
        /// <returns>SecureString</returns>
        System.Security.SecureString ConvertToSecureString(string plainText);

        /// <summary>
        /// تبدیل SecureString به رشته (فقط برای استفاده موقت)
        /// </summary>
        /// <param name="secureString">SecureString</param>
        /// <returns>رشته (باید بعد از استفاده پاک شود)</returns>
        string ConvertFromSecureString(System.Security.SecureString secureString);

        /// <summary>
        /// پاکسازی امن یک شی IDisposable
        /// </summary>
        /// <typeparam name="T">نوع شی که IDisposable است</typeparam>
        /// <param name="disposable">شی که باید پاک شود</param>
        void SecureDispose<T>(ref T? disposable) where T : class, IDisposable;
    }
}

