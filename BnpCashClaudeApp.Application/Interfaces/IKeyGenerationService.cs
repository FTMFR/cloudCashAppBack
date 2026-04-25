using System;
using System.Security.Cryptography;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس تولید کلید رمزنگاری
    /// پیاده‌سازی الزام FCS_CKM.1.1 از استاندارد ISO 15408
    /// 
    /// این سرویس برای تولید امن کلیدهای رمزنگاری استفاده می‌شود:
    /// - کلیدهای متقارن (AES)
    /// - کلیدهای JWT
    /// - کلیدهای HMAC
    /// - Salt و IV
    /// 
    /// الزامات:
    /// - استفاده از CSPRNG (Cryptographically Secure Pseudo-Random Number Generator)
    /// - حداقل طول کلید: 256 بیت برای AES
    /// - حداقل طول کلید: 256 بیت برای HMAC
    /// </summary>
    public interface IKeyGenerationService
    {
        /// <summary>
        /// تولید کلید متقارن امن با طول مشخص
        /// </summary>
        /// <param name="keyLengthBits">طول کلید به بیت (128، 192، 256)</param>
        /// <returns>کلید به صورت آرایه بایت</returns>
        byte[] GenerateSymmetricKey(int keyLengthBits = 256);

        /// <summary>
        /// تولید کلید متقارن امن و تبدیل به Base64
        /// </summary>
        /// <param name="keyLengthBits">طول کلید به بیت</param>
        /// <returns>کلید به صورت Base64</returns>
        string GenerateSymmetricKeyBase64(int keyLengthBits = 256);

        /// <summary>
        /// تولید کلید JWT امن
        /// حداقل 256 بیت برای HMAC-SHA256
        /// </summary>
        /// <returns>کلید JWT به صورت Base64</returns>
        string GenerateJwtKey();

        /// <summary>
        /// تولید کلید HMAC امن
        /// </summary>
        /// <param name="keyLengthBits">طول کلید به بیت</param>
        /// <returns>کلید HMAC به صورت آرایه بایت</returns>
        byte[] GenerateHmacKey(int keyLengthBits = 256);

        /// <summary>
        /// تولید Salt امن برای Password Hashing
        /// </summary>
        /// <param name="lengthBytes">طول Salt به بایت (پیش‌فرض: 16 بایت = 128 بیت)</param>
        /// <returns>Salt به صورت آرایه بایت</returns>
        byte[] GenerateSalt(int lengthBytes = 16);

        /// <summary>
        /// تولید IV (Initialization Vector) امن برای AES
        /// </summary>
        /// <returns>IV به صورت آرایه بایت (16 بایت برای AES)</returns>
        byte[] GenerateIV();

        /// <summary>
        /// تولید توکن امن تصادفی
        /// </summary>
        /// <param name="lengthBytes">طول توکن به بایت</param>
        /// <returns>توکن به صورت Base64</returns>
        string GenerateSecureToken(int lengthBytes = 64);

        /// <summary>
        /// تولید شناسه امن تصادفی (مثلاً برای API Key)
        /// </summary>
        /// <param name="lengthBytes">طول شناسه به بایت</param>
        /// <returns>شناسه به صورت Hex</returns>
        string GenerateSecureId(int lengthBytes = 32);

        /// <summary>
        /// اعتبارسنجی قدرت کلید
        /// </summary>
        /// <param name="key">کلید برای بررسی</param>
        /// <param name="minimumBits">حداقل طول به بیت</param>
        /// <returns>نتیجه اعتبارسنجی</returns>
        KeyStrengthValidationResult ValidateKeyStrength(byte[] key, int minimumBits = 256);

        /// <summary>
        /// اعتبارسنجی قدرت کلید Base64
        /// </summary>
        /// <param name="keyBase64">کلید به صورت Base64</param>
        /// <param name="minimumBits">حداقل طول به بیت</param>
        /// <returns>نتیجه اعتبارسنجی</returns>
        KeyStrengthValidationResult ValidateKeyStrengthBase64(string keyBase64, int minimumBits = 256);
    }

    /// <summary>
    /// نتیجه اعتبارسنجی قدرت کلید
    /// </summary>
    public class KeyStrengthValidationResult
    {
        /// <summary>
        /// آیا کلید معتبر است
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// طول کلید به بیت
        /// </summary>
        public int KeyLengthBits { get; set; }

        /// <summary>
        /// حداقل طول مورد نیاز به بیت
        /// </summary>
        public int MinimumRequiredBits { get; set; }

        /// <summary>
        /// پیام خطا (در صورت نامعتبر بودن)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// سطح قدرت کلید
        /// </summary>
        public KeyStrengthLevel StrengthLevel { get; set; }
    }

    /// <summary>
    /// سطح قدرت کلید
    /// </summary>
    public enum KeyStrengthLevel
    {
        /// <summary>
        /// ضعیف - کمتر از 128 بیت
        /// </summary>
        Weak = 0,

        /// <summary>
        /// متوسط - 128 تا 191 بیت
        /// </summary>
        Medium = 1,

        /// <summary>
        /// قوی - 192 تا 255 بیت
        /// </summary>
        Strong = 2,

        /// <summary>
        /// بسیار قوی - 256 بیت یا بیشتر
        /// </summary>
        VeryStrong = 3
    }
}

