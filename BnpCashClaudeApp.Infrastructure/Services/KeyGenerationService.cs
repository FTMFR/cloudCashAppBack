using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// سرویس تولید کلید رمزنگاری
    /// پیاده‌سازی الزام FCS_CKM.1.1 از استاندارد ISO 15408
    /// 
    /// ویژگی‌ها:
    /// - استفاده از CSPRNG (RandomNumberGenerator)
    /// - پشتیبانی از طول‌های مختلف کلید
    /// - اعتبارسنجی قدرت کلید
    /// </summary>
    public class KeyGenerationService : IKeyGenerationService
    {
        private readonly ILogger<KeyGenerationService> _logger;
        private readonly ISecureMemoryService _secureMemoryService;
        private readonly ICryptographicAlgorithmPolicyService _cryptographicAlgorithmPolicyService;

        // حداقل طول کلید برای امنیت
        private const int MinimumKeyLengthBits = 128;
        private const int RecommendedKeyLengthBits = 256;
        private const int JwtKeyLengthBits = 512; // 64 bytes for HMAC-SHA512

        public KeyGenerationService(
            ILogger<KeyGenerationService> logger,
            ISecureMemoryService secureMemoryService,
            ICryptographicAlgorithmPolicyService cryptographicAlgorithmPolicyService)
        {
            _logger = logger;
            _secureMemoryService = secureMemoryService;
            _cryptographicAlgorithmPolicyService = cryptographicAlgorithmPolicyService;
        }

        /// <summary>
        /// تولید کلید متقارن امن با طول مشخص
        /// </summary>
        public byte[] GenerateSymmetricKey(int keyLengthBits = 256)
        {
            ValidateKeyLength(keyLengthBits);

            int keyLengthBytes = keyLengthBits / 8;
            byte[] key = new byte[keyLengthBytes];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }

            _logger.LogDebug(
                "Generated symmetric key with {KeyLength} bits using CSPRNG",
                keyLengthBits);

            return key;
        }

        /// <summary>
        /// تولید کلید متقارن امن و تبدیل به Base64
        /// </summary>
        public string GenerateSymmetricKeyBase64(int keyLengthBits = 256)
        {
            byte[] key = GenerateSymmetricKey(keyLengthBits);
            string base64Key = Convert.ToBase64String(key);

            // پاکسازی کلید از حافظه
            _secureMemoryService.ClearBytes(key);

            return base64Key;
        }

        /// <summary>
        /// تولید کلید JWT امن
        /// حداقل 512 بیت برای HMAC-SHA512
        /// </summary>
        public string GenerateJwtKey()
        {
            byte[] key = GenerateSymmetricKey(JwtKeyLengthBits);
            string base64Key = Convert.ToBase64String(key);

            // پاکسازی کلید از حافظه
            _secureMemoryService.ClearBytes(key);

            _logger.LogInformation(
                "Generated JWT key with {KeyLength} bits",
                JwtKeyLengthBits);

            return base64Key;
        }

        /// <summary>
        /// تولید کلید HMAC امن
        /// </summary>
        public byte[] GenerateHmacKey(int keyLengthBits = 256)
        {
            ValidateKeyLength(keyLengthBits);

            byte[] key = GenerateSymmetricKey(keyLengthBits);

            _logger.LogDebug(
                "Generated HMAC key with {KeyLength} bits",
                keyLengthBits);

            return key;
        }

        /// <summary>
        /// تولید Salt امن برای Password Hashing
        /// </summary>
        public byte[] GenerateSalt(int lengthBytes = 16)
        {
            if (lengthBytes < 8)
            {
                throw new ArgumentException(
                    "Salt length must be at least 8 bytes (64 bits)",
                    nameof(lengthBytes));
            }

            byte[] salt = new byte[lengthBytes];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            _logger.LogDebug(
                "Generated salt with {Length} bytes",
                lengthBytes);

            return salt;
        }

        /// <summary>
        /// تولید IV (Initialization Vector) امن برای AES
        /// </summary>
        public byte[] GenerateIV()
        {
            // AES block size is 128 bits = 16 bytes
            const int ivLengthBytes = 16;
            byte[] iv = new byte[ivLengthBytes];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            _logger.LogDebug("Generated IV with 128 bits for AES");

            return iv;
        }

        /// <summary>
        /// تولید توکن امن تصادفی
        /// </summary>
        public string GenerateSecureToken(int lengthBytes = 64)
        {
            if (lengthBytes < 32)
            {
                throw new ArgumentException(
                    "Token length must be at least 32 bytes (256 bits) for security",
                    nameof(lengthBytes));
            }

            byte[] tokenBytes = new byte[lengthBytes];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }

            // URL-safe Base64 encoding
            string token = Convert.ToBase64String(tokenBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            // پاکسازی بایت‌ها از حافظه
            _secureMemoryService.ClearBytes(tokenBytes);

            _logger.LogDebug(
                "Generated secure token with {Length} bytes",
                lengthBytes);

            return token;
        }

        /// <summary>
        /// تولید شناسه امن تصادفی (مثلاً برای API Key)
        /// </summary>
        public string GenerateSecureId(int lengthBytes = 32)
        {
            if (lengthBytes < 16)
            {
                throw new ArgumentException(
                    "ID length must be at least 16 bytes (128 bits)",
                    nameof(lengthBytes));
            }

            byte[] idBytes = new byte[lengthBytes];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(idBytes);
            }

            // Convert to hex string
            StringBuilder hex = new StringBuilder(lengthBytes * 2);
            foreach (byte b in idBytes)
            {
                hex.Append(b.ToString("x2"));
            }

            // پاکسازی بایت‌ها از حافظه
            _secureMemoryService.ClearBytes(idBytes);

            _logger.LogDebug(
                "Generated secure ID with {Length} bytes",
                lengthBytes);

            return hex.ToString();
        }

        /// <summary>
        /// اعتبارسنجی قدرت کلید
        /// </summary>
        public KeyStrengthValidationResult ValidateKeyStrength(byte[] key, int minimumBits = 256)
        {
            if (key == null || key.Length == 0)
            {
                return new KeyStrengthValidationResult
                {
                    IsValid = false,
                    KeyLengthBits = 0,
                    MinimumRequiredBits = minimumBits,
                    ErrorMessage = "Key is null or empty",
                    StrengthLevel = KeyStrengthLevel.Weak
                };
            }

            int keyLengthBits = key.Length * 8;
            KeyStrengthLevel strengthLevel = DetermineStrengthLevel(keyLengthBits);

            bool isValid = keyLengthBits >= minimumBits;

            return new KeyStrengthValidationResult
            {
                IsValid = isValid,
                KeyLengthBits = keyLengthBits,
                MinimumRequiredBits = minimumBits,
                ErrorMessage = isValid ? null : $"Key length ({keyLengthBits} bits) is less than minimum required ({minimumBits} bits)",
                StrengthLevel = strengthLevel
            };
        }

        /// <summary>
        /// اعتبارسنجی قدرت کلید Base64
        /// </summary>
        public KeyStrengthValidationResult ValidateKeyStrengthBase64(string keyBase64, int minimumBits = 256)
        {
            if (string.IsNullOrEmpty(keyBase64))
            {
                return new KeyStrengthValidationResult
                {
                    IsValid = false,
                    KeyLengthBits = 0,
                    MinimumRequiredBits = minimumBits,
                    ErrorMessage = "Key is null or empty",
                    StrengthLevel = KeyStrengthLevel.Weak
                };
            }

            try
            {
                byte[] key = Convert.FromBase64String(keyBase64);
                var result = ValidateKeyStrength(key, minimumBits);

                // پاکسازی کلید از حافظه
                _secureMemoryService.ClearBytes(key);

                return result;
            }
            catch (FormatException)
            {
                return new KeyStrengthValidationResult
                {
                    IsValid = false,
                    KeyLengthBits = 0,
                    MinimumRequiredBits = minimumBits,
                    ErrorMessage = "Key is not a valid Base64 string",
                    StrengthLevel = KeyStrengthLevel.Weak
                };
            }
        }

        /// <summary>
        /// تعیین سطح قدرت کلید
        /// </summary>
        private static KeyStrengthLevel DetermineStrengthLevel(int keyLengthBits)
        {
            return keyLengthBits switch
            {
                >= 256 => KeyStrengthLevel.VeryStrong,
                >= 192 => KeyStrengthLevel.Strong,
                >= 128 => KeyStrengthLevel.Medium,
                _ => KeyStrengthLevel.Weak
            };
        }

        /// <summary>
        /// اعتبارسنجی طول کلید
        /// </summary>
        private void ValidateKeyLength(int keyLengthBits)
        {
            if (keyLengthBits < MinimumKeyLengthBits)
            {
                throw new ArgumentException(
                    $"Key length must be at least {MinimumKeyLengthBits} bits. Requested: {keyLengthBits} bits",
                    nameof(keyLengthBits));
            }

            if (keyLengthBits % 8 != 0)
            {
                throw new ArgumentException(
                    "Key length must be a multiple of 8 bits",
                    nameof(keyLengthBits));
            }

            if (!_cryptographicAlgorithmPolicyService.IsApprovedManagedKeyLength(keyLengthBits))
            {
                throw new ArgumentException(
                    $"Key length must match approved cryptographic policy. Approved values: {string.Join(", ", _cryptographicAlgorithmPolicyService.GetApprovedManagedKeyLengths())}",
                    nameof(keyLengthBits));
            }

            if (keyLengthBits < RecommendedKeyLengthBits)
            {
                _logger.LogWarning(
                    "Key length {KeyLength} is below recommended minimum of {Recommended} bits",
                    keyLengthBits,
                    RecommendedKeyLengthBits);
            }
        }
    }
}

