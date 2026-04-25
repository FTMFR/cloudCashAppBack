using BnpCashClaudeApp.Domain.Entities.AttachSubsystem;
using BnpCashClaudeApp.Domain.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس مدیریت فایل‌های پیوست
    /// ============================================
    /// پیاده‌سازی الزامات امنیتی ISO 15408:
    /// - FDP_ITC.2: ورود داده با مشخصه امنیتی
    /// - FDP_ETC.2: خروج داده با مشخصه امنیتی
    /// - FDP_SDI.2: صحت داده ذخیره شده
    /// - FDP_RIP.2: حفاظت اطلاعات باقیمانده
    /// ============================================
    /// </summary>
    public interface IAttachmentService
    {
        // ============================================
        // عملیات آپلود
        // ============================================

        /// <summary>
        /// آپلود فایل جدید
        /// </summary>
        /// <param name="fileStream">جریان فایل</param>
        /// <param name="originalFileName">نام اصلی فایل</param>
        /// <param name="contentType">نوع MIME</param>
        /// <param name="request">اطلاعات تکمیلی</param>
        /// <param name="ct">توکن لغو</param>
        /// <returns>فایل آپلود شده</returns>
        Task<AttachmentUploadResult> UploadAsync(
            Stream fileStream,
            string originalFileName,
            string contentType,
            AttachmentUploadRequest request,
            CancellationToken ct = default);

        /// <summary>
        /// آپلود چندین فایل
        /// </summary>
        Task<IEnumerable<AttachmentUploadResult>> UploadMultipleAsync(
            IEnumerable<AttachmentUploadItem> items,
            AttachmentUploadRequest request,
            CancellationToken ct = default);

        // ============================================
        // عملیات دانلود
        // ============================================

        /// <summary>
        /// دانلود فایل با شناسه عمومی
        /// </summary>
        Task<AttachmentDownloadResult> DownloadAsync(
            Guid publicId,
            long? userId = null,
            string? ipAddress = null,
            CancellationToken ct = default);

        /// <summary>
        /// دانلود فایل با شناسه داخلی
        /// </summary>
        Task<AttachmentDownloadResult> DownloadByIdAsync(
            long id,
            long? userId = null,
            string? ipAddress = null,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت URL دانلود موقت (اگر از Cloud Storage استفاده می‌شود)
        /// </summary>
        Task<string?> GetTemporaryDownloadUrlAsync(
            Guid publicId,
            TimeSpan expiration,
            CancellationToken ct = default);

        // ============================================
        // عملیات جستجو و دریافت
        // ============================================

        /// <summary>
        /// دریافت فایل با شناسه عمومی
        /// </summary>
        Task<tblAttachment?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default);

        /// <summary>
        /// دریافت فایل با شناسه داخلی
        /// </summary>
        Task<tblAttachment?> GetByIdAsync(long id, CancellationToken ct = default);

        /// <summary>
        /// دریافت فایل‌های مرتبط با یک موجودیت
        /// </summary>
        Task<IEnumerable<tblAttachment>> GetByEntityAsync(
            string entityType,
            long entityId,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت فایل‌های مرتبط با یک موجودیت (با شناسه عمومی)
        /// </summary>
        Task<IEnumerable<tblAttachment>> GetByEntityPublicIdAsync(
            string entityType,
            Guid entityPublicId,
            CancellationToken ct = default);

        /// <summary>
        /// جستجوی فایل‌ها
        /// </summary>
        Task<AttachmentSearchResult> SearchAsync(
            AttachmentSearchRequest request,
            CancellationToken ct = default);

        // ============================================
        // عملیات حذف
        // ============================================

        /// <summary>
        /// حذف نرم فایل (Soft Delete)
        /// </summary>
        Task<bool> SoftDeleteAsync(
            Guid publicId,
            long? userId = null,
            string? ipAddress = null,
            CancellationToken ct = default);

        /// <summary>
        /// حذف سخت فایل (Hard Delete) - با پاکسازی امن
        /// FDP_RIP.2: حفاظت اطلاعات باقیمانده
        /// </summary>
        Task<bool> HardDeleteAsync(
            Guid publicId,
            long? userId = null,
            string? ipAddress = null,
            CancellationToken ct = default);

        /// <summary>
        /// بازیابی فایل حذف شده
        /// </summary>
        Task<bool> RestoreAsync(Guid publicId, CancellationToken ct = default);

        // ============================================
        // عملیات به‌روزرسانی
        // ============================================

        /// <summary>
        /// به‌روزرسانی اطلاعات فایل
        /// </summary>
        Task<bool> UpdateMetadataAsync(
            Guid publicId,
            AttachmentUpdateRequest request,
            long? userId = null,
            CancellationToken ct = default);

        /// <summary>
        /// تغییر سطح حساسیت فایل
        /// </summary>
        Task<bool> ChangeSensitivityLevelAsync(
            Guid publicId,
            FileSensitivityLevel newLevel,
            long? userId = null,
            string? ipAddress = null,
            CancellationToken ct = default);

        // ============================================
        // عملیات امنیتی (FDP_SDI.2)
        // ============================================

        /// <summary>
        /// بررسی صحت فایل
        /// </summary>
        Task<IntegrityCheckResult> VerifyIntegrityAsync(
            Guid publicId,
            CancellationToken ct = default);

        /// <summary>
        /// بررسی صحت همه فایل‌ها
        /// </summary>
        Task<IEnumerable<IntegrityCheckResult>> VerifyAllIntegrityAsync(
            int batchSize = 100,
            CancellationToken ct = default);

        /// <summary>
        /// محاسبه مجدد هش فایل
        /// </summary>
        Task<string?> RecalculateHashAsync(Guid publicId, CancellationToken ct = default);

        // ============================================
        // عملیات رمزنگاری
        // ============================================

        /// <summary>
        /// رمزنگاری فایل
        /// </summary>
        Task<bool> EncryptAsync(
            Guid publicId,
            Guid? keyId = null,
            CancellationToken ct = default);

        /// <summary>
        /// رمزگشایی فایل
        /// </summary>
        Task<bool> DecryptAsync(
            Guid publicId,
            CancellationToken ct = default);

        // ============================================
        // عملیات اسکن آنتی‌ویروس
        // ============================================

        /// <summary>
        /// اسکن آنتی‌ویروس فایل
        /// </summary>
        Task<VirusScanResult> ScanForVirusAsync(
            Guid publicId,
            CancellationToken ct = default);

        /// <summary>
        /// قرنطینه کردن فایل آلوده
        /// </summary>
        Task<bool> QuarantineAsync(
            Guid publicId,
            string reason,
            CancellationToken ct = default);

        // ============================================
        // عملیات آرشیو
        // ============================================

        /// <summary>
        /// آرشیو کردن فایل
        /// </summary>
        Task<bool> ArchiveAsync(Guid publicId, CancellationToken ct = default);

        /// <summary>
        /// خارج کردن از آرشیو
        /// </summary>
        Task<bool> UnarchiveAsync(Guid publicId, CancellationToken ct = default);

        // ============================================
        // آمار و گزارش
        // ============================================

        /// <summary>
        /// دریافت آمار ذخیره‌سازی
        /// </summary>
        Task<StorageStatistics> GetStorageStatisticsAsync(
            long? customerId = null,
            long? shobeId = null,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت فایل‌های منقضی شده
        /// </summary>
        Task<IEnumerable<tblAttachment>> GetExpiredFilesAsync(CancellationToken ct = default);

        /// <summary>
        /// پاکسازی فایل‌های منقضی شده
        /// </summary>
        Task<int> CleanupExpiredFilesAsync(CancellationToken ct = default);
    }

    // ============================================
    // مدل‌های کمکی
    // ============================================

    /// <summary>
    /// درخواست آپلود فایل
    /// </summary>
    public class AttachmentUploadRequest
    {
        // اطلاعات فایل
        public Stream FileStream { get; set; } = null!;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }

        // طبقه‌بندی فایل
        public AttachmentType AttachmentType { get; set; } = AttachmentType.Document;
        public string? Category { get; set; }
        public string? Description { get; set; }

        // ارتباط با موجودیت (Polymorphic)
        public string? EntityType { get; set; }
        public long? EntityId { get; set; }
        public Guid? EntityPublicId { get; set; }

        // امنیت
        public FileSensitivityLevel SensitivityLevel { get; set; } = FileSensitivityLevel.Internal;
        public string? SecurityClassification { get; set; }
        public string? SecurityLabels { get; set; }
        public bool ShouldEncrypt { get; set; } = false;

        // انقضا
        public DateTime? ExpiresAt { get; set; }

        // Multi-tenancy
        public long? tblCustomerId { get; set; }
        public long? tblShobeId { get; set; }

        // آپلودکننده
        public long? UploadedByUserId { get; set; }
        public string? IpAddress { get; set; }
    }

    /// <summary>
    /// آیتم آپلود (برای آپلود چندتایی)
    /// </summary>
    public class AttachmentUploadItem
    {
        public Stream FileStream { get; set; } = null!;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
    }

    /// <summary>
    /// نتیجه آپلود فایل
    /// </summary>
    public class AttachmentUploadResult
    {
        public bool IsSuccess { get; set; }
        public Guid? PublicId { get; set; }
        public long? Id { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public string? ContentHash { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// نتیجه دانلود فایل
    /// </summary>
    public class AttachmentDownloadResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public Stream? FileStream { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public bool WasEncrypted { get; set; }
        public bool IntegrityVerified { get; set; }

        /// <summary>
        /// ایجاد نتیجه موفق
        /// </summary>
        public static AttachmentDownloadResult Success(Stream fileStream, string fileName, string contentType, long fileSize)
        {
            return new AttachmentDownloadResult
            {
                IsSuccess = true,
                FileStream = fileStream,
                FileName = fileName,
                ContentType = contentType,
                FileSize = fileSize
            };
        }

        /// <summary>
        /// ایجاد نتیجه ناموفق
        /// </summary>
        public static AttachmentDownloadResult Failure(string errorMessage)
        {
            return new AttachmentDownloadResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// درخواست جستجوی فایل‌ها
    /// </summary>
    public class AttachmentSearchRequest
    {
        public string? FileName { get; set; }
        public string? Category { get; set; }
        public AttachmentType? AttachmentType { get; set; }
        public AttachmentStatus? Status { get; set; }
        public FileSensitivityLevel? SensitivityLevel { get; set; }
        public string? EntityType { get; set; }
        public long? EntityId { get; set; }
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
    /// نتیجه جستجوی فایل‌ها
    /// </summary>
    public class AttachmentSearchResult
    {
        public IEnumerable<tblAttachment> Items { get; set; } = new List<tblAttachment>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    /// <summary>
    /// درخواست به‌روزرسانی فایل
    /// </summary>
    public class AttachmentUpdateRequest
    {
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? SecurityClassification { get; set; }
        public string? SecurityLabels { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// نتیجه بررسی صحت
    /// </summary>
    public class IntegrityCheckResult
    {
        public Guid PublicId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string? ExpectedHash { get; set; }
        public string? ActualHash { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CheckedAt { get; set; }
    }

    /// <summary>
    /// نتیجه اسکن آنتی‌ویروس
    /// </summary>
    public class VirusScanResult
    {
        public bool IsClean { get; set; }
        public string? ThreatName { get; set; }
        public string? ScannerName { get; set; }
        public DateTime ScannedAt { get; set; }
    }

    /// <summary>
    /// آمار ذخیره‌سازی
    /// </summary>
    public class StorageStatistics
    {
        public long TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public long ActiveFiles { get; set; }
        public long ArchivedFiles { get; set; }
        public long DeletedFiles { get; set; }
        public long EncryptedFiles { get; set; }
        public Dictionary<string, long> FilesByType { get; set; } = new Dictionary<string, long>();
        public Dictionary<string, long> SizeByType { get; set; } = new Dictionary<string, long>();
    }
}
