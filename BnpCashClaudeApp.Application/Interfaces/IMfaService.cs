using System;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس احرازهویت چندگانه (MFA) با پیامک
    /// پیاده‌سازی الزام FIA_UAU.5 از ISO 15408
    /// ============================================
    /// ارسال کد یکبار مصرف (OTP) از طریق پیامک
    /// ============================================
    /// </summary>
    public interface IMfaService
    {
        /// <summary>
        /// تولید و ارسال کد OTP به شماره موبایل
        /// </summary>
        /// <param name="mobileNumber">شماره موبایل کاربر</param>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="ct">توکن لغو</param>
        /// <returns>نتیجه ارسال پیامک</returns>
        Task<SmsSendResult> GenerateAndSendOtpAsync(string mobileNumber, long userId, CancellationToken ct = default);

        /// <summary>
        /// تایید کد OTP
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="otpCode">کد وارد شده توسط کاربر</param>
        /// <returns>آیا کد معتبر است</returns>
        bool VerifyOtp(long userId, string otpCode);

        /// <summary>
        /// تولید کدهای بازیابی
        /// </summary>
        string[] GenerateRecoveryCodes(int count = 10);

        /// <summary>
        /// هش کردن کد بازیابی برای ذخیره امن
        /// </summary>
        string HashRecoveryCode(string code);

        /// <summary>
        /// تایید کد بازیابی
        /// </summary>
        bool VerifyRecoveryCode(string hashedCode, string providedCode);

        /// <summary>
        /// حذف کد OTP از کش (پس از استفاده یا انقضا)
        /// </summary>
        void InvalidateOtp(long userId);
    }

    /// <summary>
    /// نتیجه ارسال پیامک
    /// </summary>
    public class SmsSendResult
    {
        /// <summary>
        /// آیا ارسال موفق بود
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// پیام خطا (در صورت عدم موفقیت)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// زمان انقضای کد (به ثانیه)
        /// </summary>
        public int ExpirySeconds { get; set; }

        /// <summary>
        /// شماره موبایل ماسک شده (برای نمایش به کاربر)
        /// مثال: 0912***4567
        /// </summary>
        public string? MaskedMobileNumber { get; set; }
    }

    /// <summary>
    /// نتیجه تایید MFA
    /// </summary>
    public class MfaVerificationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsRecoveryCodeUsed { get; set; }
    }
}
