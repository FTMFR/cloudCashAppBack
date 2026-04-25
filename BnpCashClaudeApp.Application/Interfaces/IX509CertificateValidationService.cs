using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// نتیجه اعتبارسنجی گواهینامه X.509
    /// </summary>
    public class CertificateValidationResult
    {
        /// <summary>
        /// آیا گواهینامه معتبر است
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// لیست خطاهای اعتبارسنجی
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// آیا گواهینامه ابطال شده است
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// روش بررسی وضعیت ابطال (OCSP یا CRL)
        /// </summary>
        public string? RevocationCheckMethod { get; set; }

        /// <summary>
        /// تاریخ انقضای گواهینامه
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Thumbprint گواهینامه
        /// </summary>
        public string? Thumbprint { get; set; }

        /// <summary>
        /// Subject گواهینامه
        /// </summary>
        public string? Subject { get; set; }
    }

    /// <summary>
    /// سرویس اعتبارسنجی گواهینامه‌های X.509
    /// پیاده‌سازی الزامات پروفایل حفاظتی برنامه‌های کاربردی تحت شبکه (ISO 15408)
    /// </summary>
    public interface IX509CertificateValidationService
    {
        /// <summary>
        /// اعتبارسنجی کامل گواهینامه X.509
        /// شامل بررسی basicConstraints، extendedKeyUsage و وضعیت ابطال
        /// </summary>
        /// <param name="certificate">گواهینامه X.509 برای اعتبارسنجی</param>
        /// <param name="ct">توکن لغو عملیات</param>
        /// <returns>نتیجه اعتبارسنجی</returns>
        Task<CertificateValidationResult> ValidateCertificateAsync(
            X509Certificate2 certificate,
            CancellationToken ct = default);

        /// <summary>
        /// بررسی basicConstraints گواهینامه
        /// الزام: گواهینامه‌های CA باید دارای CA=True باشند
        /// </summary>
        /// <param name="certificate">گواهینامه X.509</param>
        /// <returns>آیا basicConstraints معتبر است</returns>
        bool ValidateBasicConstraints(X509Certificate2 certificate);

        /// <summary>
        /// بررسی extendedKeyUsage گواهینامه
        /// الزام: گواهینامه‌های سرور باید Server Authentication داشته باشند
        /// </summary>
        /// <param name="certificate">گواهینامه X.509</param>
        /// <param name="requiredUsage">کاربرد مورد نیاز</param>
        /// <returns>آیا extendedKeyUsage معتبر است</returns>
        bool ValidateExtendedKeyUsage(X509Certificate2 certificate, string requiredUsage);

        /// <summary>
        /// بررسی وضعیت ابطال گواهینامه با استفاده از OCSP (RFC 6960)
        /// </summary>
        /// <param name="certificate">گواهینامه X.509</param>
        /// <param name="issuerCertificate">گواهینامه صادرکننده</param>
        /// <param name="ct">توکن لغو عملیات</param>
        /// <returns>آیا گواهینامه ابطال شده است</returns>
        Task<bool> CheckRevocationStatusOCSPAsync(
            X509Certificate2 certificate,
            X509Certificate2? issuerCertificate = null,
            CancellationToken ct = default);

        /// <summary>
        /// بررسی وضعیت ابطال گواهینامه با استفاده از CRL (RFC 5280)
        /// </summary>
        /// <param name="certificate">گواهینامه X.509</param>
        /// <param name="ct">توکن لغو عملیات</param>
        /// <returns>آیا گواهینامه ابطال شده است</returns>
        Task<bool> CheckRevocationStatusCRLAsync(
            X509Certificate2 certificate,
            CancellationToken ct = default);

        /// <summary>
        /// اعتبارسنجی زنجیره گواهینامه
        /// الزام: پشتیبانی از حداقل دو گواهینامه (Root CA + Server Certificate)
        /// </summary>
        /// <param name="certificate">گواهینامه X.509</param>
        /// <param name="ct">توکن لغو عملیات</param>
        /// <returns>نتیجه اعتبارسنجی زنجیره</returns>
        Task<CertificateValidationResult> ValidateCertificateChainAsync(
            X509Certificate2 certificate,
            CancellationToken ct = default);
    }

    /// <summary>
    /// OID های استاندارد برای extendedKeyUsage
    /// </summary>
    public static class ExtendedKeyUsageOids
    {
        /// <summary>
        /// Server Authentication (1.3.6.1.5.5.7.3.1)
        /// برای گواهینامه‌های سرور TLS
        /// </summary>
        public const string ServerAuthentication = "1.3.6.1.5.5.7.3.1";

        /// <summary>
        /// Client Authentication (1.3.6.1.5.5.7.3.2)
        /// برای گواهینامه‌های کلاینت TLS
        /// </summary>
        public const string ClientAuthentication = "1.3.6.1.5.5.7.3.2";

        /// <summary>
        /// Code Signing (1.3.6.1.5.5.7.3.3)
        /// برای امضای کد و به‌روزرسانی‌های نرم‌افزار
        /// </summary>
        public const string CodeSigning = "1.3.6.1.5.5.7.3.3";

        /// <summary>
        /// OCSP Signing (1.3.6.1.5.5.7.3.9)
        /// برای پاسخ‌های OCSP
        /// </summary>
        public const string OCSPSigning = "1.3.6.1.5.5.7.3.9";
    }
}

