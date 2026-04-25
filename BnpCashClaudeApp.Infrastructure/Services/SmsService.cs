using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// سرویس ارسال پیامک
    /// استفاده از همان API که در MfaService استفاده شده است
    /// </summary>
    public class SmsService : ISmsService
    {
        private readonly ILogger<SmsService> _logger;
        private readonly IShobeSettingsService _shobeSettingsService;
        private readonly IHttpClientFactory _httpClientFactory;

        public SmsService(
            ILogger<SmsService> logger,
            IShobeSettingsService shobeSettingsService,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _shobeSettingsService = shobeSettingsService;
            _httpClientFactory = httpClientFactory;
        }


        ////// <inheritdoc />
        public async Task<SmsSendResult> SendAsync(string mobileNumber, string message, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(mobileNumber))
            {
                return new SmsSendResult
                {
                    IsSuccess = false,
                    ErrorMessage = "شماره موبایل نمی‌تواند خالی باشد"
                };
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return new SmsSendResult
                {
                    IsSuccess = false,
                    ErrorMessage = "متن پیامک نمی‌تواند خالی باشد"
                };
            }

            try
            {
                // ============================================
                // تنظیمات از دیتابیس (tblShobeSettings)
                // ============================================
                var smsSettings = await _shobeSettingsService.GetSmsSettingsAsync(null, ct);
                var apiKey = smsSettings.ApiKey;
                var senderNumber = smsSettings.SenderNumber;
                var baseUrl = smsSettings.BaseUrl;
                message += "\n لغو 11";
                // ============================================
                // ساخت URL برای ارسال پیامک
                // ============================================
                var url = $"{baseUrl}?note_arr[]={Uri.EscapeDataString(message)}&api_key={apiKey}&receiver_number={mobileNumber}&sender_number={senderNumber}";

                // ============================================
                // ارسال درخواست HTTP
                // ============================================
                using var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(url, ct);
                var responseContent = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "SMS sent successfully to {MaskedMobile}",
                        MaskMobileNumber(mobileNumber));

                    return new SmsSendResult
                    {
                        IsSuccess = true,
                        MaskedMobileNumber = MaskMobileNumber(mobileNumber)
                    };
                }
                else
                {
                    _logger.LogError(
                        "Failed to send SMS. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode,
                        responseContent);

                    return new SmsSendResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"خطا در ارسال پیامک: {response.StatusCode}",
                        MaskedMobileNumber = MaskMobileNumber(mobileNumber)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {MobileNumber}", MaskMobileNumber(mobileNumber));

                return new SmsSendResult
                {
                    IsSuccess = false,
                    ErrorMessage = "خطا در ارسال پیامک. لطفاً مجدداً تلاش کنید.",
                    MaskedMobileNumber = MaskMobileNumber(mobileNumber)
                };
            }
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, SmsSendResult>> SendBulkAsync(
            IEnumerable<string> mobileNumbers, 
            string message, 
            CancellationToken ct = default)
        {
            var results = new Dictionary<string, SmsSendResult>();

            if (mobileNumbers == null || !mobileNumbers.Any())
            {
                _logger.LogWarning("No mobile numbers provided for bulk SMS");
                return results;
            }

            var phoneList = mobileNumbers
                .Where(phone => !string.IsNullOrWhiteSpace(phone))
                .Distinct()
                .ToList();

            if (!phoneList.Any())
            {
                _logger.LogWarning("No valid mobile numbers found after filtering");
                return results;
            }

            _logger.LogInformation("Sending bulk SMS to {Count} recipients", phoneList.Count);

            // ارسال به هر شماره به صورت موازی
            var tasks = phoneList.Select(async phone =>
            {
                var result = await SendAsync(phone, message, ct);
                return new { Phone = phone, Result = result };
            });

            var completedTasks = await Task.WhenAll(tasks);

            foreach (var task in completedTasks)
            {
                results[task.Phone] = task.Result;
            }

            var successCount = results.Values.Count(r => r.IsSuccess);
            var failureCount = results.Count - successCount;

            _logger.LogInformation(
                "Bulk SMS completed. Success: {SuccessCount}, Failed: {FailureCount}",
                successCount,
                failureCount);

            return results;
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

