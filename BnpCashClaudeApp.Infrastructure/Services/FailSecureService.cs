using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// پیاده‌سازی سرویس Fail-Secure
    /// ============================================
    /// پیاده‌سازی الزام FPT_FLS.1.1 (الزام 46)
    /// حفظ وضعیت امن در زمان شکست
    /// ============================================
    /// 
    /// این سرویس:
    /// 1. عملیات حساس را با حفاظت Fail-Secure اجرا می‌کند
    /// 2. شکست‌ها را ثبت و مانیتور می‌کند
    /// 3. در صورت شکست‌های متوالی، حالت امن را فعال می‌کند
    /// 4. تمام رویدادها را در Audit Log ثبت می‌کند
    /// </summary>
    public class FailSecureService : IFailSecureService
    {
        private readonly ILogger<FailSecureService> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly IMemoryCache _cache;
        private readonly FailSecureSettings _settings;

        // کلیدهای Cache
        private const string SecureModeKey = "FailSecure_System_SecureMode";
        private const string SecureModeReasonKey = "FailSecure_System_SecureMode_Reason";
        private const string SecureModeActivatedAtKey = "FailSecure_System_SecureMode_ActivatedAt";

        // مسیر ذخیره‌سازی فایل‌های لاگ شکست (وقتی دیتابیس در دسترس نیست)
        private const string FailureLogDirectory = "logs/fail-secure";

        // ذخیره‌سازی شکست‌های اخیر برای مانیتورینگ
        private static readonly ConcurrentQueue<FailureRecord> _recentFailures = new();
        private static readonly object _failureLock = new();
        private static readonly object _fileLock = new();

        public FailSecureService(
            ILogger<FailSecureService> logger,
            IAuditLogService auditLogService,
            IMemoryCache cache,
            IOptions<FailSecureSettings> settings)
        {
            _logger = logger;
            _auditLogService = auditLogService;
            _cache = cache;
            _settings = settings.Value;
        }

        /// <summary>
        /// اجرای عملیات با حفاظت Fail-Secure
        /// در صورت خطا، مقدار امن پیش‌فرض برگردانده می‌شود
        /// </summary>
        public async Task<T> ExecuteSecureAsync<T>(
            Func<Task<T>> operation,
            T failSafeDefault,
            string operationName,
            CancellationToken ct = default)
        {
            // اگر سیستم در حالت امن است، مستقیماً مقدار امن برگردان
            if (IsSystemInSecureMode() && _settings.DenyAllInSecureMode)
            {
                _logger.LogWarning(
                    "FAIL-SECURE: System is in secure mode. Returning fail-safe default for operation: {OperationName}",
                    operationName);
                return failSafeDefault;
            }

            try
            {
                return await operation();
            }
            catch (OperationCanceledException)
            {
                // لغو عملیات توسط کاربر - این یک شکست امنیتی نیست
                throw;
            }
            catch (Exception ex)
            {
                // ============================================
                // FPT_FLS.1.1: Fail-Secure - رفتار امن در خطا
                // ============================================

                _logger.LogError(ex,
                    "FAIL-SECURE: Operation {OperationName} failed. Returning fail-safe default. Exception: {ExceptionType}",
                    operationName,
                    ex.GetType().Name);

                // ثبت شکست
                await RecordFailureAsync(
                    failureType: ex.GetType().Name,
                    operationName: operationName,
                    details: ex.Message,
                    ct: ct);

                // ثبت در Audit Log
                await LogFailSecureEventAsync(operationName, ex, ct);

                // بررسی نیاز به فعال‌سازی حالت امن
                await CheckAndActivateSecureModeIfNeededAsync(ct);

                // برگرداندن مقدار امن پیش‌فرض
                return failSafeDefault;
            }
        }

        /// <summary>
        /// اجرای عملیات void با حفاظت Fail-Secure
        /// </summary>
        public async Task ExecuteSecureAsync(
            Func<Task> operation,
            string operationName,
            CancellationToken ct = default)
        {
            // اگر سیستم در حالت امن است، عملیات را نادیده بگیر
            if (IsSystemInSecureMode() && _settings.DenyAllInSecureMode)
            {
                _logger.LogWarning(
                    "FAIL-SECURE: System is in secure mode. Skipping operation: {OperationName}",
                    operationName);
                return;
            }

            try
            {
                await operation();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "FAIL-SECURE: Operation {OperationName} failed silently. Exception: {ExceptionType}",
                    operationName,
                    ex.GetType().Name);

                await RecordFailureAsync(
                    failureType: ex.GetType().Name,
                    operationName: operationName,
                    details: ex.Message,
                    ct: ct);

                await LogFailSecureEventAsync(operationName, ex, ct);
                await CheckAndActivateSecureModeIfNeededAsync(ct);

                // عملیات به صورت امن نادیده گرفته می‌شود
            }
        }

        /// <summary>
        /// بررسی فعال بودن حالت امن سیستم
        /// </summary>
        public bool IsSystemInSecureMode()
        {
            return _cache.TryGetValue(SecureModeKey, out bool isActive) && isActive;
        }

        /// <summary>
        /// فعال‌سازی حالت امن سیستم
        /// </summary>
        public async Task ActivateSecureModeAsync(string reason, CancellationToken ct = default)
        {
            var activatedAt = DateTime.UtcNow;

            _cache.Set(SecureModeKey, true);
            _cache.Set(SecureModeReasonKey, reason);
            _cache.Set(SecureModeActivatedAtKey, activatedAt);

            _logger.LogCritical(
                "🔒 SECURE MODE ACTIVATED: {Reason} at {ActivatedAt}",
                reason,
                activatedAt);

            try
            {
                await _auditLogService.LogEventAsync(
                    eventType: "SecureModeActivated",
                    entityType: "System",
                    entityId: "FailSecure",
                    isSuccess: true,
                    description: $"🔒 SECURE MODE ACTIVATED: {reason}",
                    ct: ct);
            }
            catch
            {
                // ثبت لاگ نباید مانع از فعال‌سازی حالت امن شود
            }
        }

        /// <summary>
        /// غیرفعال‌سازی حالت امن سیستم
        /// </summary>
        public async Task DeactivateSecureModeAsync(long deactivatedBy, CancellationToken ct = default)
        {
            _cache.Remove(SecureModeKey);
            _cache.Remove(SecureModeReasonKey);
            _cache.Remove(SecureModeActivatedAtKey);

            _logger.LogWarning(
                "🔓 SECURE MODE DEACTIVATED by user {UserId}",
                deactivatedBy);

            try
            {
                await _auditLogService.LogEventAsync(
                    eventType: "SecureModeDeactivated",
                    entityType: "System",
                    entityId: "FailSecure",
                    isSuccess: true,
                    userId: (int)deactivatedBy,
                    description: $"🔓 SECURE MODE DEACTIVATED by administrator (UserId: {deactivatedBy})",
                    ct: ct);
            }
            catch
            {
                // ثبت لاگ نباید مانع از غیرفعال‌سازی شود
            }

            // پاکسازی تاریخچه شکست‌ها
            ClearRecentFailures();
        }

        /// <summary>
        /// ثبت رویداد شکست
        /// </summary>
        public async Task RecordFailureAsync(
            string failureType,
            string operationName,
            string details,
            CancellationToken ct = default)
        {
            var record = new FailureRecord
            {
                FailureType = failureType,
                OperationName = operationName,
                Details = details,
                OccurredAt = DateTime.UtcNow
            };

            _recentFailures.Enqueue(record);

            // پاکسازی رکوردهای قدیمی
            CleanupOldFailures();

            await Task.CompletedTask;
        }

        /// <summary>
        /// دریافت تعداد شکست‌های اخیر
        /// </summary>
        public int GetRecentFailureCount(TimeSpan timeWindow)
        {
            var cutoff = DateTime.UtcNow - timeWindow;
            return _recentFailures.Count(f => f.OccurredAt >= cutoff);
        }

        /// <summary>
        /// دریافت وضعیت سلامت سیستم
        /// </summary>
        public FailSecureHealthStatus GetHealthStatus()
        {
            var recentCount = GetRecentFailureCount(TimeSpan.FromMinutes(5));
            var lastFailure = _recentFailures.LastOrDefault();

            _cache.TryGetValue(SecureModeReasonKey, out string? secureModeReason);
            _cache.TryGetValue(SecureModeActivatedAtKey, out DateTime? secureModeActivatedAt);

            return new FailSecureHealthStatus
            {
                IsInSecureMode = IsSystemInSecureMode(),
                RecentFailureCount = recentCount,
                LastFailureTime = lastFailure?.OccurredAt,
                LastFailureType = lastFailure?.FailureType,
                SecureModeActivatedAt = secureModeActivatedAt,
                SecureModeReason = secureModeReason
            };
        }

        /// <summary>
        /// ثبت رویداد Fail-Secure در Audit Log
        /// اگر دیتابیس در دسترس نباشد، در فایل ذخیره می‌کند
        /// </summary>
        private async Task LogFailSecureEventAsync(string operationName, Exception ex, CancellationToken ct)
        {
            var failureRecord = new FailureRecord
            {
                FailureType = ex.GetType().Name,
                OperationName = operationName,
                Details = ex.Message,
                OccurredAt = DateTime.Now
            };

            try
            {
                // اول سعی کن در دیتابیس ذخیره کنی
                await _auditLogService.LogEventAsync(
                    eventType: "FailSecureActivated",
                    entityType: "Operation",
                    entityId: operationName,
                    isSuccess: false,
                    errorMessage: $"FAIL-SECURE: {ex.GetType().Name} - Operation returned fail-safe default",
                    description: $"Operation: {operationName} | Exception: {ex.GetType().Name} | Fail-safe default applied",
                    ct: ct);
            }
            catch (Exception dbEx)
            {
                // ============================================
                // دیتابیس در دسترس نیست - ذخیره در فایل
                // ============================================
                _logger.LogWarning(dbEx, 
                    "Database unavailable for audit log. Saving failure to file: {OperationName}", 
                    operationName);

                await LogFailureToFileInternalAsync(failureRecord);
            }
        }

        /// <summary>
        /// ذخیره شکست در فایل (وقتی دیتابیس در دسترس نیست)
        /// فایل‌ها به صورت روزانه ذخیره می‌شوند
        /// مسیر: logs/fail-secure/failures_yyyy-MM-dd.log
        /// این متد public است تا سرویس‌های دیگر هم بتوانند از آن استفاده کنند
        /// </summary>
        public async Task LogFailureToFileAsync(string failureType, string operationName, string details)
        {
            var record = new FailureRecord
            {
                FailureType = failureType,
                OperationName = operationName,
                Details = details,
                OccurredAt = DateTime.Now
            };
            await LogFailureToFileInternalAsync(record);
        }

        /// <summary>
        /// پیاده‌سازی داخلی ذخیره شکست در فایل
        /// </summary>
        private async Task LogFailureToFileInternalAsync(FailureRecord record)
        {
            try
            {
                // ایجاد پوشه اگر وجود ندارد
                if (!Directory.Exists(FailureLogDirectory))
                {
                    Directory.CreateDirectory(FailureLogDirectory);
                }

                // نام فایل روزانه
                var fileName = $"failures_{DateTime.Now:yyyy-MM-dd}.log";
                var filePath = Path.Combine(FailureLogDirectory, fileName);

                // فرمت خوانا با جداکننده
                var logEntry = new StringBuilder();
                logEntry.AppendLine("================================================================================");
                logEntry.AppendLine($"🕐 زمان: {record.OccurredAt:yyyy-MM-dd HH:mm:ss}");
                logEntry.AppendLine($"📛 نوع شکست: {record.FailureType}");
                logEntry.AppendLine($"⚙️ عملیات: {record.OperationName}");
                logEntry.AppendLine($"📝 جزئیات: {record.Details}");
                logEntry.AppendLine("================================================================================");
                logEntry.AppendLine();

                // ذخیره در فایل با قفل برای جلوگیری از تداخل
                lock (_fileLock)
                {
                    File.AppendAllText(filePath, logEntry.ToString(), Encoding.UTF8);
                }

                _logger.LogInformation("Failure logged to file: {FilePath}", filePath);

                await Task.CompletedTask;
            }
            catch (Exception fileEx)
            {
                // اگر ذخیره در فایل هم نشد، فقط در لاگ سیستم ثبت کن
                _logger.LogError(fileEx, 
                    "Failed to write failure to file. FailureType: {FailureType}, Operation: {Operation}",
                    record.FailureType,
                    record.OperationName);
            }
        }

        /// <summary>
        /// بررسی و فعال‌سازی خودکار حالت امن در صورت نیاز
        /// </summary>
        private async Task CheckAndActivateSecureModeIfNeededAsync(CancellationToken ct)
        {
            if (IsSystemInSecureMode())
                return;

            var recentCount = GetRecentFailureCount(TimeSpan.FromMinutes(_settings.FailureWindowMinutes));

            if (recentCount >= _settings.MaxConsecutiveFailuresBeforeSecureMode)
            {
                await ActivateSecureModeAsync(
                    $"Automatic activation due to {recentCount} failures in the last {_settings.FailureWindowMinutes} minutes",
                    ct);
            }
        }

        /// <summary>
        /// پاکسازی رکوردهای شکست قدیمی
        /// </summary>
        private void CleanupOldFailures()
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(_settings.FailureRetentionMinutes);

            lock (_failureLock)
            {
                while (_recentFailures.TryPeek(out var oldest) && oldest.OccurredAt < cutoff)
                {
                    _recentFailures.TryDequeue(out _);
                }
            }
        }

        /// <summary>
        /// پاکسازی تمام رکوردهای شکست
        /// </summary>
        private void ClearRecentFailures()
        {
            lock (_failureLock)
            {
                while (_recentFailures.TryDequeue(out _)) { }
            }
        }

        /// <summary>
        /// رکورد شکست برای ذخیره‌سازی موقت
        /// </summary>
        private class FailureRecord
        {
            public string FailureType { get; set; } = string.Empty;
            public string OperationName { get; set; } = string.Empty;
            public string Details { get; set; } = string.Empty;
            public DateTime OccurredAt { get; set; }
        }
    }

    /// <summary>
    /// تنظیمات Fail-Secure
    /// ============================================
    /// پیاده‌سازی الزام FPT_FLS.1.1 (الزام 46)
    /// ============================================
    /// </summary>
    public class FailSecureSettings
    {
        /// <summary>
        /// در صورت خطا در بررسی دسترسی، آیا دسترسی داده شود؟
        /// پیش‌فرض: false (Deny) - امن‌ترین حالت
        /// </summary>
        public bool DefaultAccessOnFailure { get; set; } = false;

        /// <summary>
        /// در صورت خطا در احراز هویت، آیا کاربر مجاز باشد؟
        /// پیش‌فرض: false (Deny) - امن‌ترین حالت
        /// </summary>
        public bool DefaultAuthenticationOnFailure { get; set; } = false;

        /// <summary>
        /// حداکثر تعداد شکست‌های متوالی قبل از فعال‌سازی حالت امن
        /// پیش‌فرض: 10 شکست
        /// </summary>
        public int MaxConsecutiveFailuresBeforeSecureMode { get; set; } = 10;

        /// <summary>
        /// بازه زمانی بررسی شکست‌ها (دقیقه)
        /// پیش‌فرض: 5 دقیقه
        /// </summary>
        public int FailureWindowMinutes { get; set; } = 5;

        /// <summary>
        /// مدت زمان نگهداری رکوردهای شکست (دقیقه)
        /// پیش‌فرض: 60 دقیقه
        /// </summary>
        public int FailureRetentionMinutes { get; set; } = 60;

        /// <summary>
        /// آیا در حالت امن، تمام عملیات رد شوند؟
        /// پیش‌فرض: false - فقط عملیات حساس رد می‌شوند
        /// </summary>
        public bool DenyAllInSecureMode { get; set; } = false;

        /// <summary>
        /// آیا در صورت عدم دسترسی به دیتابیس، سیستم به حالت امن برود؟
        /// پیش‌فرض: true
        /// </summary>
        public bool ActivateSecureModeOnDatabaseFailure { get; set; } = true;

        /// <summary>
        /// آیا در صورت شکست احراز هویت، سیستم به حالت امن برود؟
        /// پیش‌فرض: false - فقط شکست‌های سیستمی
        /// </summary>
        public bool ActivateSecureModeOnAuthFailure { get; set; } = false;
    }
}

