using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// پیاده‌سازی سرویس اعتبارسنجی امضای دیجیتال به‌روزرسانی
    /// پیاده‌سازی الزام FPT_TUD_EXT.1.3 از استاندارد ISO 15408
    /// 
    /// این سرویس از RSA-SHA256 برای اعتبارسنجی امضای دیجیتال استفاده می‌کند.
    /// کلید عمومی از Configuration بارگذاری می‌شود.
    /// </summary>
    public class UpdateSignatureVerificationService : IUpdateSignatureVerificationService
    {
        private readonly IConfiguration _configuration;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<UpdateSignatureVerificationService> _logger;
        private RSA? _publicKey;
        private string _keyFingerprint = string.Empty;
        private DateTime? _keyLoadedAt;

        public UpdateSignatureVerificationService(
            IConfiguration configuration,
            IAuditLogService auditLogService,
            ILogger<UpdateSignatureVerificationService> logger)
        {
            _configuration = configuration;
            _auditLogService = auditLogService;
            _logger = logger;

            LoadPublicKey();
        }

        /// <summary>
        /// بارگذاری کلید عمومی از Configuration یا فایل
        /// </summary>
        private void LoadPublicKey()
        {
            try
            {
                // اولویت 1: بارگذاری از فایل PEM
                var publicKeyPath = _configuration["Security:UpdateSignature:PublicKeyPath"];
                if (!string.IsNullOrEmpty(publicKeyPath) && File.Exists(publicKeyPath))
                {
                    var pemContent = File.ReadAllText(publicKeyPath);
                    _publicKey = RSA.Create();
                    _publicKey.ImportFromPem(pemContent);
                    _logger.LogInformation("Public key loaded from file: {Path}", publicKeyPath);
                }
                // اولویت 2: بارگذاری از Configuration (Base64 encoded)
                else
                {
                    var publicKeyBase64 = _configuration["Security:UpdateSignature:PublicKey"];
                    if (!string.IsNullOrEmpty(publicKeyBase64))
                    {
                        _publicKey = RSA.Create();
                        
                        // بررسی فرمت PEM یا Raw
                        if (publicKeyBase64.Contains("-----BEGIN"))
                        {
                            _publicKey.ImportFromPem(publicKeyBase64);
                        }
                        else
                        {
                            // Raw Base64 encoded public key
                            var keyBytes = Convert.FromBase64String(publicKeyBase64);
                            _publicKey.ImportSubjectPublicKeyInfo(keyBytes, out _);
                        }
                        _logger.LogInformation("Public key loaded from configuration");
                    }
                    else
                    {
                        _logger.LogWarning("No public key configured for update signature verification. " +
                            "Configure Security:UpdateSignature:PublicKey or Security:UpdateSignature:PublicKeyPath");
                    }
                }

                // محاسبه Fingerprint کلید
                if (_publicKey != null)
                {
                    _keyFingerprint = ComputeKeyFingerprint(_publicKey);
                    _keyLoadedAt = DateTime.UtcNow;
                    _logger.LogInformation("Public key fingerprint: {Fingerprint}", _keyFingerprint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load public key for update signature verification");
                _publicKey = null;
            }
        }

        /// <summary>
        /// محاسبه Fingerprint کلید عمومی
        /// </summary>
        private static string ComputeKeyFingerprint(RSA key)
        {
            var publicKeyBytes = key.ExportSubjectPublicKeyInfo();
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(publicKeyBytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <inheritdoc />
        public async Task<SignatureVerificationResult> VerifyUpdateSignatureAsync(byte[] fileContent, string signature)
        {
            var result = new SignatureVerificationResult();

            try
            {
                if (_publicKey == null)
                {
                    result.IsValid = false;
                    result.ErrorCode = "NO_PUBLIC_KEY";
                    result.Message = "کلید عمومی برای اعتبارسنجی بارگذاری نشده است";
                    await LogVerificationEventAsync("UpdateSignatureVerification", false, result.Message);
                    return result;
                }

                if (fileContent == null || fileContent.Length == 0)
                {
                    result.IsValid = false;
                    result.ErrorCode = "EMPTY_CONTENT";
                    result.Message = "محتوای فایل خالی است";
                    return result;
                }

                if (string.IsNullOrEmpty(signature))
                {
                    result.IsValid = false;
                    result.ErrorCode = "EMPTY_SIGNATURE";
                    result.Message = "امضای دیجیتال ارائه نشده است";
                    return result;
                }

                // تبدیل امضا از Base64
                byte[] signatureBytes;
                try
                {
                    signatureBytes = Convert.FromBase64String(signature);
                }
                catch (FormatException)
                {
                    result.IsValid = false;
                    result.ErrorCode = "INVALID_SIGNATURE_FORMAT";
                    result.Message = "فرمت امضای دیجیتال نامعتبر است";
                    return result;
                }

                // اعتبارسنجی امضا با RSA-SHA256
                bool isValid = _publicKey.VerifyData(
                    fileContent,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                result.IsValid = isValid;
                result.KeyFingerprint = _keyFingerprint;

                if (isValid)
                {
                    result.Message = "امضای دیجیتال معتبر است";
                    result.Details = $"Verified with key: {_keyFingerprint.Substring(0, 16)}...";
                    await LogVerificationEventAsync("UpdateSignatureVerification", true, 
                        $"Signature verified successfully. Key: {_keyFingerprint.Substring(0, 16)}...");
                }
                else
                {
                    result.ErrorCode = "INVALID_SIGNATURE";
                    result.Message = "امضای دیجیتال نامعتبر است";
                    await LogVerificationEventAsync("UpdateSignatureVerification", false, 
                        "Signature verification failed - signature does not match");
                }

                return result;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Cryptographic error during signature verification");
                result.IsValid = false;
                result.ErrorCode = "CRYPTO_ERROR";
                result.Message = "خطای رمزنگاری در اعتبارسنجی امضا";
                await LogVerificationEventAsync("UpdateSignatureVerification", false, $"Crypto error: {ex.Message}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signature verification");
                result.IsValid = false;
                result.ErrorCode = "UNKNOWN_ERROR";
                result.Message = "خطای ناشناخته در اعتبارسنجی امضا";
                return result;
            }
        }

        /// <inheritdoc />
        public async Task<SignatureVerificationResult> VerifySignatureByHashAsync(string fileHash, string signature)
        {
            var result = new SignatureVerificationResult();

            try
            {
                if (_publicKey == null)
                {
                    result.IsValid = false;
                    result.ErrorCode = "NO_PUBLIC_KEY";
                    result.Message = "کلید عمومی برای اعتبارسنجی بارگذاری نشده است";
                    return result;
                }

                if (string.IsNullOrEmpty(fileHash))
                {
                    result.IsValid = false;
                    result.ErrorCode = "EMPTY_HASH";
                    result.Message = "Hash فایل ارائه نشده است";
                    return result;
                }

                if (string.IsNullOrEmpty(signature))
                {
                    result.IsValid = false;
                    result.ErrorCode = "EMPTY_SIGNATURE";
                    result.Message = "امضای دیجیتال ارائه نشده است";
                    return result;
                }

                // تبدیل Hash و امضا از Base64
                byte[] hashBytes;
                byte[] signatureBytes;
                try
                {
                    hashBytes = Convert.FromBase64String(fileHash);
                    signatureBytes = Convert.FromBase64String(signature);
                }
                catch (FormatException)
                {
                    result.IsValid = false;
                    result.ErrorCode = "INVALID_FORMAT";
                    result.Message = "فرمت Hash یا امضا نامعتبر است";
                    return result;
                }

                // اعتبارسنجی امضا روی Hash
                bool isValid = _publicKey.VerifyHash(
                    hashBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                result.IsValid = isValid;
                result.KeyFingerprint = _keyFingerprint;

                if (isValid)
                {
                    result.Message = "امضای دیجیتال معتبر است";
                    await LogVerificationEventAsync("HashSignatureVerification", true, "Signature verified by hash");
                }
                else
                {
                    result.ErrorCode = "INVALID_SIGNATURE";
                    result.Message = "امضای دیجیتال نامعتبر است";
                    await LogVerificationEventAsync("HashSignatureVerification", false, "Signature verification by hash failed");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during hash signature verification");
                result.IsValid = false;
                result.ErrorCode = "UNKNOWN_ERROR";
                result.Message = "خطای ناشناخته در اعتبارسنجی امضا";
                return result;
            }
        }

        /// <inheritdoc />
        public async Task<SignatureVerificationResult> VerifyVersionMetadataSignatureAsync(string versionMetadata, string signature)
        {
            if (string.IsNullOrEmpty(versionMetadata))
            {
                return new SignatureVerificationResult
                {
                    IsValid = false,
                    ErrorCode = "EMPTY_METADATA",
                    Message = "متادیتای نسخه ارائه نشده است"
                };
            }

            var metadataBytes = Encoding.UTF8.GetBytes(versionMetadata);
            return await VerifyUpdateSignatureAsync(metadataBytes, signature);
        }

        /// <inheritdoc />
        public string ComputeFileHash(byte[] content)
        {
            if (content == null || content.Length == 0)
                return string.Empty;

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(content);
            return Convert.ToBase64String(hash);
        }

        /// <inheritdoc />
        public string GenerateSignature(byte[] fileContent, string privateKeyPem)
        {
            if (fileContent == null || fileContent.Length == 0)
                throw new ArgumentException("محتوای فایل نمی‌تواند خالی باشد", nameof(fileContent));

            if (string.IsNullOrEmpty(privateKeyPem))
                throw new ArgumentException("کلید خصوصی ارائه نشده است", nameof(privateKeyPem));

            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            var signatureBytes = rsa.SignData(
                fileContent,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return Convert.ToBase64String(signatureBytes);
        }

        /// <inheritdoc />
        public bool IsPublicKeyLoaded()
        {
            return _publicKey != null;
        }

        /// <inheritdoc />
        public PublicKeyInfo GetPublicKeyInfo()
        {
            if (_publicKey == null)
            {
                return new PublicKeyInfo
                {
                    IsLoaded = false,
                    Fingerprint = string.Empty,
                    KeySize = 0,
                    Algorithm = "RSA"
                };
            }

            return new PublicKeyInfo
            {
                IsLoaded = true,
                Fingerprint = _keyFingerprint,
                KeySize = _publicKey.KeySize,
                Algorithm = "RSA",
                LoadedAt = _keyLoadedAt
            };
        }

        /// <summary>
        /// ثبت رویداد اعتبارسنجی در Audit Log
        /// </summary>
        private async Task LogVerificationEventAsync(string eventType, bool isSuccess, string description)
        {
            try
            {
                await _auditLogService.LogEventAsync(
                    eventType: eventType,
                    entityType: "UpdateSignature",
                    entityId: "Verification",
                    isSuccess: isSuccess,
                    description: description,
                    ct: default);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log verification event to audit log");
            }
        }
    }
}

