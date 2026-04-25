using System;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس اعتبارسنجی امضای دیجیتال به‌روزرسانی
    /// پیاده‌سازی الزام FPT_TUD_EXT.1.3 از استاندارد ISO 15408
    /// 
    /// این سرویس از RSA-SHA256 برای اعتبارسنجی امضای دیجیتال فایل‌های به‌روزرسانی استفاده می‌کند.
    /// قبل از نصب هر به‌روزرسانی، امضای دیجیتال آن باید تایید شود.
    /// </summary>
    public interface IUpdateSignatureVerificationService
    {
        /// <summary>
        /// اعتبارسنجی امضای دیجیتال یک فایل به‌روزرسانی
        /// </summary>
        /// <param name="fileContent">محتوای فایل به‌روزرسانی</param>
        /// <param name="signature">امضای دیجیتال به صورت Base64</param>
        /// <returns>نتیجه اعتبارسنجی</returns>
        Task<SignatureVerificationResult> VerifyUpdateSignatureAsync(byte[] fileContent, string signature);

        /// <summary>
        /// اعتبارسنجی امضای دیجیتال با استفاده از Hash فایل
        /// </summary>
        /// <param name="fileHash">Hash SHA-256 فایل به صورت Base64</param>
        /// <param name="signature">امضای دیجیتال به صورت Base64</param>
        /// <returns>نتیجه اعتبارسنجی</returns>
        Task<SignatureVerificationResult> VerifySignatureByHashAsync(string fileHash, string signature);

        /// <summary>
        /// اعتبارسنجی امضای دیجیتال متادیتای نسخه
        /// </summary>
        /// <param name="versionMetadata">متادیتای نسخه شامل version, buildDate, buildNumber</param>
        /// <param name="signature">امضای دیجیتال به صورت Base64</param>
        /// <returns>نتیجه اعتبارسنجی</returns>
        Task<SignatureVerificationResult> VerifyVersionMetadataSignatureAsync(string versionMetadata, string signature);

        /// <summary>
        /// محاسبه Hash SHA-256 برای محتوای فایل
        /// </summary>
        /// <param name="content">محتوای فایل</param>
        /// <returns>Hash به صورت Base64</returns>
        string ComputeFileHash(byte[] content);

        /// <summary>
        /// تولید امضای دیجیتال برای فایل (برای استفاده در CI/CD)
        /// </summary>
        /// <param name="fileContent">محتوای فایل</param>
        /// <param name="privateKeyPem">کلید خصوصی به فرمت PEM</param>
        /// <returns>امضای دیجیتال به صورت Base64</returns>
        string GenerateSignature(byte[] fileContent, string privateKeyPem);

        /// <summary>
        /// بررسی اعتبار کلید عمومی
        /// </summary>
        /// <returns>true اگر کلید عمومی معتبر و بارگذاری شده باشد</returns>
        bool IsPublicKeyLoaded();

        /// <summary>
        /// دریافت اطلاعات کلید عمومی
        /// </summary>
        /// <returns>اطلاعات کلید عمومی شامل Fingerprint و تاریخ انقضا</returns>
        PublicKeyInfo GetPublicKeyInfo();
    }

    /// <summary>
    /// نتیجه اعتبارسنجی امضای دیجیتال
    /// </summary>
    public class SignatureVerificationResult
    {
        /// <summary>
        /// آیا امضا معتبر است؟
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// پیام نتیجه
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// کد خطا (در صورت نامعتبر بودن)
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// جزئیات اضافی
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// تاریخ و زمان بررسی
        /// </summary>
        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fingerprint کلید استفاده شده
        /// </summary>
        public string? KeyFingerprint { get; set; }
    }

    /// <summary>
    /// اطلاعات کلید عمومی
    /// </summary>
    public class PublicKeyInfo
    {
        /// <summary>
        /// Fingerprint کلید (SHA-256)
        /// </summary>
        public string Fingerprint { get; set; } = string.Empty;

        /// <summary>
        /// اندازه کلید (بیت)
        /// </summary>
        public int KeySize { get; set; }

        /// <summary>
        /// الگوریتم کلید
        /// </summary>
        public string Algorithm { get; set; } = "RSA";

        /// <summary>
        /// آیا کلید بارگذاری شده؟
        /// </summary>
        public bool IsLoaded { get; set; }

        /// <summary>
        /// تاریخ بارگذاری کلید
        /// </summary>
        public DateTime? LoadedAt { get; set; }
    }
}

