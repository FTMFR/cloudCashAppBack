using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// Background Service برای بررسی دوره‌ای صحت داده‌های حساس
    /// پیاده‌سازی الزام FDP_SDI.2.2 از استاندارد ISO 15408
    /// 
    /// این سرویس به صورت دوره‌ای تمام Entityهای حساس را بررسی می‌کند
    /// و در صورت شناسایی نقض صحت، رویداد را در Audit Log ثبت می‌کند.
    /// </summary>
    public class DataIntegrityVerificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataIntegrityVerificationBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _verificationInterval;

        public DataIntegrityVerificationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<DataIntegrityVerificationBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;

            // خواندن فاصله زمانی بررسی از Configuration (پیش‌فرض: هر 24 ساعت)
            var intervalHours = _configuration.GetValue<int>("Security:DataIntegrity:VerificationIntervalHours", 24);
            _verificationInterval = TimeSpan.FromHours(intervalHours);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Data Integrity Verification Background Service started. Verification interval: {Interval}",
                _verificationInterval);

            // تأخیر اولیه برای اطمینان از راه‌اندازی کامل سیستم
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("شروع بررسی دوره‌ای صحت داده‌های حساس");

                    using var scope = _serviceProvider.CreateScope();
                    var integrityService = scope.ServiceProvider.GetRequiredService<IDataIntegrityService>();
                    
                    var result = await integrityService.VerifyAllEntitiesIntegrityAsync();

                    if (result.TotalViolations > 0)
                    {
                        _logger.LogWarning(
                            "تعداد {TotalCount} مورد نقض صحت داده شناسایی شد. تفکیک: {Breakdown}",
                            result.TotalViolations,
                            result.ViolationsByEntityType.Count > 0 
                                ? string.Join(", ", result.ViolationsByEntityType.Select(kv => $"{kv.Key}: {kv.Value}"))
                                : "هیچ نقضی شناسایی نشد");
                    }
                    else
                    {
                        _logger.LogInformation("تمام داده‌های حساس معتبر هستند");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطا در بررسی دوره‌ای صحت داده‌ها");
                }

                await Task.Delay(_verificationInterval, stoppingToken);
            }

            _logger.LogInformation("Data Integrity Verification Background Service stopped");
        }
    }
}

