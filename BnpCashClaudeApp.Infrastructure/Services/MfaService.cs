using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// پیاده‌سازی سرویس احرازهویت چندگانه (MFA) با پیامک
    /// الزام FIA_UAU.5 از پروفایل حفاظتی (ISO 15408)
    /// ============================================
    /// - ارسال کد OTP از طریق وب‌سرویس پیامک
    /// - ذخیره کد در Memory Cache با انقضای محدود
    /// - کدهای بازیابی امن
    /// ============================================
    /// </summary>
    public class MfaService : IMfaService
    {
        private readonly ILogger<MfaService> _logger;
        private readonly IShobeSettingsService _shobeSettingsService;
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuditLogService _auditLogService;
        private readonly ISecureMemoryService _secureMemoryService;

        // ============================================
        // پیشوند کلیدهای کش
        // ============================================
        private const string OtpCacheKeyPrefix = "MFA_OTP_";

        // ============================================
        // تنظیمات پیش‌فرض
        // ============================================
        private const int DefaultOtpLength = 6;
        private const int DefaultOtpExpirySeconds = 120; // ۲ دقیقه

        public MfaService(
            ILogger<MfaService> logger,
            IShobeSettingsService shobeSettingsService,
            IMemoryCache cache,
            IHttpClientFactory httpClientFactory,
            IAuditLogService auditLogService,
            ISecureMemoryService secureMemoryService)
        {
            _logger = logger;
            _shobeSettingsService = shobeSettingsService;
            _cache = cache;
            _httpClientFactory = httpClientFactory;
            _auditLogService = auditLogService;
            _secureMemoryService = secureMemoryService;
        }

        /// <summary>
        /// تولید و ارسال کد OTP به شماره موبایل
        /// </summary>
        public async Task<SmsSendResult> GenerateAndSendOtpAsync(
            string mobileNumber, 
            long userId, 
            CancellationToken ct = default)
        {
            try
            {
                // ============================================
                // تنظیمات از دیتابیس (tblShobeSettings)
                // ============================================
                var smsSettings = await _shobeSettingsService.GetSmsSettingsAsync(null, ct);
                var apiKey = smsSettings.ApiKey;
                var senderNumber = smsSettings.SenderNumber;
                var baseUrl = smsSettings.BaseUrl;
                var otpLength = smsSettings.OtpLength;
                var otpExpirySeconds = smsSettings.OtpExpirySeconds;

                // ============================================
                // تولید کد OTP تصادفی
                // ============================================
                var otpCode = GenerateOtpCode(otpLength);

                // ============================================
                // ذخیره کد در کش
                // ============================================
                var cacheKey = OtpCacheKeyPrefix + userId;
                _cache.Set(cacheKey, otpCode, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(otpExpirySeconds)
                });

                // ============================================
                // متن پیامک
                // ============================================
                var messageTemplate = smsSettings.MessageTemplate;
                var smsText = string.Format(messageTemplate + "\n لغو 11", otpCode, otpExpirySeconds);

                // ============================================
                // ارسال پیامک
                // ============================================
                var url = $"{baseUrl}?note_arr[]={Uri.EscapeDataString(smsText)}&api_key={apiKey}&receiver_number={mobileNumber}&sender_number={senderNumber}";

                using var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(url, ct);
                var responseContent = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "OTP SMS sent successfully to {MaskedMobile} for user {UserId}",
                        MaskMobileNumber(mobileNumber),
                        userId);

                    await _auditLogService.LogEventAsync(
                        eventType: "MFA_OTP_Sent",
                        entityType: "User",
                        entityId: userId.ToString(),
                        isSuccess: true,
                        description: $"کد OTP به شماره {MaskMobileNumber(mobileNumber)} ارسال شد",
                        ct: ct);

                    return new SmsSendResult
                    {
                        IsSuccess = true,
                        ExpirySeconds = otpExpirySeconds,
                        MaskedMobileNumber = MaskMobileNumber(mobileNumber)
                    };
                }
                else
                {
                    _logger.LogError(
                        "Failed to send OTP SMS. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode,
                        responseContent);

                    await _auditLogService.LogEventAsync(
                        eventType: "MFA_OTP_Send_Failed",
                        entityType: "User",
                        entityId: userId.ToString(),
                        isSuccess: false,
                        errorMessage: $"خطا در ارسال پیامک: {response.StatusCode}",
                        description: $"خطا در ارسال OTP به شماره {MaskMobileNumber(mobileNumber)}",
                        ct: ct);

                    return new SmsSendResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "خطا در ارسال پیامک. لطفاً مجدداً تلاش کنید."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP SMS to user {UserId}", userId);

                return new SmsSendResult
                {
                    IsSuccess = false,
                    ErrorMessage = "خطا در ارسال پیامک. لطفاً مجدداً تلاش کنید."
                };
            }
        }

        /// <summary>
        /// تایید کد OTP
        /// پیاده‌سازی الزام FDP_RIP.2 - پاکسازی OTP از حافظه پس از استفاده
        /// </summary>
        public bool VerifyOtp(long userId, string otpCode)
        {
            if (string.IsNullOrWhiteSpace(otpCode))
            {
                return false;
            }

            var cacheKey = OtpCacheKeyPrefix + userId;
            bool result = false;

            try
            {
                if (_cache.TryGetValue(cacheKey, out string? storedOtp))
                {
                    // مقایسه امن OTP
                    var trimmedOtp = otpCode.Trim();
                    result = storedOtp == trimmedOtp;

                    if (result)
                    {
                        // حذف کد پس از استفاده موفق
                        _cache.Remove(cacheKey);
                        _logger.LogDebug("OTP verified successfully for user {UserId}", userId);
                    }
                }
            }
            finally
            {
                // ============================================
                // پاکسازی OTP از حافظه (FDP_RIP.2)
                // ============================================
                var otpCopy = otpCode;
                _secureMemoryService.ClearString(ref otpCopy);
            }

            if (!result)
            {
                _logger.LogWarning("OTP verification failed for user {UserId}", userId);
            }

            return result;
        }

        /// <summary>
        /// حذف کد OTP از کش
        /// </summary>
        public void InvalidateOtp(long userId)
        {
            var cacheKey = OtpCacheKeyPrefix + userId;
            _cache.Remove(cacheKey);
        }

        /// <summary>
        /// تولید کد OTP تصادفی
        /// </summary>
        private string GenerateOtpCode(int length)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            
            var number = BitConverter.ToUInt32(bytes, 0);
            var maxValue = (uint)Math.Pow(10, length);
            var code = (number % maxValue).ToString().PadLeft(length, '0');
            
            return code;
        }

        /// <summary>
        /// ماسک کردن شماره موبایل
        /// مثال: 09123456789 -> 0912***6789
        /// </summary>
        private string MaskMobileNumber(string mobileNumber)
        {
            if (string.IsNullOrEmpty(mobileNumber) || mobileNumber.Length < 7)
                return mobileNumber;

            return mobileNumber.Substring(0, 4) + "***" + mobileNumber.Substring(mobileNumber.Length - 4);
        }

        /// <summary>
        /// تولید کدهای بازیابی
        /// ============================================
        /// فرمت: XXXX-XXXX (۸ کاراکتر با خط تیره)
        /// ============================================
        /// </summary>
        public string[] GenerateRecoveryCodes(int count = 10)
        {
            var codes = new string[count];
            using var rng = RandomNumberGenerator.Create();

            for (int i = 0; i < count; i++)
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var code = Convert.ToHexString(bytes).ToUpper();
                codes[i] = $"{code.Substring(0, 4)}-{code.Substring(4, 4)}";
            }

            return codes;
        }

        /// <summary>
        /// هش کردن کد بازیابی
        /// </summary>
        public string HashRecoveryCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return string.Empty;

            // حذف خط تیره و تبدیل به حروف بزرگ
            var normalizedCode = code.Replace("-", "").ToUpperInvariant();

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedCode));
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// تایید کد بازیابی
        /// </summary>
        public bool VerifyRecoveryCode(string hashedCode, string providedCode)
        {
            if (string.IsNullOrWhiteSpace(hashedCode) || string.IsNullOrWhiteSpace(providedCode))
                return false;

            var providedHash = HashRecoveryCode(providedCode);
            return hashedCode == providedHash;
        }
    }
}
