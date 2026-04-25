using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// سرویس پس‌زمینه برای چرخش خودکار کلیدها
    /// ============================================
    /// پیاده‌سازی الزامات FCS_CKM از استاندارد ISO 15408
    /// 
    /// وظایف:
    /// - بررسی دوره‌ای نیاز به چرخش کلیدها
    /// - چرخش خودکار کلیدهای قدیمی
    /// - تخریب کلیدهای منقضی
    /// - ثبت Audit Log
    /// ============================================
    /// </summary>
    public class KeyRotationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<KeyRotationBackgroundService> _logger;
        private readonly IConfiguration _configuration;

        // تنظیمات پیش‌فرض
        private readonly TimeSpan _checkInterval;
        private readonly int _maxKeyAgeDays;
        private readonly int _gracePeriodMinutes;

        // لیست کلیدهایی که باید چرخش شوند
        private readonly List<string> _keyPurposes = new()
        {
            "JWT",
            "AES-Encryption",
            "HMAC-Signing",
            "API-Key"
        };

        public KeyRotationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<KeyRotationBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;

            // خواندن تنظیمات از Configuration
            _checkInterval = TimeSpan.FromHours(
                _configuration.GetValue<int>("Security:KeyRotation:CheckIntervalHours", 24));
            _maxKeyAgeDays = _configuration.GetValue<int>("Security:KeyRotation:MaxKeyAgeDays", 90);
            _gracePeriodMinutes = _configuration.GetValue<int>("Security:KeyRotation:GracePeriodMinutes", 60);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Key Rotation Background Service started. Check interval: {Interval}, Max key age: {MaxAge} days",
                _checkInterval,
                _maxKeyAgeDays);

            // تأخیر اولیه برای اطمینان از راه‌اندازی کامل سیستم
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformKeyMaintenanceAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during key maintenance");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Key Rotation Background Service stopped");
        }

        /// <summary>
        /// انجام عملیات نگهداری کلیدها
        /// </summary>
        private async Task PerformKeyMaintenanceAsync(CancellationToken ct)
        {
            _logger.LogDebug("Starting key maintenance cycle");

            using var scope = _serviceProvider.CreateScope();
            var keyManagementService = scope.ServiceProvider.GetRequiredService<IKeyManagementService>();
            var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

            // 1. تخریب کلیدهای منقضی
            await DestroyExpiredKeysAsync(keyManagementService, auditLogService, ct);

            // 2. بررسی و چرخش کلیدهای قدیمی
            await RotateOldKeysAsync(keyManagementService, auditLogService, ct);

            // 3. گزارش آمار
            await LogKeyStatisticsAsync(keyManagementService, ct);

            _logger.LogDebug("Key maintenance cycle completed");
        }

        /// <summary>
        /// تخریب کلیدهای منقضی
        /// </summary>
        private async Task DestroyExpiredKeysAsync(
            IKeyManagementService keyManagementService,
            IAuditLogService auditLogService,
            CancellationToken ct)
        {
            try
            {
                int destroyedCount = await keyManagementService.DestroyExpiredKeysAsync(ct);

                if (destroyedCount > 0)
                {
                    _logger.LogInformation(
                        "Destroyed {Count} expired keys during maintenance",
                        destroyedCount);

                    await auditLogService.LogEventAsync(
                        eventType: "ExpiredKeysDestroyed",
                        entityType: "CryptographicKey",
                        isSuccess: true,
                        description: $"Background service destroyed {destroyedCount} expired keys");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error destroying expired keys");
            }
        }

        /// <summary>
        /// چرخش کلیدهای قدیمی
        /// </summary>
        private async Task RotateOldKeysAsync(
            IKeyManagementService keyManagementService,
            IAuditLogService auditLogService,
            CancellationToken ct)
        {
            foreach (var purpose in _keyPurposes)
            {
                try
                {
                    bool needsRotation = await keyManagementService.NeedsRotationAsync(
                        purpose,
                        _maxKeyAgeDays,
                        ct);

                    if (needsRotation)
                    {
                        _logger.LogInformation(
                            "Key for purpose '{Purpose}' needs rotation (older than {MaxAge} days)",
                            purpose,
                            _maxKeyAgeDays);

                        var newKeyId = await keyManagementService.AutoRotateKeyAsync(
                            purpose,
                            256, // 256-bit key
                            _gracePeriodMinutes,
                            ct);

                        _logger.LogInformation(
                            "Rotated key for purpose '{Purpose}'. New key ID: {KeyId}",
                            purpose,
                            newKeyId);

                        await auditLogService.LogEventAsync(
                            eventType: "KeyAutoRotated",
                            entityType: "CryptographicKey",
                            entityId: newKeyId.ToString(),
                            isSuccess: true,
                            description: $"Background service auto-rotated key for purpose: {purpose}, Max age: {_maxKeyAgeDays} days");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error rotating key for purpose '{Purpose}'", purpose);
                }
            }
        }

        /// <summary>
        /// ثبت آمار کلیدها در لاگ
        /// </summary>
        private async Task LogKeyStatisticsAsync(
            IKeyManagementService keyManagementService,
            CancellationToken ct)
        {
            try
            {
                var statistics = await keyManagementService.GetKeyStatisticsAsync(ct);

                _logger.LogInformation(
                    "Key Statistics - Total: {Total}, Active: {Active}, Expired: {Expired}, Destroyed: {Destroyed}",
                    statistics.TotalKeys,
                    statistics.ActiveKeys,
                    statistics.ExpiredKeys,
                    statistics.DestroyedKeys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting key statistics");
            }
        }
    }
}

