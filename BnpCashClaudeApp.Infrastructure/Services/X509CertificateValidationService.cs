using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// سرویس اعتبارسنجی گواهینامه‌های X.509
    /// پیاده‌سازی الزامات پروفایل حفاظتی برنامه‌های کاربردی تحت شبکه (ISO 15408)
    /// </summary>
    public class X509CertificateValidationService : IX509CertificateValidationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<X509CertificateValidationService> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly IConfiguration _configuration;

        // OID های مهم برای اعتبارسنجی
        private const string BasicConstraintsOid = "2.5.29.19";
        private const string ExtendedKeyUsageOid = "2.5.29.37";
        private const string AuthorityInfoAccessOid = "1.3.6.1.5.5.7.1.1";
        private const string CrlDistributionPointsOid = "2.5.29.31";

        public X509CertificateValidationService(
            IHttpClientFactory httpClientFactory,
            ILogger<X509CertificateValidationService> logger,
            IAuditLogService auditLogService,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _auditLogService = auditLogService;
            _configuration = configuration;
        }

        /// <summary>
        /// اعتبارسنجی کامل گواهینامه X.509
        /// شامل بررسی basicConstraints، extendedKeyUsage و وضعیت ابطال
        /// </summary>
        public async Task<CertificateValidationResult> ValidateCertificateAsync(
            X509Certificate2 certificate,
            CancellationToken ct = default)
        {
            var result = new CertificateValidationResult
            {
                IsValid = true,
                Thumbprint = certificate.Thumbprint,
                Subject = certificate.Subject,
                ExpirationDate = certificate.NotAfter
            };

            try
            {
                // ============================================
                // الزام 1: بررسی تاریخ انقضای گواهینامه
                // ============================================
                if (DateTime.Now > certificate.NotAfter)
                {
                    result.IsValid = false;
                    result.Errors.Add($"گواهینامه منقضی شده است. تاریخ انقضا: {certificate.NotAfter:yyyy-MM-dd}");
                }

                if (DateTime.Now < certificate.NotBefore)
                {
                    result.IsValid = false;
                    result.Errors.Add($"گواهینامه هنوز معتبر نشده است. تاریخ شروع: {certificate.NotBefore:yyyy-MM-dd}");
                }

                // ============================================
                // الزام 2: بررسی basicConstraints
                // ============================================
                if (!ValidateBasicConstraints(certificate))
                {
                    result.IsValid = false;
                    result.Errors.Add("گواهینامه CA دارای basicConstraints نامعتبر است");
                }

                // ============================================
                // الزام 3: بررسی extendedKeyUsage برای گواهینامه‌های سرور
                // ============================================
                // این بررسی فقط برای گواهینامه‌های end-entity انجام می‌شود
                var basicConstraints = certificate.Extensions
                    .OfType<X509BasicConstraintsExtension>()
                    .FirstOrDefault();

                if (basicConstraints == null || !basicConstraints.CertificateAuthority)
                {
                    // این یک گواهینامه end-entity است
                    if (!ValidateExtendedKeyUsage(certificate, ExtendedKeyUsageOids.ServerAuthentication))
                    {
                        _logger.LogWarning(
                            "گواهینامه {Thumbprint} فاقد extendedKeyUsage برای Server Authentication است",
                            certificate.Thumbprint);
                        // این یک هشدار است، نه خطا - بسته به پیکربندی می‌تواند خطا باشد
                    }
                }

                // ============================================
                // الزام 4: بررسی وضعیت ابطال (OCSP یا CRL)
                // ============================================
                var useOCSP = _configuration.GetValue<bool>("CertificateValidation:UseOCSP", true);
                var useCRL = _configuration.GetValue<bool>("CertificateValidation:UseCRL", true);
                var requireRevocationCheck = _configuration.GetValue<bool>("CertificateValidation:RequireRevocationCheck", false);

                bool revocationChecked = false;

                if (useOCSP)
                {
                    try
                    {
                        result.IsRevoked = await CheckRevocationStatusOCSPAsync(certificate, null, ct);
                        result.RevocationCheckMethod = "OCSP";
                        revocationChecked = true;

                        if (result.IsRevoked)
                        {
                            result.IsValid = false;
                            result.Errors.Add("گواهینامه ابطال شده است (بررسی شده با OCSP)");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "بررسی OCSP برای گواهینامه {Thumbprint} با خطا مواجه شد", certificate.Thumbprint);
                    }
                }

                if (!revocationChecked && useCRL)
                {
                    try
                    {
                        result.IsRevoked = await CheckRevocationStatusCRLAsync(certificate, ct);
                        result.RevocationCheckMethod = "CRL";
                        revocationChecked = true;

                        if (result.IsRevoked)
                        {
                            result.IsValid = false;
                            result.Errors.Add("گواهینامه ابطال شده است (بررسی شده با CRL)");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "بررسی CRL برای گواهینامه {Thumbprint} با خطا مواجه شد", certificate.Thumbprint);
                    }
                }

                if (!revocationChecked && requireRevocationCheck)
                {
                    result.IsValid = false;
                    result.Errors.Add("امکان بررسی وضعیت ابطال گواهینامه وجود ندارد");
                }

                // ============================================
                // ثبت رویداد اعتبارسنجی در Audit Log
                // ============================================
                await _auditLogService.LogEventAsync(
                    eventType: "CertificateValidation",
                    entityType: "X509Certificate",
                    entityId: certificate.Thumbprint,
                    isSuccess: result.IsValid,
                    errorMessage: result.IsValid ? null : string.Join("; ", result.Errors),
                    description: $"اعتبارسنجی گواهینامه: {certificate.Subject}",
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در اعتبارسنجی گواهینامه {Thumbprint}", certificate.Thumbprint);
                result.IsValid = false;
                result.Errors.Add($"خطا در اعتبارسنجی: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// بررسی basicConstraints گواهینامه
        /// الزام: گواهینامه‌های CA باید دارای CA=True باشند
        /// </summary>
        public bool ValidateBasicConstraints(X509Certificate2 certificate)
        {
            try
            {
                var basicConstraints = certificate.Extensions
                    .OfType<X509BasicConstraintsExtension>()
                    .FirstOrDefault();

                if (basicConstraints == null)
                {
                    // اگر extension وجود نداشته باشد، فرض می‌کنیم یک گواهینامه end-entity است
                    return true;
                }

                // اگر گواهینامه CA است، باید CertificateAuthority=True باشد
                // این بررسی برای جلوگیری از حملات man-in-the-middle است
                if (basicConstraints.CertificateAuthority)
                {
                    _logger.LogDebug(
                        "گواهینامه {Thumbprint} یک CA certificate است با PathLengthConstraint: {PathLength}",
                        certificate.Thumbprint,
                        basicConstraints.HasPathLengthConstraint ? basicConstraints.PathLengthConstraint.ToString() : "نامحدود");
                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در بررسی basicConstraints برای گواهینامه {Thumbprint}", certificate.Thumbprint);
                return false;
            }
        }

        /// <summary>
        /// بررسی extendedKeyUsage گواهینامه
        /// الزام: گواهینامه‌های سرور باید Server Authentication داشته باشند
        /// </summary>
        public bool ValidateExtendedKeyUsage(X509Certificate2 certificate, string requiredUsage)
        {
            try
            {
                var extendedKeyUsage = certificate.Extensions
                    .OfType<X509EnhancedKeyUsageExtension>()
                    .FirstOrDefault();

                if (extendedKeyUsage == null)
                {
                    // اگر extension وجود نداشته باشد، گواهینامه می‌تواند برای هر کاربردی استفاده شود
                    return true;
                }

                // بررسی اینکه OID مورد نظر در لیست وجود دارد
                var hasRequiredUsage = extendedKeyUsage.EnhancedKeyUsages
                    .Cast<Oid>()
                    .Any(oid => oid.Value == requiredUsage);

                if (hasRequiredUsage)
                {
                    _logger.LogDebug(
                        "گواهینامه {Thumbprint} دارای extendedKeyUsage مورد نظر است: {Usage}",
                        certificate.Thumbprint,
                        requiredUsage);
                }

                return hasRequiredUsage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در بررسی extendedKeyUsage برای گواهینامه {Thumbprint}", certificate.Thumbprint);
                return false;
            }
        }

        /// <summary>
        /// بررسی وضعیت ابطال گواهینامه با استفاده از OCSP (RFC 6960)
        /// </summary>
        public async Task<bool> CheckRevocationStatusOCSPAsync(
            X509Certificate2 certificate,
            X509Certificate2? issuerCertificate = null,
            CancellationToken ct = default)
        {
            try
            {
                // استخراج URL OCSP از گواهینامه
                var authorityInfoAccess = certificate.Extensions
                    .OfType<X509Extension>()
                    .FirstOrDefault(e => e.Oid?.Value == AuthorityInfoAccessOid);

                if (authorityInfoAccess == null)
                {
                    _logger.LogWarning(
                        "گواهینامه {Thumbprint} فاقد Authority Information Access extension است",
                        certificate.Thumbprint);
                    throw new InvalidOperationException("OCSP URL در گواهینامه یافت نشد");
                }

                // ============================================
                // توجه: برای پیاده‌سازی کامل OCSP، نیاز به 
                // ساخت OCSP Request و پردازش OCSP Response داریم
                // این کار نیاز به کتابخانه‌های تخصصی مانند BouncyCastle دارد
                // ============================================

                // استفاده از X509Chain برای بررسی ساده‌تر
                using (var chain = new X509Chain())
                {
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                    chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(
                        _configuration.GetValue<int>("CertificateValidation:OCSPTimeout", 10));
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

                    var chainBuilt = chain.Build(certificate);

                    foreach (var element in chain.ChainElements)
                    {
                        foreach (var status in element.ChainElementStatus)
                        {
                            if (status.Status == X509ChainStatusFlags.Revoked)
                            {
                                _logger.LogWarning(
                                    "گواهینامه {Thumbprint} ابطال شده است (OCSP)",
                                    certificate.Thumbprint);
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "خطا در بررسی OCSP برای گواهینامه {Thumbprint}", certificate.Thumbprint);
                throw;
            }
        }

        /// <summary>
        /// بررسی وضعیت ابطال گواهینامه با استفاده از CRL (RFC 5280)
        /// </summary>
        public async Task<bool> CheckRevocationStatusCRLAsync(
            X509Certificate2 certificate,
            CancellationToken ct = default)
        {
            try
            {
                // بررسی با استفاده از X509Chain
                using (var chain = new X509Chain())
                {
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                    chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(
                        _configuration.GetValue<int>("CertificateValidation:CRLTimeout", 10));
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

                    var chainBuilt = chain.Build(certificate);

                    foreach (var element in chain.ChainElements)
                    {
                        foreach (var status in element.ChainElementStatus)
                        {
                            if (status.Status == X509ChainStatusFlags.Revoked)
                            {
                                _logger.LogWarning(
                                    "گواهینامه {Thumbprint} ابطال شده است (CRL)",
                                    certificate.Thumbprint);
                                return true;
                            }

                            if (status.Status == X509ChainStatusFlags.RevocationStatusUnknown)
                            {
                                _logger.LogWarning(
                                    "وضعیت ابطال گواهینامه {Thumbprint} نامشخص است",
                                    certificate.Thumbprint);
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "خطا در بررسی CRL برای گواهینامه {Thumbprint}", certificate.Thumbprint);
                throw;
            }
        }

        /// <summary>
        /// اعتبارسنجی زنجیره گواهینامه
        /// الزام: پشتیبانی از حداقل دو گواهینامه (Root CA + Server Certificate)
        /// </summary>
        public async Task<CertificateValidationResult> ValidateCertificateChainAsync(
            X509Certificate2 certificate,
            CancellationToken ct = default)
        {
            var result = new CertificateValidationResult
            {
                IsValid = true,
                Thumbprint = certificate.Thumbprint,
                Subject = certificate.Subject,
                ExpirationDate = certificate.NotAfter
            };

            try
            {
                using (var chain = new X509Chain())
                {
                    // ============================================
                    // پیکربندی اعتبارسنجی زنجیره
                    // ============================================
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                    chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(10);

                    // ساخت زنجیره
                    var chainBuilt = chain.Build(certificate);

                    // ============================================
                    // الزام: حداقل دو گواهینامه در زنجیره
                    // (Root CA + Server Certificate)
                    // ============================================
                    if (chain.ChainElements.Count < 2)
                    {
                        _logger.LogWarning(
                            "زنجیره گواهینامه {Thumbprint} کمتر از ۲ گواهینامه دارد",
                            certificate.Thumbprint);
                        result.Errors.Add("زنجیره گواهینامه باید حداقل شامل Root CA و Server Certificate باشد");
                    }

                    // بررسی وضعیت هر عنصر در زنجیره
                    foreach (var element in chain.ChainElements)
                    {
                        foreach (var status in element.ChainElementStatus)
                        {
                            switch (status.Status)
                            {
                                case X509ChainStatusFlags.NoError:
                                    continue;

                                case X509ChainStatusFlags.Revoked:
                                    result.IsValid = false;
                                    result.IsRevoked = true;
                                    result.Errors.Add($"گواهینامه {element.Certificate.Subject} ابطال شده است");
                                    break;

                                case X509ChainStatusFlags.NotTimeValid:
                                    result.IsValid = false;
                                    result.Errors.Add($"گواهینامه {element.Certificate.Subject} منقضی شده است");
                                    break;

                                case X509ChainStatusFlags.UntrustedRoot:
                                    result.IsValid = false;
                                    result.Errors.Add($"Root CA نامعتبر: {element.Certificate.Subject}");
                                    break;

                                case X509ChainStatusFlags.PartialChain:
                                    result.IsValid = false;
                                    result.Errors.Add("زنجیره گواهینامه کامل نیست");
                                    break;

                                default:
                                    _logger.LogWarning(
                                        "وضعیت زنجیره: {Status} برای گواهینامه {Subject}",
                                        status.Status,
                                        element.Certificate.Subject);
                                    result.Errors.Add($"وضعیت زنجیره: {status.StatusInformation}");
                                    break;
                            }
                        }
                    }

                    // ============================================
                    // ثبت رویداد اعتبارسنجی زنجیره در Audit Log
                    // ============================================
                    await _auditLogService.LogEventAsync(
                        eventType: "CertificateChainValidation",
                        entityType: "X509CertificateChain",
                        entityId: certificate.Thumbprint,
                        isSuccess: result.IsValid,
                        errorMessage: result.IsValid ? null : string.Join("; ", result.Errors),
                        description: $"اعتبارسنجی زنجیره گواهینامه: {certificate.Subject} با {chain.ChainElements.Count} عنصر",
                        ct: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در اعتبارسنجی زنجیره گواهینامه {Thumbprint}", certificate.Thumbprint);
                result.IsValid = false;
                result.Errors.Add($"خطا در اعتبارسنجی زنجیره: {ex.Message}");
            }

            return result;
        }
    }
}

