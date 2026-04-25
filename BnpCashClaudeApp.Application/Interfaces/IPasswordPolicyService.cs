using System;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// نتیجه اعتبارسنجی رمز عبور
    /// </summary>
    public class PasswordValidationResult
    {
        /// <summary>
        /// آیا رمز عبور معتبر است
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// لیست خطاهای اعتبارسنجی
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// امتیاز قدرت رمز عبور (0-100)
        /// </summary>
        public int StrengthScore { get; set; }

        /// <summary>
        /// توصیف قدرت رمز عبور
        /// </summary>
        public string StrengthDescription { get; set; } = string.Empty;
    }

    /// <summary>
    /// تنظیمات سیاست رمز عبور
    /// </summary>
    public class PasswordPolicySettings
    {
        /// <summary>
        /// حداقل طول رمز عبور
        /// الزام استاندارد: حداقل 8 کاراکتر
        /// </summary>
        public int MinimumLength { get; set; } = 8;

        /// <summary>
        /// حداکثر طول رمز عبور
        /// </summary>
        public int MaximumLength { get; set; } = 128;

        /// <summary>
        /// آیا حداقل یک حرف بزرگ الزامی است
        /// </summary>
        public bool RequireUppercase { get; set; } = true;

        /// <summary>
        /// آیا حداقل یک حرف کوچک الزامی است
        /// </summary>
        public bool RequireLowercase { get; set; } = true;

        /// <summary>
        /// آیا حداقل یک عدد الزامی است
        /// </summary>
        public bool RequireDigit { get; set; } = true;

        /// <summary>
        /// آیا حداقل یک کاراکتر خاص الزامی است
        /// </summary>
        public bool RequireSpecialCharacter { get; set; } = true;

        /// <summary>
        /// لیست کاراکترهای خاص مجاز
        /// </summary>
        public string SpecialCharacters { get; set; } = "!@#$%^&*()_+-=[]{}|;':\",./<>?";

        /// <summary>
        /// آیا رمز عبور نباید شامل نام کاربری باشد
        /// </summary>
        public bool DisallowUsername { get; set; } = true;

        /// <summary>
        /// تعداد رمزهای قبلی که نباید تکرار شوند
        /// </summary>
        public int PasswordHistoryCount { get; set; } = 5;

        /// <summary>
        /// مدت اعتبار رمز عبور به روز (0 = بدون انقضا)
        /// </summary>
        public int PasswordExpirationDays { get; set; } = 90;
    }

    /// <summary>
    /// سرویس سیاست رمز عبور
    /// پیاده‌سازی الزامات پروفایل حفاظتی برنامه‌های کاربردی تحت شبکه (ISO 15408)
    /// </summary>
    public interface IPasswordPolicyService
    {
        /// <summary>
        /// اعتبارسنجی رمز عبور بر اساس سیاست‌های تعریف شده
        /// </summary>
        /// <param name="password">رمز عبور برای اعتبارسنجی</param>
        /// <param name="username">نام کاربری (اختیاری - برای بررسی عدم تشابه)</param>
        /// <returns>نتیجه اعتبارسنجی</returns>
        PasswordValidationResult ValidatePassword(string password, string? username = null);

        /// <summary>
        /// محاسبه امتیاز قدرت رمز عبور
        /// </summary>
        /// <param name="password">رمز عبور</param>
        /// <returns>امتیاز قدرت (0-100)</returns>
        int CalculatePasswordStrength(string password);

        /// <summary>
        /// دریافت تنظیمات سیاست رمز عبور
        /// </summary>
        PasswordPolicySettings GetPolicySettings();

        /// <summary>
        /// بررسی اینکه آیا رمز عبور در تاریخچه رمزهای قبلی کاربر وجود دارد
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="newPasswordHash">Hash رمز عبور جدید</param>
        /// <returns>آیا رمز عبور تکراری است</returns>
        bool IsPasswordInHistory(int userId, string newPasswordHash);
    }
}

