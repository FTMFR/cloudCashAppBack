using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Entities.AuditLogSubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// سرویس حفاظت از داده‌های ممیزی
    /// پیاده‌سازی الزامات FAU_STG.3.1 و FAU_STG.4.1 از استاندارد ISO 15408
    /// 
    /// FAU_STG.3.1: اقدامات لازم در زمان از دست رفتن داده ممیزی
    /// - ارسال هشدار در صورت شکست ذخیره‌سازی
    /// - مکانیزم Retry با Exponential Backoff
    /// - ذخیره‌سازی جایگزین در فایل سیستم (Fallback)
    /// 
    /// FAU_STG.4.1: پیشگیری از اتلاف و از بین رفتن داده ممیزی
    /// - پشتیبان‌گیری خودکار به فرمت JSON
    /// - سیاست نگهداری (Retention Policy)
    /// - آرشیو داده‌های قدیمی
    /// 
    /// تنظیمات از جدول SecuritySettings خوانده می‌شوند
    /// در صورت عدم دسترسی به دیتابیس، از appsettings.json استفاده می‌شود (Fallback Config)
    /// </summary>
    public class AuditLogProtectionService : IAuditLogProtectionService
    {
        private readonly IDbContextFactory<LogDbContext> _contextFactory;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuditLogProtectionService> _logger;
        private readonly IHttpClientFactory? _httpClientFactory;
        private readonly ISmsService? _smsService;
        private readonly IHostEnvironment _hostEnvironment;
        
        // تنظیمات پیش‌فرض از appsettings.json (برای Bootstrap)
        private readonly string _defaultFallbackDirectory;
        private readonly string _defaultBackupDirectory;
        private readonly string _defaultArchiveDirectory;
        private readonly int _defaultMaxRetryAttempts;
        private readonly int _defaultRetentionDays;
        private readonly int _defaultArchiveAfterDays;
        
        // Cache برای تنظیمات از دیتابیس
        private AuditLogProtectionSettings? _cachedSettings;
        private DateTime _cacheExpiry = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        private static readonly SemaphoreSlim _settingsLock = new(1, 1);
        
        private DateTime? _lastSuccessfulSave;
        private DateTime? _lastFailure;
        private string? _lastErrorMessage;
        private int _failureCount24Hours;
        private DateTime _failureCountResetTime = DateTime.UtcNow;

        private static readonly SemaphoreSlim _fallbackLock = new(1, 1);
        private static readonly SemaphoreSlim _alertFileLock = new(1, 1);

        public AuditLogProtectionService(
            IDbContextFactory<LogDbContext> contextFactory,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<AuditLogProtectionService> logger,
            IHostEnvironment hostEnvironment,
            IHttpClientFactory? httpClientFactory = null,
            ISmsService? smsService = null)
        {
            _contextFactory = contextFactory;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _httpClientFactory = httpClientFactory;
            _smsService = smsService;

            // تنظیمات پیش‌فرض از appsettings.json (برای Bootstrap و Fallback)
            var auditProtection = configuration.GetSection("AuditLogProtection");
            
            _defaultFallbackDirectory = auditProtection["FallbackDirectory"] 
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audit-fallback");
            _defaultBackupDirectory = auditProtection["BackupDirectory"] 
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audit-backups");
            _defaultArchiveDirectory = auditProtection["ArchiveDirectory"] 
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audit-archives");
            
            _defaultMaxRetryAttempts = int.TryParse(auditProtection["MaxRetryAttempts"], out var retry) ? retry : 3;
            _defaultRetentionDays = int.TryParse(auditProtection["RetentionDays"], out var retention) ? retention : 365;
            _defaultArchiveAfterDays = int.TryParse(auditProtection["ArchiveAfterDays"], out var archive) ? archive : 90;

            // ایجاد دایرکتوری‌ها با تنظیمات پیش‌فرض
            EnsureDirectoriesExist();
        }

        #region Settings Management

        /// <summary>
        /// بارگذاری تنظیمات از دیتابیس (SecuritySettings)
        /// با Caching برای کارایی بهتر
        /// </summary>
        private async Task<AuditLogProtectionSettings> GetSettingsAsync(CancellationToken ct = default)
        {
            // بررسی Cache
            if (_cachedSettings != null && DateTime.UtcNow < _cacheExpiry)
            {
                return _cachedSettings;
            }

            await _settingsLock.WaitAsync(ct);
            try
            {
                // Double-check بعد از گرفتن Lock
                if (_cachedSettings != null && DateTime.UtcNow < _cacheExpiry)
                {
                    return _cachedSettings;
                }

                try
                {
                    // استفاده از IServiceProvider برای جلوگیری از Circular Dependency
                    using var scope = _serviceProvider.CreateScope();
                    var securitySettingsService = scope.ServiceProvider.GetService<ISecuritySettingsService>();
                    
                    if (securitySettingsService != null)
                    {
                        _cachedSettings = await securitySettingsService.GetAuditLogProtectionSettingsAsync(ct);
                        _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
                        
                        _logger.LogDebug("[FAU_STG] Loaded AuditLogProtection settings from database");
                        
                        // به‌روزرسانی دایرکتوری‌ها اگر تغییر کرده باشند
                        EnsureDirectoriesExist(_cachedSettings);
                        
                        return _cachedSettings;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[FAU_STG] Failed to load settings from database, using defaults");
                }

                // Fallback به تنظیمات پیش‌فرض
                _cachedSettings = GetDefaultSettings();
                _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
                
                return _cachedSettings;
            }
            finally
            {
                _settingsLock.Release();
            }
        }

        private AuditLogProtectionSettings GetDefaultSettings()
        {
            return new AuditLogProtectionSettings
            {
                IsEnabled = true,
                MaxRetryAttempts = _defaultMaxRetryAttempts,
                EnableAlertOnFailure = true,
                AlertEmailAddresses = string.Empty,
                AlertSmsNumbers = string.Empty,
                RetentionDays = _defaultRetentionDays,
                ArchiveAfterDays = _defaultArchiveAfterDays,
                BackupIntervalHours = 24,
                RetentionCheckIntervalHours = 24,
                FallbackRecoveryIntervalMinutes = 5,
                HealthCheckIntervalMinutes = 10,
                FallbackDirectory = _defaultFallbackDirectory,
                BackupDirectory = _defaultBackupDirectory,
                ArchiveDirectory = _defaultArchiveDirectory
            };
        }

        /// <summary>
        /// دریافت مسیر دایرکتوری Fallback
        /// </summary>
        private string GetFallbackDirectory(AuditLogProtectionSettings? settings = null)
        {
            return string.IsNullOrWhiteSpace(settings?.FallbackDirectory) 
                ? _defaultFallbackDirectory 
                : settings.FallbackDirectory;
        }

        /// <summary>
        /// دریافت مسیر دایرکتوری Backup
        /// </summary>
        private string GetBackupDirectory(AuditLogProtectionSettings? settings = null)
        {
            return string.IsNullOrWhiteSpace(settings?.BackupDirectory) 
                ? _defaultBackupDirectory 
                : settings.BackupDirectory;
        }

        /// <summary>
        /// دریافت مسیر دایرکتوری Archive
        /// </summary>
        private string GetArchiveDirectory(AuditLogProtectionSettings? settings = null)
        {
            return string.IsNullOrWhiteSpace(settings?.ArchiveDirectory) 
                ? _defaultArchiveDirectory 
                : settings.ArchiveDirectory;
        }

        #endregion

        private void EnsureDirectoriesExist(AuditLogProtectionSettings? settings = null)
        {
            try
            {
                var fallbackDir = GetFallbackDirectory(settings);
                var backupDir = GetBackupDirectory(settings);
                var archiveDir = GetArchiveDirectory(settings);

                if (!Directory.Exists(fallbackDir))
                    Directory.CreateDirectory(fallbackDir);
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);
                if (!Directory.Exists(archiveDir))
                    Directory.CreateDirectory(archiveDir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create audit log protection directories");
            }
        }

        #region FAU_STG.3.1 - Alert & Fallback

        /// <inheritdoc />
        public async Task<AuditLogSaveResult> SaveAuditLogWithProtectionAsync(
            AuditLogEntry entry,
            CancellationToken ct = default)
        {
            var result = new AuditLogSaveResult();
            Exception? lastException = null;
            
            // در Development: ساده‌سازی FAU_STG.3.1 - فقط یک تلاش بدون Retry و Fallback
            if (_hostEnvironment.IsDevelopment())
            {
                try
                {
                    await using var context = await _contextFactory.CreateDbContextAsync(ct);
                    var auditLog = CreateAuditLogMaster(entry);
                    context.AuditLogMasters.Add(auditLog);
                    await context.SaveChangesAsync(ct);

                    result.Success = true;
                    result.LogId = auditLog.Id;
                    result.UsedFallback = false;
                    result.RetryCount = 1;
                    _lastSuccessfulSave = DateTime.UtcNow;

                    _logger.LogDebug("Audit log saved successfully in Development mode. ID: {LogId}", auditLog.Id);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save audit log in Development mode");
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                    result.RetryCount = 1;
                    return result;
                }
            }
            
            // بارگذاری تنظیمات از دیتابیس
            var settings = await GetSettingsAsync(ct);
            var maxRetryAttempts = settings.MaxRetryAttempts > 0 ? settings.MaxRetryAttempts : _defaultMaxRetryAttempts;

            // تلاش برای ذخیره با Retry
            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                result.RetryCount = attempt;
                
                try
                {
                    await using var context = await _contextFactory.CreateDbContextAsync(ct);

                    var auditLog = CreateAuditLogMaster(entry);
                    context.AuditLogMasters.Add(auditLog);
                    await context.SaveChangesAsync(ct);

                    result.Success = true;
                    result.LogId = auditLog.Id;
                    result.UsedFallback = false;
                    _lastSuccessfulSave = DateTime.UtcNow;

                    _logger.LogDebug("Audit log saved successfully. ID: {LogId}, Attempt: {Attempt}", 
                        auditLog.Id, attempt);

                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, 
                        "Failed to save audit log. Attempt {Attempt}/{MaxAttempts}", 
                        attempt, maxRetryAttempts);

                    if (attempt < maxRetryAttempts)
                    {
                        // Exponential backoff
                        var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                        await Task.Delay(delay, ct);
                    }
                }
            }

            // اگر همه تلاش‌ها شکست خورد، از Fallback استفاده کن (فقط در Production)
            _logger.LogError(lastException, 
                "All retry attempts failed. Using fallback storage. EventType: {EventType}", 
                entry.EventType);

            // ثبت شکست
            await RecordFailureAsync(lastException?.Message ?? "Unknown error");

            // ارسال هشدار (اگر فعال باشد) - فقط در Production
            if (settings.EnableAlertOnFailure && !_hostEnvironment.IsDevelopment())
            {
                await SendStorageFailureAlertAsync(
                    lastException?.Message ?? "Database storage failed after all retries",
                    $"EventType: {entry.EventType}, User: {entry.UserName}",
                    settings,
                    ct);
            }

            // ذخیره در Fallback - فقط در Production
            if (!_hostEnvironment.IsDevelopment())
            {
                try
                {
                    await SaveToFallbackAsync(entry, settings, ct);
                    result.Success = true;
                    result.UsedFallback = true;
                    result.ErrorMessage = "Saved to fallback storage";
                    
                    _logger.LogWarning("Audit log saved to fallback storage. EventType: {EventType}", 
                        entry.EventType);
                }
                catch (Exception fallbackEx)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Both database and fallback failed: {fallbackEx.Message}";
                    
                    _logger.LogCritical(fallbackEx, 
                        "CRITICAL: Both database and fallback storage failed! Audit data may be lost!");
                }
            }
            else
            {
                // در Development: فقط ثبت خطا بدون Fallback
                result.Success = false;
                result.ErrorMessage = lastException?.Message ?? "Database storage failed";
                _logger.LogWarning("Audit log save failed in Development mode. No fallback used.");
            }

            return result;
        }

        /// <inheritdoc />
        public async Task SendStorageFailureAlertAsync(
            string errorMessage,
            string? additionalInfo = null,
            CancellationToken ct = default)
        {
            // در Development: غیرفعال کردن FAU_STG.3.1 - Alert
            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogDebug("[FAU_STG.3.1] Alert skipped in Development mode. Error: {Error}", errorMessage);
                await Task.CompletedTask;
                return;
            }
            
            var settings = await GetSettingsAsync(ct);
            await SendStorageFailureAlertAsync(errorMessage, additionalInfo, settings, ct);
        }

        /// <summary>
        /// ارسال هشدار با تنظیمات از پیش بارگذاری شده
        /// </summary>
        private async Task SendStorageFailureAlertAsync(
            string errorMessage,
            string? additionalInfo,
            AuditLogProtectionSettings settings,
            CancellationToken ct)
        {
            try
            {
                var alertMessage = new StringBuilder();
                alertMessage.AppendLine("⚠️ AUDIT LOG STORAGE FAILURE ALERT ⚠️");
                alertMessage.AppendLine($"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                alertMessage.AppendLine($"Error: {errorMessage}");
                
                if (!string.IsNullOrEmpty(additionalInfo))
                    alertMessage.AppendLine($"Additional Info: {additionalInfo}");
                
                alertMessage.AppendLine($"Failures in last 24h: {_failureCount24Hours}");
                alertMessage.AppendLine($"Environment: {_hostEnvironment.EnvironmentName}");

                // ثبت در لاگ سیستم
                _logger.LogCritical(
                    "[FAU_STG.3.1] Audit Storage Failure Alert: {Error}. Info: {Info}",
                    errorMessage, additionalInfo);

                // ذخیره هشدار در فایل
                var alertPayload = new
                {
                    AlertType = "AuditStorageFailure",
                    TimestampUtc = DateTime.UtcNow,
                    Error = errorMessage,
                    AdditionalInfo = additionalInfo,
                    FailureCountLast24Hours = _failureCount24Hours,
                    Environment = _hostEnvironment.EnvironmentName
                };

                var fallbackDir = GetFallbackDirectory(settings);
                var alertFile = Path.Combine(fallbackDir, "alerts.log");
                await AppendLineAsync(
                    alertFile,
                    alertMessage.ToString() + Environment.NewLine + "---",
                    ct);

                // SECURITY HARDENING: emit structured alert event for SIEM collectors.
                await WriteSiemAlertAsync(alertPayload, settings, ct);

                // ارسال ایمیل (اگر تنظیم شده باشد)
                if (!string.IsNullOrWhiteSpace(settings.AlertEmailAddresses))
                {
                    var emails = ParseCsv(settings.AlertEmailAddresses);
                    if (emails.Count > 0)
                    {
                        _logger.LogInformation(
                            "[FAU_STG.3.1] Sending alert email to: {Emails}",
                            string.Join(", ", emails));

                        try
                        {
                            await SendAlertEmailsAsync(
                                emails,
                                $"[FAU_STG.3.1] Audit storage failure ({_hostEnvironment.EnvironmentName})",
                                alertMessage.ToString(),
                                ct);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "[FAU_STG.3.1] Error sending alert email");
                        }
                    }
                }

                // ارسال پیامک (اگر تنظیم شده باشد)
                if (!string.IsNullOrWhiteSpace(settings.AlertSmsNumbers) && _smsService != null)
                {
                    var phones = settings.AlertSmsNumbers
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToList();

                    if (phones.Any())
                    {
                        _logger.LogInformation("[FAU_STG.3.1] Sending alert SMS to: {Phones}", 
                            string.Join(", ", phones.Select(MaskMobileNumber)));

                        try
                        {
                            var smsMessage = alertMessage.ToString();
                            var results = await _smsService.SendBulkAsync(phones, smsMessage, ct);

                            var successCount = results.Values.Count(r => r.IsSuccess);
                            var failureCount = results.Count - successCount;

                            if (successCount > 0)
                            {
                                _logger.LogInformation(
                                    "[FAU_STG.3.1] Alert SMS sent successfully to {SuccessCount} recipients",
                                    successCount);
                            }

                            if (failureCount > 0)
                            {
                                _logger.LogWarning(
                                    "[FAU_STG.3.1] Failed to send alert SMS to {FailureCount} recipients",
                                    failureCount);
                            }
                        }
                        catch (Exception smsEx)
                        {
                            _logger.LogError(smsEx, "[FAU_STG.3.1] Error sending alert SMS");
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(settings.AlertSmsNumbers) && _smsService == null)
                {
                    _logger.LogWarning("[FAU_STG.3.1] SMS service not available, cannot send alert SMS");
                }

                // SECURITY HARDENING: operational webhook channel for SIEM/SOC integrations.
                await SendWebhookAlertsAsync(alertPayload, settings, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send storage failure alert");
            }
        }

        /// <inheritdoc />
        public async Task<int> RecoverFallbackLogsAsync(CancellationToken ct = default)
        {
            // در Development: غیرفعال کردن FAU_STG.3.1 - Fallback Recovery
            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogDebug("[FAU_STG.3.1] Fallback recovery skipped in Development mode");
                return 0;
            }
            
            int recoveredCount = 0;
            var settings = await GetSettingsAsync(ct);
            var fallbackDir = GetFallbackDirectory(settings);

            await _fallbackLock.WaitAsync(ct);
            try
            {
                var fallbackFiles = Directory.GetFiles(fallbackDir, "*.json")
                    .OrderBy(f => f)
                    .ToList();

                if (!fallbackFiles.Any())
                    return 0;

                _logger.LogInformation("Starting recovery of {Count} fallback log files", fallbackFiles.Count);

                await using var context = await _contextFactory.CreateDbContextAsync(ct);

                foreach (var filePath in fallbackFiles)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(filePath, ct);
                        var entry = JsonSerializer.Deserialize<AuditLogEntry>(json);

                        if (entry != null)
                        {
                            var auditLog = CreateAuditLogMaster(entry);
                            auditLog.Description = $"[RECOVERED FROM FALLBACK] {auditLog.Description}";
                            
                            context.AuditLogMasters.Add(auditLog);
                            await context.SaveChangesAsync(ct);

                            // حذف فایل بعد از بازیابی موفق
                            File.Delete(filePath);
                            recoveredCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to recover fallback file: {FilePath}", filePath);
                    }
                }

                if (recoveredCount > 0)
                {
                    _logger.LogInformation("Successfully recovered {Count} audit logs from fallback", 
                        recoveredCount);
                }

                return recoveredCount;
            }
            finally
            {
                _fallbackLock.Release();
            }
        }

        /// <inheritdoc />
        public async Task<int> GetFallbackLogCountAsync(CancellationToken ct = default)
        {
            var settings = await GetSettingsAsync(ct);
            var fallbackDir = GetFallbackDirectory(settings);
            
            if (!Directory.Exists(fallbackDir))
                return 0;

            return Directory.GetFiles(fallbackDir, "*.json").Length;
        }

        /// <inheritdoc />
        public async Task<StorageHealthStatus> GetStorageHealthStatusAsync(CancellationToken ct = default)
        {
            var settings = await GetSettingsAsync(ct);
            var fallbackDir = GetFallbackDirectory(settings);
            
            var status = new StorageHealthStatus
            {
                LastSuccessfulSave = _lastSuccessfulSave,
                LastFailure = _lastFailure,
                FailureCountLast24Hours = _failureCount24Hours,
                LastErrorMessage = _lastErrorMessage,
                PendingFallbackLogs = await GetFallbackLogCountAsync(ct)
            };

            // بررسی سلامت دیتابیس
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync(ct);
                status.TotalLogsCount = await context.AuditLogMasters.LongCountAsync(ct);
                status.IsDatabaseHealthy = true;
            }
            catch
            {
                status.IsDatabaseHealthy = false;
            }

            // بررسی سلامت Fallback
            status.IsFallbackHealthy = Directory.Exists(fallbackDir);

            return status;
        }

        #endregion

        #region FAU_STG.4.1 - Backup & Retention

        /// <inheritdoc />
        public async Task<BackupResult> CreateBackupAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            // در Development: غیرفعال کردن FAU_STG.4.1 - Backup
            //if (_hostEnvironment.IsDevelopment())
            //{
            //    _logger.LogDebug("[FAU_STG.4.1] Backup skipped in Development mode");
            //    return new BackupResult
            //    {
            //        Success = true,
            //        FromDate = fromDate,
            //        ToDate = toDate,
            //        ErrorMessage = "Backup skipped in Development mode"
            //    };
            //}

            var result = new BackupResult
            {
                FromDate = fromDate,
                ToDate = toDate
            };
            
            var settings = await GetSettingsAsync(ct);
            var backupDir = GetBackupDirectory(settings);

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync(ct);

                var query = context.AuditLogMasters
                    .Include(a => a.Details)
                    .AsNoTracking();

                if (fromDate.HasValue)
                    query = query.Where(a => a.EventDateTime >= fromDate.Value);
                if (toDate.HasValue)
                    query = query.Where(a => a.EventDateTime <= toDate.Value);

                var logs = await query.ToListAsync(ct);

                if (!logs.Any())
                {
                    result.Success = true;
                    result.RecordsCount = 0;
                    result.ErrorMessage = "No records to backup in the specified date range";
                    return result;
                }

                // ایجاد فایل پشتیبان
                var backupId = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                var backupPath = Path.Combine(backupDir, $"{backupId}.json");

                var backupData = new
                {
                    BackupId = backupId,
                    CreatedAt = DateTime.UtcNow,
                    FromDate = fromDate,
                    ToDate = toDate,
                    RecordsCount = logs.Count,
                    Logs = logs.Select(l => new
                    {
                        l.Id,
                        l.EventDateTime,
                        l.EventType,
                        l.EntityType,
                        l.EntityId,
                        l.IsSuccess,
                        l.ErrorMessage,
                        l.IpAddress,
                        l.UserName,
                        l.UserId,
                        l.OperatingSystem,
                        l.UserAgent,
                        l.Description,
                        Details = l.Details.Select(d => new
                        {
                            d.FieldName,
                            d.OldValue,
                            d.NewValue,
                            d.DataType
                        })
                    })
                };

                var json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                await File.WriteAllTextAsync(backupPath, json, ct);

                var fileInfo = new FileInfo(backupPath);

                result.Success = true;
                result.BackupId = backupId;
                result.BackupPath = backupPath;
                result.RecordsCount = logs.Count;
                result.FileSizeBytes = fileInfo.Length;

                _logger.LogInformation(
                    "[FAU_STG.4.1] Backup created successfully. ID: {BackupId}, Records: {Count}, Size: {Size} bytes",
                    backupId, logs.Count, fileInfo.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create audit log backup");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <inheritdoc />
        public async Task<ArchiveResult> ArchiveOldLogsAsync(
            DateTime olderThan,
            CancellationToken ct = default)
        {
            // در Development: غیرفعال کردن FAU_STG.4.1 - Archive
            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogDebug("[FAU_STG.4.1] Archive skipped in Development mode");
                return new ArchiveResult
                {
                    Success = true,
                    OlderThan = olderThan,
                    ErrorMessage = "Archive skipped in Development mode"
                };
            }
            
            var result = new ArchiveResult { OlderThan = olderThan };
            var settings = await GetSettingsAsync(ct);
            var archiveDir = GetArchiveDirectory(settings);

            try
            {
                // ابتدا پشتیبان‌گیری
                var backupResult = await CreateBackupAsync(null, olderThan, ct);
                
                if (!backupResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Backup before archive failed: {backupResult.ErrorMessage}";
                    return result;
                }

                // انتقال به آرشیو
                if (!string.IsNullOrEmpty(backupResult.BackupPath) && File.Exists(backupResult.BackupPath))
                {
                    var archivePath = Path.Combine(archiveDir, Path.GetFileName(backupResult.BackupPath));
                    File.Move(backupResult.BackupPath, archivePath);
                    result.ArchivePath = archivePath;
                }

                // حذف از دیتابیس
                await using var context = await _contextFactory.CreateDbContextAsync(ct);
                
                var logsToArchive = await context.AuditLogMasters
                    .Where(a => a.EventDateTime < olderThan)
                    .ToListAsync(ct);

                context.AuditLogMasters.RemoveRange(logsToArchive);
                await context.SaveChangesAsync(ct);

                result.Success = true;
                result.ArchivedCount = logsToArchive.Count;

                _logger.LogInformation(
                    "[FAU_STG.4.1] Archived {Count} audit logs older than {Date}. Archive: {Path}",
                    logsToArchive.Count, olderThan, result.ArchivePath);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive old audit logs");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <inheritdoc />
        public async Task<RetentionResult> ApplyRetentionPolicyAsync(CancellationToken ct = default)
        {
            // در Development: غیرفعال کردن FAU_STG.4.1 - Retention Policy
            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogDebug("[FAU_STG.4.1] Retention policy skipped in Development mode");
                return new RetentionResult
                {
                    Success = true,
                    RetentionDays = 365,
                    ArchiveDays = 90,
                    ErrorMessage = "Retention policy skipped in Development mode"
                };
            }
            
            var settings = await GetSettingsAsync(ct);
            var retentionDays = settings.RetentionDays > 0 ? settings.RetentionDays : _defaultRetentionDays;
            var archiveAfterDays = settings.ArchiveAfterDays > 0 ? settings.ArchiveAfterDays : _defaultArchiveAfterDays;
            var archiveDir = GetArchiveDirectory(settings);
            
            var result = new RetentionResult
            {
                RetentionDays = retentionDays,
                ArchiveDays = archiveAfterDays
            };

            try
            {
                var now = DateTime.UtcNow;
                var archiveDate = now.AddDays(-archiveAfterDays);
                var deleteDate = now.AddDays(-retentionDays);

                _logger.LogInformation(
                    "[FAU_STG.4.1] Applying retention policy. Archive logs before: {ArchiveDate}, Delete before: {DeleteDate}",
                    archiveDate, deleteDate);

                // آرشیو لاگ‌های قدیمی
                var archiveResult = await ArchiveOldLogsAsync(archiveDate, ct);
                result.ArchivedCount = archiveResult.ArchivedCount;

                // حذف آرشیوهای خیلی قدیمی
                var oldArchives = Directory.GetFiles(archiveDir, "*.json")
                    .Select(f => new FileInfo(f))
                    .Where(f => f.CreationTimeUtc < deleteDate)
                    .ToList();

                foreach (var oldArchive in oldArchives)
                {
                    try
                    {
                        File.Delete(oldArchive.FullName);
                        result.DeletedCount++;
                        
                        _logger.LogInformation("Deleted old archive: {FilePath}", oldArchive.FullName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old archive: {FilePath}", oldArchive.FullName);
                    }
                }

                result.Success = true;

                _logger.LogInformation(
                    "[FAU_STG.4.1] Retention policy applied. Archived: {Archived}, Deleted: {Deleted}",
                    result.ArchivedCount, result.DeletedCount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply retention policy");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <inheritdoc />
        public async Task<AuditLogStatistics> GetStatisticsAsync(CancellationToken ct = default)
        {
            var stats = new AuditLogStatistics();

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync(ct);

                var now = DateTime.UtcNow;

                stats.TotalLogs = await context.AuditLogMasters.LongCountAsync(ct);
                stats.TodayLogs = await context.AuditLogMasters
                    .Where(a => a.EventDateTime >= now.Date)
                    .LongCountAsync(ct);
                stats.Last7DaysLogs = await context.AuditLogMasters
                    .Where(a => a.EventDateTime >= now.AddDays(-7))
                    .LongCountAsync(ct);
                stats.Last30DaysLogs = await context.AuditLogMasters
                    .Where(a => a.EventDateTime >= now.AddDays(-30))
                    .LongCountAsync(ct);

                stats.OldestLog = await context.AuditLogMasters
                    .OrderBy(a => a.EventDateTime)
                    .Select(a => a.EventDateTime)
                    .FirstOrDefaultAsync(ct);
                    
                stats.NewestLog = await context.AuditLogMasters
                    .OrderByDescending(a => a.EventDateTime)
                    .Select(a => a.EventDateTime)
                    .FirstOrDefaultAsync(ct);

                stats.SuccessfulLogs = await context.AuditLogMasters
                    .Where(a => a.IsSuccess)
                    .LongCountAsync(ct);
                stats.FailedLogs = stats.TotalLogs - stats.SuccessfulLogs;

                // آمار بر اساس نوع رویداد
                stats.LogsByEventType = await context.AuditLogMasters
                    .GroupBy(a => a.EventType)
                    .Select(g => new { EventType = g.Key, Count = g.LongCount() })
                    .ToDictionaryAsync(x => x.EventType, x => x.Count, ct);

                // بارگذاری تنظیمات برای مسیرها
                var settings = await GetSettingsAsync(ct);
                var backupDir = GetBackupDirectory(settings);
                var archiveDir = GetArchiveDirectory(settings);

                // تعداد پشتیبان‌ها
                if (Directory.Exists(backupDir))
                {
                    var backups = Directory.GetFiles(backupDir, "*.json");
                    stats.BackupsCount = backups.Length;
                    if (backups.Any())
                    {
                        stats.LastBackupDate = backups
                            .Select(f => new FileInfo(f).CreationTimeUtc)
                            .Max();
                    }
                }

                // تعداد آرشیوها
                if (Directory.Exists(archiveDir))
                {
                    var archives = Directory.GetFiles(archiveDir, "*.json");
                    stats.ArchivesCount = archives.Length;
                    if (archives.Any())
                    {
                        stats.LastArchiveDate = archives
                            .Select(f => new FileInfo(f).CreationTimeUtc)
                            .Max();
                    }
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audit log statistics");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<BackupInfo>> GetBackupsAsync(CancellationToken ct = default)
        {
            var backups = new List<BackupInfo>();
            var settings = await GetSettingsAsync(ct);
            var backupDir = GetBackupDirectory(settings);

            if (!Directory.Exists(backupDir))
                return backups;

            foreach (var filePath in Directory.GetFiles(backupDir, "*.json"))
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var backupInfo = new BackupInfo
                    {
                        BackupId = Path.GetFileNameWithoutExtension(filePath),
                        FilePath = filePath,
                        FileSizeBytes = fileInfo.Length,
                        CreatedAt = fileInfo.CreationTimeUtc
                    };

                    try
                    {
                        var json = await File.ReadAllTextAsync(filePath, ct);
                        var metadata = JsonSerializer.Deserialize<AuditBackupFile>(
                            json,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (metadata != null)
                        {
                            backupInfo.RecordsCount = metadata.RecordsCount > 0
                                ? metadata.RecordsCount
                                : metadata.Logs.Count;
                            backupInfo.FromDate = metadata.FromDate;
                            backupInfo.ToDate = metadata.ToDate;
                        }
                    }
                    catch (Exception metadataEx)
                    {
                        _logger.LogDebug(metadataEx, "[FAU_STG.4.1] Failed to parse backup metadata: {FilePath}", filePath);
                    }

                    backups.Add(backupInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read backup info: {FilePath}", filePath);
                }
            }

            return backups.OrderByDescending(b => b.CreatedAt);
        }

        /// <inheritdoc />
        public async Task<RestoreResult> RestoreFromBackupAsync(
            string backupId,
            CancellationToken ct = default)
        {
            var result = new RestoreResult { BackupId = backupId };
            var settings = await GetSettingsAsync(ct);
            var backupDir = GetBackupDirectory(settings);
            var archiveDir = GetArchiveDirectory(settings);

            try
            {
                var backupPath = Path.Combine(backupDir, $"{backupId}.json");
                
                if (!File.Exists(backupPath))
                {
                    // جستجو در آرشیو
                    backupPath = Path.Combine(archiveDir, $"{backupId}.json");
                }

                if (!File.Exists(backupPath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Backup not found: {backupId}";
                    return result;
                }

                var json = await File.ReadAllTextAsync(backupPath, ct);
                var backupData = JsonSerializer.Deserialize<AuditBackupFile>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (backupData?.Logs == null || backupData.Logs.Count == 0)
                {
                    result.Success = true;
                    result.RestoredCount = 0;
                    result.ErrorMessage = "Backup contains no logs to restore.";

                    _logger.LogInformation(
                        "[FAU_STG.4.1] Restore skipped, backup has no logs. BackupId: {BackupId}",
                        backupId);

                    return result;
                }

                await using var context = await _contextFactory.CreateDbContextAsync(ct);

                var minEventDate = backupData.Logs.Min(l => l.EventDateTime);
                var maxEventDate = backupData.Logs.Max(l => l.EventDateTime);

                var existingFingerprints = await context.AuditLogMasters
                    .Where(a => a.EventDateTime >= minEventDate && a.EventDateTime <= maxEventDate)
                    .Select(a => new AuditBackupLog
                    {
                        EventDateTime = a.EventDateTime,
                        EventType = a.EventType,
                        EntityType = a.EntityType,
                        EntityId = a.EntityId,
                        IsSuccess = a.IsSuccess,
                        ErrorMessage = a.ErrorMessage,
                        IpAddress = a.IpAddress,
                        UserName = a.UserName,
                        UserId = a.UserId,
                        OperatingSystem = a.OperatingSystem,
                        UserAgent = a.UserAgent,
                        Description = a.Description
                    })
                    .ToListAsync(ct);

                var existingSet = existingFingerprints
                    .Select(BuildRestoreFingerprint)
                    .ToHashSet(StringComparer.Ordinal);

                long restoredCount = 0;
                long skippedCount = 0;
                var now = DateTime.UtcNow;

                foreach (var log in backupData.Logs.OrderBy(l => l.EventDateTime))
                {
                    ct.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(log.EventType))
                    {
                        skippedCount++;
                        continue;
                    }

                    var fingerprint = BuildRestoreFingerprint(log);
                    if (existingSet.Contains(fingerprint))
                    {
                        skippedCount++;
                        continue;
                    }

                    var eventDateTime = log.EventDateTime == default ? now : log.EventDateTime;
                    var insertedBy = log.UserId ?? 0;

                    var restoredLog = new AuditLogMaster
                    {
                        EventDateTime = eventDateTime,
                        EventType = log.EventType,
                        EntityType = log.EntityType,
                        EntityId = log.EntityId,
                        IsSuccess = log.IsSuccess,
                        ErrorMessage = log.ErrorMessage,
                        IpAddress = log.IpAddress,
                        UserName = log.UserName,
                        UserId = log.UserId,
                        OperatingSystem = log.OperatingSystem,
                        UserAgent = log.UserAgent,
                        Description = log.Description,
                        TblUserGrpIdInsert = insertedBy
                    };
                    restoredLog.SetZamanInsert(eventDateTime);

                    if (log.Details != null)
                    {
                        foreach (var detail in log.Details.Where(d => !string.IsNullOrWhiteSpace(d.FieldName)))
                        {
                            var restoredDetail = new AuditLogDetail
                            {
                                FieldName = detail.FieldName!,
                                OldValue = detail.OldValue,
                                NewValue = detail.NewValue,
                                DataType = detail.DataType,
                                TblUserGrpIdInsert = insertedBy
                            };
                            restoredDetail.SetZamanInsert(eventDateTime);
                            restoredLog.Details.Add(restoredDetail);
                        }
                    }

                    context.AuditLogMasters.Add(restoredLog);
                    existingSet.Add(fingerprint);
                    restoredCount++;
                }

                if (restoredCount > 0)
                {
                    await context.SaveChangesAsync(ct);
                }

                result.Success = true;
                result.RestoredCount = restoredCount;
                result.ErrorMessage = skippedCount > 0
                    ? $"Restore completed. Restored {restoredCount} record(s), skipped {skippedCount} duplicate/invalid record(s)."
                    : null;

                _logger.LogInformation(
                    "[FAU_STG.4.1] Restore completed. BackupId: {BackupId}, Restored: {Restored}, Skipped: {Skipped}, Source: {BackupPath}",
                    backupId, restoredCount, skippedCount, backupPath);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore from backup: {BackupId}", backupId);
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        #endregion

        #region Private Methods

        private AuditLogMaster CreateAuditLogMaster(AuditLogEntry entry)
        {
            var now = DateTime.UtcNow;
            var auditLog = new AuditLogMaster
            {
                EventDateTime = now,
                EventType = entry.EventType,
                EntityType = entry.EntityType,
                EntityId = entry.EntityId,
                IsSuccess = entry.IsSuccess,
                ErrorMessage = entry.ErrorMessage,
                IpAddress = entry.IpAddress,
                UserName = entry.UserName,
                UserId = entry.UserId,
                OperatingSystem = entry.OperatingSystem,
                UserAgent = entry.UserAgent,
                Description = entry.Description,
                TblUserGrpIdInsert = entry.UserId ?? 0
            };
            
            auditLog.SetZamanInsert(now);

            if (entry.Changes != null && entry.Changes.Any())
            {
                foreach (var change in entry.Changes)
                {
                    var oldValueStr = change.Value.OldValue?.ToString() ?? string.Empty;
                    var newValueStr = change.Value.NewValue?.ToString() ?? string.Empty;

                    if (oldValueStr != newValueStr)
                    {
                        var detail = new AuditLogDetail
                        {
                            FieldName = change.Key,
                            OldValue = oldValueStr,
                            NewValue = newValueStr,
                            DataType = change.Value.OldValue?.GetType().Name ?? change.Value.NewValue?.GetType().Name,
                            TblUserGrpIdInsert = entry.UserId ?? 0
                        };
                        detail.SetZamanInsert(now);
                        auditLog.Details.Add(detail);
                    }
                }
            }

            return auditLog;
        }

        private async Task SaveToFallbackAsync(AuditLogEntry entry, AuditLogProtectionSettings settings, CancellationToken ct)
        {
            await _fallbackLock.WaitAsync(ct);
            try
            {
                var fallbackDir = GetFallbackDirectory(settings);
                var fileName = $"audit_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.json";
                var filePath = Path.Combine(fallbackDir, fileName);

                var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                await File.WriteAllTextAsync(filePath, json, ct);
            }
            finally
            {
                _fallbackLock.Release();
            }
        }

        private async Task AppendLineAsync(string filePath, string content, CancellationToken ct)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await _alertFileLock.WaitAsync(ct);
            try
            {
                await File.AppendAllTextAsync(filePath, content + Environment.NewLine, ct);
            }
            finally
            {
                _alertFileLock.Release();
            }
        }

        private async Task WriteSiemAlertAsync(object alertPayload, AuditLogProtectionSettings settings, CancellationToken ct)
        {
            try
            {
                var enabled = true;
                if (bool.TryParse(_configuration["AuditLogProtection:Alerting:Siem:Enabled"], out var parsedEnabled))
                {
                    enabled = parsedEnabled;
                }

                if (!enabled)
                {
                    return;
                }

                var configuredFilePath = _configuration["AuditLogProtection:Alerting:Siem:FilePath"];
                var siemFilePath = string.IsNullOrWhiteSpace(configuredFilePath)
                    ? Path.Combine(GetFallbackDirectory(settings), "siem-alerts.jsonl")
                    : configuredFilePath;

                var siemEvent = new
                {
                    EventType = "AuditStorageFailure",
                    TimestampUtc = DateTime.UtcNow,
                    Environment = _hostEnvironment.EnvironmentName,
                    Host = Environment.MachineName,
                    Payload = alertPayload
                };

                await AppendLineAsync(siemFilePath, JsonSerializer.Serialize(siemEvent), ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[FAU_STG.3.1] Failed to write SIEM alert event");
            }
        }

        private async Task SendWebhookAlertsAsync(object alertPayload, AuditLogProtectionSettings settings, CancellationToken ct)
        {
            try
            {
                var rawWebhookUrls = new List<string>();
                rawWebhookUrls.AddRange(ParseCsv(_configuration["AuditLogProtection:Alerting:WebhookUrls"]));
                rawWebhookUrls.AddRange(
                    _configuration.GetSection("AuditLogProtection:Alerting:Webhooks")
                        .GetChildren()
                        .Select(c => c.Value ?? string.Empty));

                var webhookUrls = rawWebhookUrls
                    .Select(v => v.Trim())
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Where(v => Uri.TryCreate(v, UriKind.Absolute, out var uri) &&
                                (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (webhookUrls.Count == 0)
                {
                    return;
                }

                using var httpClient = _httpClientFactory?.CreateClient() ?? new HttpClient();
                if (_httpClientFactory == null)
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                }

                var payload = JsonSerializer.Serialize(new
                {
                    AlertType = "AuditStorageFailure",
                    TimestampUtc = DateTime.UtcNow,
                    Environment = _hostEnvironment.EnvironmentName,
                    FallbackDirectory = GetFallbackDirectory(settings),
                    Data = alertPayload
                });

                foreach (var webhookUrl in webhookUrls)
                {
                    try
                    {
                        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                        using var response = await httpClient.PostAsync(webhookUrl, content, ct);

                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogWarning(
                                "[FAU_STG.3.1] Webhook alert failed. Url: {WebhookUrl}, Status: {StatusCode}",
                                webhookUrl,
                                response.StatusCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[FAU_STG.3.1] Failed to send webhook alert to {WebhookUrl}", webhookUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[FAU_STG.3.1] Failed preparing webhook alert dispatch");
            }
        }

        private static List<string> ParseCsv(string? csvValues)
        {
            if (string.IsNullOrWhiteSpace(csvValues))
            {
                return new List<string>();
            }

            return csvValues
                .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task SendAlertEmailsAsync(
            IReadOnlyList<string> recipients,
            string subject,
            string body,
            CancellationToken ct)
        {
            if (recipients.Count == 0)
            {
                return;
            }

            var smtpSection = _configuration.GetSection("AuditLogProtection:Alerting:Smtp");
            var host = smtpSection["Host"];
            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning(
                    "[FAU_STG.3.1] AlertEmailAddresses configured but SMTP host is missing (AuditLogProtection:Alerting:Smtp:Host).");
                return;
            }

            var port = int.TryParse(smtpSection["Port"], out var configuredPort) ? configuredPort : 25;
            var enableSsl = bool.TryParse(smtpSection["EnableSsl"], out var configuredEnableSsl) && configuredEnableSsl;
            var username = smtpSection["Username"];
            var password = smtpSection["Password"];
            var timeoutMs = int.TryParse(smtpSection["TimeoutMs"], out var configuredTimeoutMs) ? configuredTimeoutMs : 10000;

            var fromAddressValue = smtpSection["FromAddress"];
            if (string.IsNullOrWhiteSpace(fromAddressValue))
            {
                fromAddressValue = !string.IsNullOrWhiteSpace(username) && username.Contains("@")
                    ? username
                    : "no-reply@localhost.localdomain";
            }

            var fromDisplayName = smtpSection["FromDisplayName"] ?? "Audit Alert";

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddressValue, fromDisplayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            foreach (var recipient in recipients)
            {
                try
                {
                    message.To.Add(new MailAddress(recipient));
                }
                catch (FormatException)
                {
                    _logger.LogWarning("[FAU_STG.3.1] Invalid alert email address skipped: {Recipient}", recipient);
                }
            }

            if (message.To.Count == 0)
            {
                _logger.LogWarning("[FAU_STG.3.1] No valid alert email recipients found after validation");
                return;
            }

            using var smtpClient = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = timeoutMs
            };

            if (!string.IsNullOrWhiteSpace(username))
            {
                smtpClient.Credentials = new NetworkCredential(username, password ?? string.Empty);
            }
            else
            {
                smtpClient.UseDefaultCredentials = true;
            }

            ct.ThrowIfCancellationRequested();
            await smtpClient.SendMailAsync(message);

            _logger.LogInformation(
                "[FAU_STG.3.1] Alert email sent successfully to {Count} recipient(s)",
                message.To.Count);
        }

        private static string BuildRestoreFingerprint(AuditBackupLog log)
        {
            return string.Join("|",
                NormalizeRestoreValue(log.EventDateTime.ToString("O")),
                NormalizeRestoreValue(log.EventType),
                NormalizeRestoreValue(log.EntityType),
                NormalizeRestoreValue(log.EntityId),
                log.IsSuccess ? "1" : "0",
                NormalizeRestoreValue(log.ErrorMessage),
                NormalizeRestoreValue(log.IpAddress),
                NormalizeRestoreValue(log.UserName),
                log.UserId?.ToString() ?? string.Empty,
                NormalizeRestoreValue(log.OperatingSystem),
                NormalizeRestoreValue(log.UserAgent),
                NormalizeRestoreValue(log.Description));
        }

        private static string NormalizeRestoreValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private sealed class AuditBackupFile
        {
            public string? BackupId { get; set; }
            public DateTime? CreatedAt { get; set; }
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
            public long RecordsCount { get; set; }
            public List<AuditBackupLog> Logs { get; set; } = new();
        }

        private sealed class AuditBackupLog
        {
            public long Id { get; set; }
            public DateTime EventDateTime { get; set; }
            public string EventType { get; set; } = string.Empty;
            public string? EntityType { get; set; }
            public string? EntityId { get; set; }
            public bool IsSuccess { get; set; }
            public string? ErrorMessage { get; set; }
            public string? IpAddress { get; set; }
            public string? UserName { get; set; }
            public long? UserId { get; set; }
            public string? OperatingSystem { get; set; }
            public string? UserAgent { get; set; }
            public string? Description { get; set; }
            public List<AuditBackupDetail> Details { get; set; } = new();
        }

        private sealed class AuditBackupDetail
        {
            public string? FieldName { get; set; }
            public string? OldValue { get; set; }
            public string? NewValue { get; set; }
            public string? DataType { get; set; }
        }

        private async Task RecordFailureAsync(string errorMessage)
        {
            _lastFailure = DateTime.UtcNow;
            _lastErrorMessage = errorMessage;

            // Reset counter if more than 24 hours
            if (DateTime.UtcNow - _failureCountResetTime > TimeSpan.FromHours(24))
            {
                _failureCount24Hours = 0;
                _failureCountResetTime = DateTime.UtcNow;
            }

            _failureCount24Hours++;
            await Task.CompletedTask;
        }

        /// <summary>
        /// ماسک کردن شماره موبایل
        /// مثال: 09123456789 -> 0912***6789
        /// </summary>
        private static string MaskMobileNumber(string mobileNumber)
        {
            if (string.IsNullOrEmpty(mobileNumber) || mobileNumber.Length < 7)
                return mobileNumber;

            return mobileNumber.Substring(0, 4) + "***" + mobileNumber.Substring(mobileNumber.Length - 4);
        }

        #endregion
    }
}

