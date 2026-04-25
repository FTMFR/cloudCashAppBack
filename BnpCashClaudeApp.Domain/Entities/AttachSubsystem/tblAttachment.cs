using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BnpCashClaudeApp.Domain.Entities.AttachSubsystem
{
    /// <summary>
    /// موجودیت فایل پیوست
    /// ============================================
    /// پیاده‌سازی الزامات امنیتی ISO 15408:
    /// - FDP_ITC.2: ورود داده با مشخصه امنیتی
    /// - FDP_ETC.2: خروج داده با مشخصه امنیتی
    /// - FDP_SDI.2: صحت داده ذخیره شده
    /// - FDP_RIP.2: حفاظت اطلاعات باقیمانده
    /// ============================================
    /// </summary>
    public class tblAttachment : BaseEntity
    {
        // ============================================
        // اطلاعات فایل
        // ============================================

        /// <summary>
        /// نام اصلی فایل (قبل از آپلود)
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// نام ذخیره شده فایل (GUID-based برای امنیت)
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string StoredFileName { get; set; } = string.Empty;

        /// <summary>
        /// پسوند فایل (بدون نقطه)
        /// </summary>
        [MaxLength(20)]
        public string FileExtension { get; set; } = string.Empty;

        /// <summary>
        /// نوع MIME فایل
        /// </summary>
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// حجم فایل (بایت)
        /// </summary>
        public long FileSize { get; set; }

        // ============================================
        // مسیر ذخیره‌سازی
        // ============================================

        /// <summary>
        /// مسیر نسبی ذخیره‌سازی (بدون نام فایل)
        /// مثال: "2026/01/customers/123"
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string StoragePath { get; set; } = string.Empty;

        /// <summary>
        /// نوع Storage
        /// 0: FileSystem, 1: Database (BLOB), 2: Azure Blob, 3: S3
        /// </summary>
        public int StorageType { get; set; } = (int)Enums.StorageType.FileSystem;

        /// <summary>
        /// داده فایل (اختیاری - برای ذخیره در دیتابیس)
        /// فقط برای فایل‌های کوچک یا حساس
        /// </summary>
        public byte[]? FileData { get; set; }

        // ============================================
        // طبقه‌بندی و نوع فایل
        // ============================================

        /// <summary>
        /// نوع پیوست
        /// 0: Document, 1: Image, 2: Signature, 3: Report, 4: Export, 5: Backup, 6: Other
        /// </summary>
        public int AttachmentType { get; set; } = (int)Enums.AttachmentType.Document;

        /// <summary>
        /// دسته‌بندی فایل
        /// مثال: "CustomerDocuments", "UserPhotos", "DigitalSignatures"
        /// </summary>
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// توضیحات فایل
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        // ============================================
        // ارتباط با موجودیت‌ها (Polymorphic)
        // ============================================

        /// <summary>
        /// نوع موجودیت مرتبط
        /// مثال: "tblUser", "tblCustomer", "tblShobe"
        /// </summary>
        [MaxLength(100)]
        public string? EntityType { get; set; }

        /// <summary>
        /// شناسه موجودیت مرتبط
        /// </summary>
        public long? EntityId { get; set; }

        /// <summary>
        /// شناسه عمومی موجودیت مرتبط
        /// </summary>
        public Guid? EntityPublicId { get; set; }

        // ============================================
        // مشخصات امنیتی (FDP_ITC.2 / FDP_ETC.2)
        // ============================================

        /// <summary>
        /// سطح حساسیت
        /// 0: Public, 1: Internal, 2: Confidential, 3: Secret
        /// </summary>
        public int SensitivityLevel { get; set; } = (int)Enums.FileSensitivityLevel.Internal;

        /// <summary>
        /// طبقه‌بندی امنیتی
        /// مثال: "PersonalData", "FinancialData", "AuditData"
        /// </summary>
        [MaxLength(100)]
        public string? SecurityClassification { get; set; }

        /// <summary>
        /// برچسب‌های امنیتی (جداشده با ;)
        /// مثال: "PII;GDPR;Sensitive"
        /// </summary>
        [MaxLength(500)]
        public string? SecurityLabels { get; set; }

        /// <summary>
        /// آیا فایل رمزنگاری شده است
        /// </summary>
        public bool IsEncrypted { get; set; } = false;

        /// <summary>
        /// الگوریتم رمزنگاری استفاده شده
        /// مثال: "AES-256-CBC"
        /// </summary>
        [MaxLength(50)]
        public string? EncryptionAlgorithm { get; set; }

        /// <summary>
        /// شناسه کلید رمزنگاری (FK به CryptographicKeyEntity)
        /// </summary>
        public Guid? EncryptionKeyId { get; set; }

        /// <summary>
        /// IV رمزنگاری (Base64)
        /// </summary>
        [MaxLength(100)]
        public string? EncryptionIV { get; set; }

        // ============================================
        // صحت داده (FDP_SDI.2)
        // ============================================

        /// <summary>
        /// هش SHA-256 محتوای فایل (قبل از رمزنگاری)
        /// برای تشخیص تغییرات غیرمجاز
        /// </summary>
        [MaxLength(64)]
        public string ContentHash { get; set; } = string.Empty;

        /// <summary>
        /// الگوریتم هش استفاده شده
        /// </summary>
        [MaxLength(20)]
        public string HashAlgorithm { get; set; } = "SHA256";

        /// <summary>
        /// امضای دیجیتال فایل (HMAC یا RSA)
        /// </summary>
        [MaxLength(500)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// تاریخ آخرین بررسی صحت (شمسی)
        /// </summary>
        public string? LastIntegrityCheckAt { get; set; }

        /// <summary>
        /// نتیجه آخرین بررسی صحت
        /// true: سالم، false: خراب، null: بررسی نشده
        /// </summary>
        public bool? LastIntegrityCheckResult { get; set; }

        // ============================================
        // وضعیت فایل
        // ============================================

        /// <summary>
        /// وضعیت فایل
        /// 0: Pending, 1: Active, 2: Archived, 3: Deleted, 4: Quarantined
        /// </summary>
        public int Status { get; set; } = (int)AttachmentStatus.Active;

        /// <summary>
        /// آیا فایل اسکن آنتی‌ویروس شده
        /// </summary>
        public bool IsVirusScanned { get; set; } = false;

        /// <summary>
        /// نتیجه اسکن آنتی‌ویروس
        /// true: پاک، false: آلوده، null: اسکن نشده
        /// </summary>
        public bool? VirusScanResult { get; set; }

        /// <summary>
        /// تاریخ اسکن آنتی‌ویروس (شمسی)
        /// </summary>
        public string? VirusScannedAt { get; set; }

        // ============================================
        // ردیابی دسترسی (Audit)
        // ============================================

        /// <summary>
        /// تعداد دفعات دانلود
        /// </summary>
        public int DownloadCount { get; set; } = 0;

        /// <summary>
        /// تاریخ آخرین دسترسی (شمسی)
        /// </summary>
        public string? LastAccessedAt { get; set; }

        /// <summary>
        /// IP آخرین دسترسی
        /// </summary>
        [MaxLength(45)]
        public string? LastAccessedFromIp { get; set; }

        /// <summary>
        /// شناسه کاربر آخرین دسترسی
        /// </summary>
        public long? LastAccessedByUserId { get; set; }

        // ============================================
        // تنظیمات انقضا و حذف
        // ============================================

        /// <summary>
        /// تاریخ انقضای فایل (شمسی) - اختیاری
        /// </summary>
        public string? ExpiresAt { get; set; }

        /// <summary>
        /// تاریخ حذف خودکار (شمسی) - اختیاری
        /// </summary>
        public string? AutoDeleteAt { get; set; }

        /// <summary>
        /// آیا فایل قابل حذف است
        /// false = فایل‌های حیاتی مثل امضای دیجیتال
        /// </summary>
        public bool IsDeletable { get; set; } = true;

        // ============================================
        // Multi-tenancy
        // ============================================

        /// <summary>
        /// شناسه مشتری مالک فایل
        /// </summary>
        public long? tblCustomerId { get; set; }

        /// <summary>
        /// شناسه شعبه مالک فایل
        /// </summary>
        public long? tblShobeId { get; set; }

        // ============================================
        // متدهای کمکی
        // ============================================

        /// <summary>
        /// تنظیم تاریخ آخرین دسترسی
        /// </summary>
        public void SetLastAccessedAt(DateTime dateTime)
        {
            LastAccessedAt = BaseEntity.ToPersianDateTime(dateTime);
        }

        /// <summary>
        /// تنظیم تاریخ انقضا
        /// </summary>
        public void SetExpiresAt(DateTime dateTime)
        {
            ExpiresAt = BaseEntity.ToPersianDateTime(dateTime);
        }

        /// <summary>
        /// تنظیم تاریخ اسکن آنتی‌ویروس
        /// </summary>
        public void SetVirusScannedAt(DateTime dateTime)
        {
            VirusScannedAt = BaseEntity.ToPersianDateTime(dateTime);
        }

        /// <summary>
        /// تنظیم تاریخ آخرین بررسی صحت
        /// </summary>
        public void SetLastIntegrityCheckAt(DateTime dateTime)
        {
            LastIntegrityCheckAt = BaseEntity.ToPersianDateTime(dateTime);
        }

        /// <summary>
        /// مسیر کامل فایل
        /// </summary>
        [NotMapped]
        public string FullPath => string.IsNullOrEmpty(StoragePath) 
            ? StoredFileName 
            : $"{StoragePath}/{StoredFileName}";

        /// <summary>
        /// بررسی اینکه فایل فعال است
        /// </summary>
        [NotMapped]
        public bool IsActive => Status == (int)AttachmentStatus.Active;

        /// <summary>
        /// بررسی اینکه فایل منقضی شده
        /// </summary>
        [NotMapped]
        public bool IsExpired => !string.IsNullOrEmpty(ExpiresAt) &&
            BaseEntity.ToGregorianDateTime(ExpiresAt) < DateTime.Now;

        /// <summary>
        /// دریافت نوع پیوست به صورت Enum
        /// </summary>
        [NotMapped]
        public AttachmentType AttachmentTypeEnum
        {
            get => (AttachmentType)AttachmentType;
            set => AttachmentType = (int)value;
        }

        /// <summary>
        /// دریافت وضعیت به صورت Enum
        /// </summary>
        [NotMapped]
        public AttachmentStatus StatusEnum
        {
            get => (AttachmentStatus)Status;
            set => Status = (int)value;
        }

        /// <summary>
        /// دریافت سطح حساسیت به صورت Enum
        /// </summary>
        [NotMapped]
        public FileSensitivityLevel SensitivityLevelEnum
        {
            get => (FileSensitivityLevel)SensitivityLevel;
            set => SensitivityLevel = (int)value;
        }

        /// <summary>
        /// دریافت نوع ذخیره‌سازی به صورت Enum
        /// </summary>
        [NotMapped]
        public StorageType StorageTypeEnum
        {
            get => (StorageType)StorageType;
            set => StorageType = (int)value;
        }
    }
}
