using BnpCashClaudeApp.Domain.Entities.AuditLogSubsystem;
using BnpCashClaudeApp.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس لاگ دسترسی به فایل‌های پیوست
    /// ============================================
    /// پیاده‌سازی الزامات امنیتی ISO 15408:
    /// - FAU_GEN.1: تولید داده ممیزی
    /// - FAU_GEN.2: مرتبط نمودن هویت کاربر
    /// - FTA_TAH.1: سوابق دسترسی به محصول
    /// - FDP_ETC.2: خروج داده با مشخصه امنیتی
    /// ============================================
    /// </summary>
    public interface IAttachmentAccessLogService
    {
        // ============================================
        // ثبت لاگ
        // ============================================

        /// <summary>
        /// ثبت لاگ دسترسی به فایل
        /// </summary>
        Task<long> LogAccessAsync(
            long attachmentId,
            Guid attachmentPublicId,
            string fileName,
            AttachmentAccessType accessType,
            bool isSuccess,
            AttachmentAccessLogRequest request,
            CancellationToken ct = default);

        /// <summary>
        /// ثبت لاگ مشاهده فایل
        /// </summary>
        Task<long> LogViewAsync(
            long attachmentId,
            Guid attachmentPublicId,
            string fileName,
            AttachmentAccessLogRequest request,
            CancellationToken ct = default);

        /// <summary>
        /// ثبت لاگ دانلود فایل
        /// </summary>
        Task<long> LogDownloadAsync(
            long attachmentId,
            Guid attachmentPublicId,
            string fileName,
            long bytesTransferred,
            AttachmentAccessLogRequest request,
            CancellationToken ct = default);

        /// <summary>
        /// ثبت لاگ آپلود فایل
        /// </summary>
        Task<long> LogUploadAsync(
            long attachmentId,
            Guid attachmentPublicId,
            string fileName,
            long fileSize,
            AttachmentAccessLogRequest request,
            CancellationToken ct = default);

        /// <summary>
        /// ثبت لاگ حذف فایل
        /// </summary>
        Task<long> LogDeleteAsync(
            long attachmentId,
            Guid attachmentPublicId,
            string fileName,
            AttachmentAccessLogRequest request,
            CancellationToken ct = default);

        /// <summary>
        /// ثبت لاگ رد دسترسی
        /// </summary>
        Task<long> LogAccessDeniedAsync(
            long attachmentId,
            Guid attachmentPublicId,
            string fileName,
            AttachmentAccessType accessType,
            string deniedReason,
            AttachmentAccessLogRequest request,
            CancellationToken ct = default);

        // ============================================
        // جستجو و دریافت لاگ‌ها
        // ============================================

        /// <summary>
        /// دریافت لاگ‌های دسترسی به یک فایل
        /// </summary>
        Task<IEnumerable<tblAttachmentAccessLog>> GetByAttachmentAsync(
            Guid attachmentPublicId,
            int? limit = null,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت لاگ‌های دسترسی یک کاربر
        /// </summary>
        Task<IEnumerable<tblAttachmentAccessLog>> GetByUserAsync(
            long userId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? limit = null,
            CancellationToken ct = default);

        /// <summary>
        /// جستجوی لاگ‌ها
        /// </summary>
        Task<AttachmentAccessLogSearchResult> SearchAsync(
            AttachmentAccessLogSearchRequest request,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت لاگ‌های دسترسی ناموفق
        /// </summary>
        Task<IEnumerable<tblAttachmentAccessLog>> GetFailedAccessesAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? limit = null,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت لاگ‌های دسترسی از یک IP خاص
        /// </summary>
        Task<IEnumerable<tblAttachmentAccessLog>> GetByIpAddressAsync(
            string ipAddress,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? limit = null,
            CancellationToken ct = default);

        // ============================================
        // تحلیل و آمار
        // ============================================

        /// <summary>
        /// دریافت آمار دسترسی به فایل‌ها
        /// </summary>
        Task<AttachmentAccessStatistics> GetStatisticsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            long? customerId = null,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت پرمراجعه‌ترین فایل‌ها
        /// </summary>
        Task<IEnumerable<TopAccessedFile>> GetTopAccessedFilesAsync(
            int top = 10,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت فعال‌ترین کاربران در دسترسی به فایل‌ها
        /// </summary>
        Task<IEnumerable<TopFileAccessUser>> GetTopUsersAsync(
            int top = 10,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default);

        /// <summary>
        /// تشخیص الگوهای مشکوک دسترسی
        /// </summary>
        Task<IEnumerable<SuspiciousAccessPattern>> DetectSuspiciousActivityAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default);

        // ============================================
        // نگهداری و پاکسازی
        // ============================================

        /// <summary>
        /// پاکسازی لاگ‌های قدیمی
        /// </summary>
        Task<int> CleanupOldLogsAsync(
            int retentionDays,
            CancellationToken ct = default);

        /// <summary>
        /// آرشیو لاگ‌های قدیمی
        /// </summary>
        Task<int> ArchiveLogsAsync(
            int olderThanDays,
            string archivePath,
            CancellationToken ct = default);
    }

    // ============================================
    // مدل‌های کمکی
    // ============================================

    /// <summary>
    /// درخواست ثبت لاگ دسترسی
    /// </summary>
    public class AttachmentAccessLogRequest
    {
        public long? UserId { get; set; }
        public string? UserName { get; set; }
        public long? UserGroupId { get; set; }
        public string? UserGroupName { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Browser { get; set; }
        public string? BrowserVersion { get; set; }
        public string? OperatingSystem { get; set; }
        public string? DeviceType { get; set; }
        public string? RequestId { get; set; }
        public string? SessionId { get; set; }
        public long? CustomerId { get; set; }
        public long? ShobeId { get; set; }
        public string? FileType { get; set; }
        public long? FileSize { get; set; }
        public int? FileSensitivityLevel { get; set; }
        public string? FileSecurityClassification { get; set; }
        public bool? WasEncrypted { get; set; }
        public bool? IntegrityVerified { get; set; }
        public bool? IntegrityCheckResult { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Description { get; set; }
        public string? AdditionalInfo { get; set; }
    }

    /// <summary>
    /// درخواست جستجوی لاگ‌ها
    /// </summary>
    public class AttachmentAccessLogSearchRequest
    {
        public Guid? AttachmentPublicId { get; set; }
        public long? AttachmentId { get; set; }
        public string? FileName { get; set; }
        public AttachmentAccessType? AccessType { get; set; }
        public long? UserId { get; set; }
        public string? UserName { get; set; }
        public string? IpAddress { get; set; }
        public bool? IsSuccess { get; set; }
        public long? CustomerId { get; set; }
        public long? ShobeId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;
    }

    /// <summary>
    /// نتیجه جستجوی لاگ‌ها
    /// </summary>
    public class AttachmentAccessLogSearchResult
    {
        public IEnumerable<tblAttachmentAccessLog> Items { get; set; } = new List<tblAttachmentAccessLog>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    /// <summary>
    /// آمار دسترسی به فایل‌ها
    /// </summary>
    public class AttachmentAccessStatistics
    {
        public long TotalAccesses { get; set; }
        public long SuccessfulAccesses { get; set; }
        public long FailedAccesses { get; set; }
        public long TotalDownloads { get; set; }
        public long TotalViews { get; set; }
        public long TotalUploads { get; set; }
        public long TotalDeletes { get; set; }
        public long UniqueUsers { get; set; }
        public long UniqueFiles { get; set; }
        public long TotalBytesTransferred { get; set; }
        public Dictionary<string, long> AccessesByType { get; set; } = new Dictionary<string, long>();
        public Dictionary<string, long> AccessesByDay { get; set; } = new Dictionary<string, long>();
        public Dictionary<string, long> AccessesByHour { get; set; } = new Dictionary<string, long>();
    }

    /// <summary>
    /// فایل پرمراجعه
    /// </summary>
    public class TopAccessedFile
    {
        public long AttachmentId { get; set; }
        public Guid AttachmentPublicId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long AccessCount { get; set; }
        public long DownloadCount { get; set; }
        public long ViewCount { get; set; }
        public DateTime LastAccessedAt { get; set; }
    }

    /// <summary>
    /// کاربر فعال در دسترسی به فایل‌ها
    /// </summary>
    public class TopFileAccessUser
    {
        public long UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public long TotalAccesses { get; set; }
        public long DownloadCount { get; set; }
        public long UploadCount { get; set; }
        public DateTime LastAccessAt { get; set; }
    }

    /// <summary>
    /// الگوی مشکوک دسترسی
    /// </summary>
    public class SuspiciousAccessPattern
    {
        public string PatternType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? IpAddress { get; set; }
        public int OccurrenceCount { get; set; }
        public DateTime FirstOccurrence { get; set; }
        public DateTime LastOccurrence { get; set; }
        public string? AffectedFiles { get; set; }
        public string Severity { get; set; } = "Medium";
    }
}
