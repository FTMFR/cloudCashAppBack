using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس حفاظت از داده‌های ممیزی
    /// پیاده‌سازی الزامات FAU_STG.3.1 و FAU_STG.4.1 از استاندارد ISO 15408
    /// 
    /// FAU_STG.3.1: اقدامات لازم در زمان از دست رفتن داده ممیزی
    /// - ارسال هشدار در صورت شکست ذخیره‌سازی
    /// - مکانیزم Retry
    /// - ذخیره‌سازی جایگزین (Fallback)
    /// 
    /// FAU_STG.4.1: پیشگیری از اتلاف و از بین رفتن داده ممیزی
    /// - پشتیبان‌گیری خودکار
    /// - سیاست نگهداری (Retention Policy)
    /// - آرشیو داده‌های قدیمی
    /// </summary>
    public interface IAuditLogProtectionService
    {
        #region FAU_STG.3.1 - Alert & Fallback

        /// <summary>
        /// ثبت رویداد ممیزی با حفاظت کامل
        /// در صورت شکست، از Fallback Storage استفاده می‌کند
        /// </summary>
        Task<AuditLogSaveResult> SaveAuditLogWithProtectionAsync(
            AuditLogEntry entry,
            CancellationToken ct = default);

        /// <summary>
        /// ارسال هشدار شکست ذخیره‌سازی
        /// </summary>
        Task SendStorageFailureAlertAsync(
            string errorMessage,
            string? additionalInfo = null,
            CancellationToken ct = default);

        /// <summary>
        /// بازیابی لاگ‌های ذخیره شده در Fallback و انتقال به دیتابیس اصلی
        /// </summary>
        Task<int> RecoverFallbackLogsAsync(CancellationToken ct = default);

        /// <summary>
        /// دریافت تعداد لاگ‌های موجود در Fallback Storage
        /// </summary>
        Task<int> GetFallbackLogCountAsync(CancellationToken ct = default);

        /// <summary>
        /// دریافت وضعیت سلامت سیستم ذخیره‌سازی
        /// </summary>
        Task<StorageHealthStatus> GetStorageHealthStatusAsync(CancellationToken ct = default);

        #endregion

        #region FAU_STG.4.1 - Backup & Retention

        /// <summary>
        /// ایجاد پشتیبان از داده‌های ممیزی
        /// </summary>
        Task<BackupResult> CreateBackupAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default);

        /// <summary>
        /// آرشیو داده‌های قدیمی‌تر از تاریخ مشخص
        /// </summary>
        Task<ArchiveResult> ArchiveOldLogsAsync(
            DateTime olderThan,
            CancellationToken ct = default);

        /// <summary>
        /// اعمال سیاست نگهداری - حذف داده‌های منقضی شده
        /// </summary>
        Task<RetentionResult> ApplyRetentionPolicyAsync(CancellationToken ct = default);

        /// <summary>
        /// دریافت آمار داده‌های ممیزی
        /// </summary>
        Task<AuditLogStatistics> GetStatisticsAsync(CancellationToken ct = default);

        /// <summary>
        /// دریافت لیست پشتیبان‌های موجود
        /// </summary>
        Task<IEnumerable<BackupInfo>> GetBackupsAsync(CancellationToken ct = default);

        /// <summary>
        /// بازیابی از پشتیبان
        /// </summary>
        Task<RestoreResult> RestoreFromBackupAsync(
            string backupId,
            CancellationToken ct = default);

        #endregion
    }

    #region DTOs

    /// <summary>
    /// ورودی لاگ ممیزی
    /// </summary>
    public class AuditLogEntry
    {
        public string EventType { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? IpAddress { get; set; }
        public string? UserName { get; set; }
        public long? UserId { get; set; }
        public string? OperatingSystem { get; set; }
        public string? UserAgent { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, (object? OldValue, object? NewValue)>? Changes { get; set; }
    }

    /// <summary>
    /// نتیجه ذخیره‌سازی لاگ
    /// </summary>
    public class AuditLogSaveResult
    {
        public bool Success { get; set; }
        public long? LogId { get; set; }
        public string? ErrorMessage { get; set; }
        public bool UsedFallback { get; set; }
        public int RetryCount { get; set; }
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// وضعیت سلامت سیستم ذخیره‌سازی
    /// </summary>
    public class StorageHealthStatus
    {
        public bool IsDatabaseHealthy { get; set; }
        public bool IsFallbackHealthy { get; set; }
        public int PendingFallbackLogs { get; set; }
        public DateTime? LastSuccessfulSave { get; set; }
        public DateTime? LastFailure { get; set; }
        public int FailureCountLast24Hours { get; set; }
        public string? LastErrorMessage { get; set; }
        public long TotalLogsCount { get; set; }
        public long DatabaseSizeBytes { get; set; }
        public string Status => IsDatabaseHealthy ? "Healthy" : (IsFallbackHealthy ? "Degraded" : "Critical");
    }

    /// <summary>
    /// نتیجه پشتیبان‌گیری
    /// </summary>
    public class BackupResult
    {
        public bool Success { get; set; }
        public string? BackupId { get; set; }
        public string? BackupPath { get; set; }
        public long RecordsCount { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// نتیجه آرشیو
    /// </summary>
    public class ArchiveResult
    {
        public bool Success { get; set; }
        public long ArchivedCount { get; set; }
        public string? ArchivePath { get; set; }
        public DateTime OlderThan { get; set; }
        public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// نتیجه اعمال سیاست نگهداری
    /// </summary>
    public class RetentionResult
    {
        public bool Success { get; set; }
        public long DeletedCount { get; set; }
        public long ArchivedCount { get; set; }
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public int RetentionDays { get; set; }
        public int ArchiveDays { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// آمار داده‌های ممیزی
    /// </summary>
    public class AuditLogStatistics
    {
        public long TotalLogs { get; set; }
        public long TodayLogs { get; set; }
        public long Last7DaysLogs { get; set; }
        public long Last30DaysLogs { get; set; }
        public DateTime? OldestLog { get; set; }
        public DateTime? NewestLog { get; set; }
        public long SuccessfulLogs { get; set; }
        public long FailedLogs { get; set; }
        public long DatabaseSizeBytes { get; set; }
        public int BackupsCount { get; set; }
        public DateTime? LastBackupDate { get; set; }
        public int ArchivesCount { get; set; }
        public DateTime? LastArchiveDate { get; set; }
        public Dictionary<string, long> LogsByEventType { get; set; } = new Dictionary<string, long>();
    }

    /// <summary>
    /// اطلاعات پشتیبان
    /// </summary>
    public class BackupInfo
    {
        public string BackupId { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public long RecordsCount { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// نتیجه بازیابی
    /// </summary>
    public class RestoreResult
    {
        public bool Success { get; set; }
        public long RestoredCount { get; set; }
        public string? BackupId { get; set; }
        public DateTime RestoredAt { get; set; } = DateTime.UtcNow;
        public string? ErrorMessage { get; set; }
    }

    #endregion
}

