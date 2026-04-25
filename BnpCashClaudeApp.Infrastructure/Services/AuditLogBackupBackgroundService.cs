using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// سرویس پس‌زمینه برای پشتیبان‌گیری و نگهداری داده‌های ممیزی
    /// پیاده‌سازی الزام FAU_STG.4.1 از استاندارد ISO 15408
    /// 
    /// وظایف:
    /// 1. پشتیبان‌گیری دوره‌ای خودکار
    /// 2. اعمال سیاست نگهداری (حذف داده‌های منقضی)
    /// 3. آرشیو داده‌های قدیمی
    /// 4. بازیابی لاگ‌های Fallback به دیتابیس اصلی
    /// 5. بررسی سلامت سیستم ذخیره‌سازی
    /// 
    /// تنظیمات (شامل IsEnabled) از جدول SecuritySettings خوانده می‌شوند (با Caching)
    /// در صورت عدم دسترسی به دیتابیس، از appsettings.json استفاده می‌شود (Fallback Config)
    /// </summary>
    public class AuditLogBackupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuditLogBackupBackgroundService> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        // تنظیمات پیش‌فرض از appsettings.json (فقط برای Bootstrap و Fallback)
        private readonly TimeSpan _defaultBackupInterval;
        private readonly TimeSpan _defaultRetentionCheckInterval;
        private readonly TimeSpan _defaultFallbackRecoveryInterval;
        private readonly TimeSpan _defaultHealthCheckInterval;
        private readonly bool _defaultEnabled;

        // Cache برای تنظیمات از دیتابیس
        private AuditLogProtectionSettings? _cachedSettings;
        private DateTime _settingsCacheExpiry = DateTime.MinValue;
        private static readonly TimeSpan SettingsCacheDuration = TimeSpan.FromMinutes(5);

        private DateTime _lastBackupTime = DateTime.MinValue;
        private DateTime _lastRetentionCheckTime = DateTime.MinValue;
        private DateTime _lastFallbackRecoveryTime = DateTime.MinValue;
        private DateTime _lastHealthCheckTime = DateTime.MinValue;

        public AuditLogBackupBackgroundService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<AuditLogBackupBackgroundService> logger,
            IHostEnvironment hostEnvironment)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
            _hostEnvironment = hostEnvironment;

            // تنظیمات پیش‌فرض از appsettings.json (فقط Bootstrap و Fallback)
            var auditProtection = configuration.GetSection("AuditLogProtection");
            
            _defaultEnabled = bool.TryParse(auditProtection["Enabled"], out var enabled) && enabled;
            
            var backupHours = int.TryParse(auditProtection["BackupIntervalHours"], out var bh) ? bh : 24;
            _defaultBackupInterval = TimeSpan.FromHours(backupHours);
            
            var retentionHours = int.TryParse(auditProtection["RetentionCheckIntervalHours"], out var rh) ? rh : 24;
            _defaultRetentionCheckInterval = TimeSpan.FromHours(retentionHours);
            
            var fallbackMinutes = int.TryParse(auditProtection["FallbackRecoveryIntervalMinutes"], out var fm) ? fm : 5;
            _defaultFallbackRecoveryInterval = TimeSpan.FromMinutes(fallbackMinutes);
            
            var healthMinutes = int.TryParse(auditProtection["HealthCheckIntervalMinutes"], out var hm) ? hm : 10;
            _defaultHealthCheckInterval = TimeSpan.FromMinutes(healthMinutes);
        }

        /// <summary>
        /// بارگذاری تنظیمات از دیتابیس با Caching
        /// </summary>
        private async Task<AuditLogProtectionSettings> GetSettingsAsync(CancellationToken ct)
        {
            if (_cachedSettings != null && DateTime.UtcNow < _settingsCacheExpiry)
            {
                return _cachedSettings;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var securitySettingsService = scope.ServiceProvider.GetService<ISecuritySettingsService>();
                
                if (securitySettingsService != null)
                {
                    _cachedSettings = await securitySettingsService.GetAuditLogProtectionSettingsAsync(ct);
                    _settingsCacheExpiry = DateTime.UtcNow.Add(SettingsCacheDuration);
                    return _cachedSettings;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[FAU_STG.4.1] Failed to load settings from database, using defaults");
            }

            // Fallback به تنظیمات پیش‌فرض
            return GetDefaultSettings();
        }

        private AuditLogProtectionSettings GetDefaultSettings()
        {
            return new AuditLogProtectionSettings
            {
                IsEnabled = _defaultEnabled,
                BackupIntervalHours = (int)_defaultBackupInterval.TotalHours,
                RetentionCheckIntervalHours = (int)_defaultRetentionCheckInterval.TotalHours,
                FallbackRecoveryIntervalMinutes = (int)_defaultFallbackRecoveryInterval.TotalMinutes,
                HealthCheckIntervalMinutes = (int)_defaultHealthCheckInterval.TotalMinutes
            };
        }

        /// <summary>
        /// دریافت فاصله زمانی پشتیبان‌گیری
        /// </summary>
        private TimeSpan GetBackupInterval(AuditLogProtectionSettings settings)
        {
            return settings.BackupIntervalHours > 0 
                ? TimeSpan.FromHours(settings.BackupIntervalHours) 
                : _defaultBackupInterval;
        }

        /// <summary>
        /// دریافت فاصله زمانی بررسی سیاست نگهداری
        /// </summary>
        private TimeSpan GetRetentionCheckInterval(AuditLogProtectionSettings settings)
        {
            return settings.RetentionCheckIntervalHours > 0 
                ? TimeSpan.FromHours(settings.RetentionCheckIntervalHours) 
                : _defaultRetentionCheckInterval;
        }

        /// <summary>
        /// دریافت فاصله زمانی بازیابی Fallback
        /// </summary>
        private TimeSpan GetFallbackRecoveryInterval(AuditLogProtectionSettings settings)
        {
            return settings.FallbackRecoveryIntervalMinutes > 0 
                ? TimeSpan.FromMinutes(settings.FallbackRecoveryIntervalMinutes) 
                : _defaultFallbackRecoveryInterval;
        }

        /// <summary>
        /// دریافت فاصله زمانی بررسی سلامت
        /// </summary>
        private TimeSpan GetHealthCheckInterval(AuditLogProtectionSettings settings)
        {
            return settings.HealthCheckIntervalMinutes > 0 
                ? TimeSpan.FromMinutes(settings.HealthCheckIntervalMinutes) 
                : _defaultHealthCheckInterval;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // در Development: غیرفعال کردن FAU_STG.4.1 - Background Service
            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogInformation("[FAU_STG.4.1] Audit Log Backup Background Service is disabled in Development mode");
                return;
            }

            _logger.LogInformation(
                "[FAU_STG.4.1] Audit Log Backup Background Service started. " +
                "Default intervals - Backup: {BackupInterval}, Retention: {RetentionInterval}, " +
                "Fallback recovery: {FallbackInterval}, Health check: {HealthInterval}. " +
                "IsEnabled will be loaded from database (SecuritySettings table). Default enabled: {DefaultEnabled}",
                _defaultBackupInterval, _defaultRetentionCheckInterval, 
                _defaultFallbackRecoveryInterval, _defaultHealthCheckInterval, _defaultEnabled);

            // کمی صبر کن تا اپلیکیشن کاملاً بالا بیاید
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    
                    // بارگذاری تنظیمات از دیتابیس (شامل IsEnabled)
                    var settings = await GetSettingsAsync(stoppingToken);
                    
                    // بررسی فعال بودن از دیتابیس
                    if (!settings.IsEnabled)
                    {
                        _logger.LogDebug("[FAU_STG.4.1] Audit log protection is disabled in database settings");
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                        continue;
                    }

                    // 1. بازیابی لاگ‌های Fallback
                    var fallbackInterval = GetFallbackRecoveryInterval(settings);
                    if (now - _lastFallbackRecoveryTime >= fallbackInterval)
                    {
                        await RecoverFallbackLogsAsync(stoppingToken);
                        _lastFallbackRecoveryTime = now;
                    }

                    // 2. بررسی سلامت
                    var healthInterval = GetHealthCheckInterval(settings);
                    if (now - _lastHealthCheckTime >= healthInterval)
                    {
                        await PerformHealthCheckAsync(stoppingToken);
                        _lastHealthCheckTime = now;
                    }

                    // 3. پشتیبان‌گیری
                    var backupInterval = GetBackupInterval(settings);
                    if (now - _lastBackupTime >= backupInterval)
                    {
                        await CreateBackupAsync(stoppingToken);
                        _lastBackupTime = now;
                    }

                    // 4. اعمال سیاست نگهداری
                    var retentionInterval = GetRetentionCheckInterval(settings);
                    if (now - _lastRetentionCheckTime >= retentionInterval)
                    {
                        await ApplyRetentionPolicyAsync(stoppingToken);
                        _lastRetentionCheckTime = now;
                    }

                    // صبر تا بررسی بعدی (هر 1 دقیقه)
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[FAU_STG.4.1] Error in Audit Log Backup Background Service");
                    
                    // صبر قبل از تلاش مجدد
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("[FAU_STG.4.1] Audit Log Backup Background Service stopped");
        }

        /// <summary>
        /// بازیابی لاگ‌های ذخیره شده در Fallback
        /// </summary>
        private async Task RecoverFallbackLogsAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var protectionService = scope.ServiceProvider.GetRequiredService<IAuditLogProtectionService>();

                var fallbackCount = await protectionService.GetFallbackLogCountAsync(ct);
                
                if (fallbackCount > 0)
                {
                    _logger.LogInformation(
                        "[FAU_STG.3.1] Found {Count} logs in fallback storage. Starting recovery...", 
                        fallbackCount);

                    var recovered = await protectionService.RecoverFallbackLogsAsync(ct);
                    
                    if (recovered > 0)
                    {
                        _logger.LogInformation(
                            "[FAU_STG.3.1] Successfully recovered {Count} audit logs from fallback", 
                            recovered);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[FAU_STG.3.1] Failed to recover fallback logs");
            }
        }

        /// <summary>
        /// بررسی سلامت سیستم ذخیره‌سازی
        /// </summary>
        private async Task PerformHealthCheckAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var protectionService = scope.ServiceProvider.GetRequiredService<IAuditLogProtectionService>();

                var status = await protectionService.GetStorageHealthStatusAsync(ct);

                if (!status.IsDatabaseHealthy)
                {
                    _logger.LogWarning(
                        "[FAU_STG.3.1] ALERT: Database storage is unhealthy! " +
                        "Fallback logs: {FallbackCount}, Failures 24h: {Failures}",
                        status.PendingFallbackLogs, status.FailureCountLast24Hours);

                    await protectionService.SendStorageFailureAlertAsync(
                        "Database health check failed",
                        $"Pending fallback logs: {status.PendingFallbackLogs}",
                        ct);
                }
                else if (status.PendingFallbackLogs > 0)
                {
                    _logger.LogInformation(
                        "[FAU_STG.4.1] Storage status: {Status}, Total logs: {Total}, Pending fallback: {Fallback}",
                        status.Status, status.TotalLogsCount, status.PendingFallbackLogs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[FAU_STG.3.1] Health check failed");
            }
        }

        /// <summary>
        /// ایجاد پشتیبان دوره‌ای
        /// </summary>
        private async Task CreateBackupAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var protectionService = scope.ServiceProvider.GetRequiredService<IAuditLogProtectionService>();

                _logger.LogInformation("[FAU_STG.4.1] Starting scheduled backup...");

                // پشتیبان‌گیری از لاگ‌های 7 روز اخیر
                var fromDate = DateTime.UtcNow.AddDays(-7);
                var result = await protectionService.CreateBackupAsync(fromDate, null, ct);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "[FAU_STG.4.1] Scheduled backup completed. ID: {BackupId}, Records: {Count}, Size: {Size} bytes",
                        result.BackupId, result.RecordsCount, result.FileSizeBytes);
                }
                else
                {
                    _logger.LogWarning(
                        "[FAU_STG.4.1] Scheduled backup failed: {Error}", 
                        result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FAU_STG.4.1] Failed to create scheduled backup");
            }
        }

        /// <summary>
        /// اعمال سیاست نگهداری
        /// </summary>
        private async Task ApplyRetentionPolicyAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var protectionService = scope.ServiceProvider.GetRequiredService<IAuditLogProtectionService>();

                _logger.LogInformation("[FAU_STG.4.1] Applying retention policy...");

                var result = await protectionService.ApplyRetentionPolicyAsync(ct);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "[FAU_STG.4.1] Retention policy applied. Archived: {Archived}, Deleted: {Deleted}",
                        result.ArchivedCount, result.DeletedCount);
                }
                else
                {
                    _logger.LogWarning(
                        "[FAU_STG.4.1] Retention policy application failed: {Error}", 
                        result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FAU_STG.4.1] Failed to apply retention policy");
            }
        }
    }
}

