using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Entities.AuditLogSubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// پیاده‌سازی سرویس Audit Log
    /// ============================================
    /// پیاده‌سازی الزامات FAU_STG.3.1 و FAU_STG.4.1 از استاندارد ISO 15408
    /// 
    /// FAU_STG.3.1: اقدامات لازم در زمان از دست رفتن داده ممیزی
    /// - Retry با Exponential Backoff
    /// - Fallback به فایل سیستم
    /// - ثبت هشدار در صورت شکست (از طریق IAuditLogProtectionService)
    /// 
    /// از DbContextFactory برای جلوگیری از خطای concurrent access استفاده می‌کند
    /// هر عملیات یک DbContext جداگانه ایجاد می‌کند
    /// 
    /// تنظیمات (MaxRetryAttempts, FallbackDirectory و ...) از جدول SecuritySettings خوانده می‌شوند
    /// در صورت عدم دسترسی به دیتابیس، از مقادیر پیش‌فرض استفاده می‌شود
    /// ============================================
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        private readonly IDbContextFactory<LogDbContext> _contextFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuditLogService>? _logger;
        private readonly IAuditLogProtectionService? _protectionService;
        private readonly string _defaultFallbackDirectory;
        private const int DefaultMaxRetryAttempts = 3;
        private static readonly SemaphoreSlim _fallbackLock = new(1, 1);
        
        // Cache برای تنظیمات از دیتابیس
        private AuditLogProtectionSettings? _cachedSettings;
        private DateTime _cacheExpiry = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public AuditLogService(
            IDbContextFactory<LogDbContext> contextFactory,
            IServiceProvider serviceProvider,
            ILogger<AuditLogService>? logger = null,
            IAuditLogProtectionService? protectionService = null)
        {
            _contextFactory = contextFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _protectionService = protectionService;
            _defaultFallbackDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audit-fallback");
            
            // اطمینان از وجود دایرکتوری Fallback پیش‌فرض
            if (!Directory.Exists(_defaultFallbackDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_defaultFallbackDirectory);
                }
                catch { /* Ignore */ }
            }
        }

        #region Settings Management

        /// <summary>
        /// بارگذاری تنظیمات از دیتابیس (SecuritySettings) با Caching
        /// در صورت عدم دسترسی به دیتابیس، از مقادیر پیش‌فرض استفاده می‌شود
        /// </summary>
        private async Task<AuditLogProtectionSettings> GetSettingsAsync(CancellationToken ct = default)
        {
            // بررسی Cache
            if (_cachedSettings != null && DateTime.UtcNow < _cacheExpiry)
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
                    _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

                    _logger?.LogDebug("[FAU_STG] AuditLogService loaded settings from database");

                    // اطمینان از وجود دایرکتوری Fallback
                    var fallbackDir = GetFallbackDirectory(_cachedSettings);
                    if (!Directory.Exists(fallbackDir))
                    {
                        try { Directory.CreateDirectory(fallbackDir); } catch { }
                    }

                    return _cachedSettings;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[FAU_STG] AuditLogService failed to load settings from database, using defaults");
            }

            // Fallback به مقادیر پیش‌فرض
            _cachedSettings = GetDefaultSettings();
            _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
            return _cachedSettings;
        }

        private AuditLogProtectionSettings GetDefaultSettings()
        {
            return new AuditLogProtectionSettings
            {
                IsEnabled = true,
                MaxRetryAttempts = DefaultMaxRetryAttempts,
                FallbackDirectory = _defaultFallbackDirectory
            };
        }

        /// <summary>
        /// دریافت مسیر دایرکتوری Fallback از تنظیمات
        /// </summary>
        private string GetFallbackDirectory(AuditLogProtectionSettings? settings = null)
        {
            return string.IsNullOrWhiteSpace(settings?.FallbackDirectory)
                ? _defaultFallbackDirectory
                : settings.FallbackDirectory;
        }

        #endregion

        public async Task<long> LogEventAsync(
            string eventType,
            string? entityType = null,
            string? entityId = null,
            bool isSuccess = true,
            string? errorMessage = null,
            string? ipAddress = null,
            string? userName = null,
            long? userId = null,
            string? operatingSystem = null,
            string? userAgent = null,
            string? description = null,
            List<AuditLogDetail>? details = null,
            CancellationToken ct = default)
        {
            // ============================================
            // اگر ProtectionService موجود باشد، از آن استفاده کن
            // این سرویس شامل Retry، Fallback و ارسال هشدار (SMS/Email) است
            // ============================================
            if (_protectionService != null)
            {
                try
                {
                    // تبدیل List<AuditLogDetail> به Dictionary برای AuditLogEntry
                    Dictionary<string, (object? OldValue, object? NewValue)>? changes = null;
                    if (details != null && details.Any())
                    {
                        changes = new Dictionary<string, (object? OldValue, object? NewValue)>();
                        foreach (var detail in details)
                        {
                            changes[detail.FieldName] = (detail.OldValue, detail.NewValue);
                        }
                    }

                    var entry = new AuditLogEntry
                    {
                        EventType = eventType,
                        EntityType = entityType,
                        EntityId = entityId,
                        IsSuccess = isSuccess,
                        ErrorMessage = errorMessage,
                        IpAddress = ipAddress,
                        UserName = userName,
                        UserId = userId,
                        OperatingSystem = operatingSystem,
                        UserAgent = userAgent,
                        Description = description,
                        Changes = changes
                    };

                    var result = await _protectionService.SaveAuditLogWithProtectionAsync(entry, ct);
                    
                    if (result.Success)
                    {
                        return result.LogId ?? -1;
                    }
                    
                    // اگر Fallback استفاده شد، -1 برمی‌گرداند
                    return result.UsedFallback ? -1 : -1;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, 
                        "[FAU_STG.3.1] Error in protection service, falling back to direct save. EventType: {EventType}",
                        eventType);
                    // ادامه با روش قدیمی
                }
            }

            // ============================================
            // روش قدیمی (Fallback) - فقط اگر ProtectionService موجود نباشد
            // FAU_STG.3.1: تلاش برای ذخیره با Retry
            // تنظیمات از دیتابیس خوانده می‌شوند
            // ============================================
            var settings = await GetSettingsAsync(ct);
            var maxRetryAttempts = settings.MaxRetryAttempts > 0 ? settings.MaxRetryAttempts : DefaultMaxRetryAttempts;
            var fallbackDir = GetFallbackDirectory(settings);
            
            Exception? lastException = null;

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    await using var context = await _contextFactory.CreateDbContextAsync(ct);

                    var now = DateTime.UtcNow;
                    var auditLog = new AuditLogMaster
                    {
                        EventDateTime = now,
                        EventType = eventType,
                        EntityType = entityType,
                        EntityId = entityId,
                        IsSuccess = isSuccess,
                        ErrorMessage = errorMessage,
                        IpAddress = ipAddress,
                        UserName = userName,
                        UserId = userId,
                        OperatingSystem = operatingSystem,
                        UserAgent = userAgent,
                        Description = description,
                        TblUserGrpIdInsert = userId ?? 0
                    };
                    // تنظیم تاریخ به شمسی
                    auditLog.SetZamanInsert(now);

                    if (details != null && details.Any())
                    {
                        foreach (var detail in details)
                        {
                            detail.SetZamanInsert(now); // تاریخ به شمسی
                            detail.TblUserGrpIdInsert = userId ?? 0;
                            auditLog.Details.Add(detail);
                        }
                    }

                    context.AuditLogMasters.Add(auditLog);
                    await context.SaveChangesAsync(ct);

                    return auditLog.Id;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger?.LogWarning(ex, 
                        "[FAU_STG.3.1] Audit log save failed. Attempt {Attempt}/{Max}. EventType: {EventType}",
                        attempt, maxRetryAttempts, eventType);

                    if (attempt < maxRetryAttempts)
                    {
                        // Exponential backoff
                        var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                        await Task.Delay(delay, ct);
                    }
                }
            }

            // ============================================
            // FAU_STG.3.1: Fallback به فایل سیستم
            // ============================================
            _logger?.LogError(lastException, 
                "[FAU_STG.3.1] All retry attempts failed. Using fallback storage. EventType: {EventType}",
                eventType);

            try
            {
                await SaveToFallbackAsync(new
                {
                    EventType = eventType,
                    EntityType = entityType,
                    EntityId = entityId,
                    IsSuccess = isSuccess,
                    ErrorMessage = errorMessage,
                    IpAddress = ipAddress,
                    UserName = userName,
                    UserId = userId,
                    OperatingSystem = operatingSystem,
                    UserAgent = userAgent,
                    Description = description,
                    Details = details?.Select(d => new
                    {
                        d.FieldName,
                        d.OldValue,
                        d.NewValue,
                        d.DataType
                    }),
                    FailedAt = DateTime.UtcNow,
                    FailureReason = lastException?.Message
                }, fallbackDir, ct);

                _logger?.LogWarning(
                    "[FAU_STG.3.1] Audit log saved to fallback storage. EventType: {EventType}",
                    eventType);
            }
            catch (Exception fallbackEx)
            {
                _logger?.LogCritical(fallbackEx,
                    "[FAU_STG.3.1] CRITICAL: Both database and fallback storage failed! Audit data may be lost! EventType: {EventType}",
                    eventType);
            }

            return -1; // نشان‌دهنده ذخیره در Fallback
        }

        /// <summary>
        /// ذخیره‌سازی در Fallback Storage
        /// مسیر دایرکتوری از تنظیمات دیتابیس خوانده می‌شود
        /// </summary>
        private async Task SaveToFallbackAsync(object entry, string fallbackDirectory, CancellationToken ct)
        {
            await _fallbackLock.WaitAsync(ct);
            try
            {
                // اطمینان از وجود دایرکتوری
                if (!Directory.Exists(fallbackDirectory))
                {
                    try { Directory.CreateDirectory(fallbackDirectory); } catch { }
                }

                var fileName = $"audit_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.json";
                var filePath = Path.Combine(fallbackDirectory, fileName);

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

        public async Task<long> LogEntityChangeAsync(
            string eventType,
            string entityType,
            string entityId,
            Dictionary<string, (object? oldValue, object? newValue)> changes,
            string? ipAddress = null,
            string? userName = null,
            long? userId = null,
            string? operatingSystem = null,
            string? userAgent = null,
            CancellationToken ct = default)
        {
            var details = new List<AuditLogDetail>();

            foreach (var change in changes)
            {
                var oldValueStr = change.Value.oldValue?.ToString() ?? string.Empty;
                var newValueStr = change.Value.newValue?.ToString() ?? string.Empty;

                // فقط اگر تغییر داشته باشیم، ثبت می‌کنیم
                if (oldValueStr != newValueStr)
                {
                    details.Add(new AuditLogDetail
                    {
                        FieldName = change.Key,
                        OldValue = oldValueStr,
                        NewValue = newValueStr,
                        DataType = change.Value.oldValue?.GetType().Name ?? change.Value.newValue?.GetType().Name
                    });
                }
            }

            return await LogEventAsync(
                eventType: eventType,
                entityType: entityType,
                entityId: entityId,
                isSuccess: true,
                ipAddress: ipAddress,
                userName: userName,
                userId: userId,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                details: details,
                ct: ct);
        }
    }
}
