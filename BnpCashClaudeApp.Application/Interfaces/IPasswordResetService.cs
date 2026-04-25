using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس بازیابی رمز عبور با کد یکبار مصرف (OTP)
    /// ============================================
    /// فلو:
    /// 1. درخواست ارسال OTP به شماره موبایل (ForgotPassword)
    /// 2. تایید OTP و دریافت توکن ریست (VerifyOtp)
    /// 3. تغییر رمز عبور با توکن ریست (ResetPassword)
    /// ============================================
    /// </summary>
    public interface IPasswordResetService
    {
        /// <summary>
        /// تولید و ارسال کد OTP برای بازیابی رمز عبور
        /// </summary>
        /// <param name="mobileNumber">شماره موبایل کاربر</param>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="ct">توکن لغو</param>
        /// <returns>نتیجه ارسال پیامک</returns>
        Task<SmsSendResult> GenerateAndSendPasswordResetOtpAsync(string mobileNumber, long userId, CancellationToken ct = default);

        /// <summary>
        /// تایید کد OTP بازیابی رمز عبور
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="otpCode">کد وارد شده توسط کاربر</param>
        /// <returns>آیا کد معتبر است</returns>
        bool VerifyPasswordResetOtp(long userId, string otpCode);

        /// <summary>
        /// ثبت تلاش ناموفق OTP و بررسی محدودیت تلاش
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <returns>تعداد تلاش‌های باقی‌مانده (0 یعنی OTP باطل شد)</returns>
        int RecordFailedOtpAttempt(long userId);

        /// <summary>
        /// حذف OTP بازیابی رمز عبور از کش
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        void InvalidatePasswordResetOtp(long userId);

        /// <summary>
        /// ذخیره توکن ریست رمز عبور در کش
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="resetToken">توکن ریست</param>
        void StorePasswordResetToken(long userId, string resetToken);

        /// <summary>
        /// اعتبارسنجی توکن ریست رمز عبور از کش
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="resetToken">توکن ریست</param>
        /// <returns>آیا توکن معتبر است</returns>
        bool ValidatePasswordResetToken(long userId, string resetToken);

        /// <summary>
        /// حذف توکن ریست رمز عبور از کش (یکبار مصرف)
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        void InvalidatePasswordResetToken(long userId);
    }
}
