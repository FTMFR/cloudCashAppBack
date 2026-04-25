using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس تولید و اعتبارسنجی CAPTCHA
    /// الزام FIA_UAU.5 - لایه امنیتی اضافی برای MFA
    /// ============================================
    /// - تولید تصویر CAPTCHA با کد قابل تنظیم
    /// - ذخیره کد در Memory Cache
    /// - اعتبارسنجی ورودی کاربر
    /// - تنظیمات از SecuritySettings خوانده می‌شود
    /// ============================================
    /// </summary>
    public interface ICaptchaService
    {
        /// <summary>
        /// تولید CAPTCHA جدید
        /// </summary>
        /// <param name="base64Image">تصویر CAPTCHA به صورت Base64</param>
        /// <param name="captchaId">شناسه یکتای CAPTCHA</param>
        /// <param name="ct">توکن لغو</param>
        /// <returns>کد CAPTCHA (برای لاگ، در production استفاده نشود)</returns>
        Task<string> GenerateCaptchaAsync(CancellationToken ct = default);

        /// <summary>
        /// دریافت آخرین CAPTCHA تولید شده
        /// </summary>
        CaptchaResult? GetLastCaptcha();

        /// <summary>
        /// اعتبارسنجی ورودی کاربر
        /// </summary>
        /// <param name="captchaId">شناسه CAPTCHA</param>
        /// <param name="userInput">کد وارد شده توسط کاربر</param>
        /// <param name="message">پیام نتیجه</param>
        /// <returns>آیا کد صحیح است</returns>
        bool ValidateCaptcha(string captchaId, string userInput, out string message);

        /// <summary>
        /// آیا CAPTCHA فعال است؟
        /// </summary>
        Task<bool> IsEnabledAsync(CancellationToken ct = default);

        /// <summary>
        /// آیا در MFA نیاز به CAPTCHA است؟
        /// </summary>
        Task<bool> IsRequiredOnMfaAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// نتیجه تولید CAPTCHA
    /// </summary>
    public class CaptchaResult
    {
        /// <summary>
        /// شناسه یکتای CAPTCHA
        /// </summary>
        public string CaptchaId { get; set; } = string.Empty;

        /// <summary>
        /// تصویر CAPTCHA به صورت Base64
        /// </summary>
        public string ImageBase64 { get; set; } = string.Empty;
    }
}

