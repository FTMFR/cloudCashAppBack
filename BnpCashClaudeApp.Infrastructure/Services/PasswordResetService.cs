using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// پیاده‌سازی سرویس بازیابی رمز عبور با کد یکبار مصرف (OTP)
    /// ============================================
    /// - ارسال کد OTP از طریق وب‌سرویس پیامک
    /// - ذخیره کد در Memory Cache با انقضای محدود
    /// - محدودیت تعداد تلاش ناموفق (حداکثر 3 بار)
    /// - توکن ریست یکبار مصرف با انقضای 10 دقیقه
    /// ============================================
    /// </summary>
    public class PasswordResetService : IPasswordResetService
    {
        private readonly ILogger<PasswordResetService> _logger;
        private readonly IShobeSettingsService _shobeSettingsService;
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuditLogService _auditLogService;
        private readonly ISecureMemoryService _secureMemoryService;

        // ============================================
        // پیشوندهای کلیدهای کش - مجزا از MFA
        // ============================================
        private const string OtpCacheKeyPrefix = "PasswordReset_OTP_";
        private const string OtpAttemptsCacheKeyPrefix = "PasswordReset_Attempts_";
        private const string ResetTokenCacheKeyPrefix = "PasswordReset_Token_";

        // ============================================
        // تنظیمات
        // ============================================
        private const int MaxOtpAttempts = 3;
        private const int ResetTokenExpiryMinutes = 10;

        public PasswordResetService(
            ILogger<PasswordResetService> logger,
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
        /// تولید و ارسال کد OTP برای بازیابی رمز عبور
        /// </summary>
        public async Task<SmsSendResult> GenerateAndSendPasswordResetOtpAsync(
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
                // ذخیره کد در کش با کلید مجزا از MFA
                // ============================================
                var cacheKey = OtpCacheKeyPrefix + userId;
                _cache.Set(cacheKey, otpCode, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(otpExpirySeconds)
                });

                // ریست شمارنده تلاش‌های ناموفق
                var attemptsKey = OtpAttemptsCacheKeyPrefix + userId;
                _cache.Set(attemptsKey, 0, new MemoryCacheEntryOptions
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
                        "Password reset OTP sent successfully to {MaskedMobile} for user {UserId}",
                        MaskMobileNumber(mobileNumber),
                        userId);

                    await _auditLogService.LogEventAsync(
                        eventType: "PasswordReset_OTP_Sent",
                        entityType: "User",
                        entityId: userId.ToString(),
                        isSuccess: true,
                        description: $"کد بازیابی رمز عبور به شماره {MaskMobileNumber(mobileNumber)} ارسال شد",
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
                        "Failed to send password reset OTP SMS. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode,
                        responseContent);

                    await _auditLogService.LogEventAsync(
                        eventType: "PasswordReset_OTP_Send_Failed",
                        entityType: "User",
                        entityId: userId.ToString(),
                        isSuccess: false,
                        errorMessage: $"خطا در ارسال پیامک بازیابی: {response.StatusCode}",
                        description: $"خطا در ارسال OTP بازیابی به شماره {MaskMobileNumber(mobileNumber)}",
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
                _logger.LogError(ex, "Error sending password reset OTP to user {UserId}", userId);

                return new SmsSendResult
                {
                    IsSuccess = false,
                    ErrorMessage = "خطا در ارسال پیامک. لطفاً مجدداً تلاش کنید."
                };
            }
        }

        /// <summary>
        /// تایید کد OTP بازیابی رمز عبور
        /// پیاده‌سازی الزام FDP_RIP.2 - پاکسازی OTP از حافظه پس از استفاده
        /// </summary>
        public bool VerifyPasswordResetOtp(long userId, string otpCode)
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
                        // حذف کد پس از استفاده موفق (یکبار مصرف)
                        _cache.Remove(cacheKey);
                        // حذف شمارنده تلاش‌ها
                        _cache.Remove(OtpAttemptsCacheKeyPrefix + userId);
                        _logger.LogDebug("Password reset OTP verified successfully for user {UserId}", userId);
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
                _logger.LogWarning("Password reset OTP verification failed for user {UserId}", userId);
            }

            return result;
        }

        /// <summary>
        /// ثبت تلاش ناموفق OTP و بررسی محدودیت تلاش
        /// </summary>
        /// <returns>تعداد تلاش‌های باقی‌مانده (0 یعنی OTP باطل شد)</returns>
        public int RecordFailedOtpAttempt(long userId)
        {
            var attemptsKey = OtpAttemptsCacheKeyPrefix + userId;

            if (!_cache.TryGetValue(attemptsKey, out int currentAttempts))
            {
                currentAttempts = 0;
            }

            currentAttempts++;
            var remainingAttempts = MaxOtpAttempts - currentAttempts;

            if (remainingAttempts <= 0)
            {
                // حذف OTP - بیش از حد مجاز تلاش شده
                _cache.Remove(OtpCacheKeyPrefix + userId);
                _cache.Remove(attemptsKey);
                _logger.LogWarning("Password reset OTP invalidated due to max attempts for user {UserId}", userId);
                return 0;
            }

            // به‌روزرسانی شمارنده
            _cache.Set(attemptsKey, currentAttempts, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return remainingAttempts;
        }

        /// <summary>
        /// حذف OTP بازیابی رمز عبور از کش
        /// </summary>
        public void InvalidatePasswordResetOtp(long userId)
        {
            _cache.Remove(OtpCacheKeyPrefix + userId);
            _cache.Remove(OtpAttemptsCacheKeyPrefix + userId);
        }

        /// <summary>
        /// ذخیره توکن ریست رمز عبور در کش
        /// </summary>
        public void StorePasswordResetToken(long userId, string resetToken)
        {
            var cacheKey = ResetTokenCacheKeyPrefix + userId;
            _cache.Set(cacheKey, resetToken, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ResetTokenExpiryMinutes)
            });
        }

        /// <summary>
        /// اعتبارسنجی توکن ریست رمز عبور از کش
        /// </summary>
        public bool ValidatePasswordResetToken(long userId, string resetToken)
        {
            var cacheKey = ResetTokenCacheKeyPrefix + userId;

            if (_cache.TryGetValue(cacheKey, out string? storedToken))
            {
                return storedToken == resetToken;
            }

            return false;
        }

        /// <summary>
        /// حذف توکن ریست رمز عبور از کش (یکبار مصرف)
        /// </summary>
        public void InvalidatePasswordResetToken(long userId)
        {
            _cache.Remove(ResetTokenCacheKeyPrefix + userId);
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
    }
}
