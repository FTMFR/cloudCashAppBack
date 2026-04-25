using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace BnpCashClaudeApp.Domain.Entities.AuditLogSubsystem
{
    /// <summary>
    /// جدول لاگ دسترسی به فایل‌های پیوست
    /// ============================================
    /// پیاده‌سازی الزامات امنیتی ISO 15408:
    /// - FAU_GEN.1: تولید داده ممیزی
    /// - FAU_GEN.2: مرتبط نمودن هویت کاربر
    /// - FTA_TAH.1: سوابق دسترسی به محصول
    /// - FDP_ETC.2: خروج داده با مشخصه امنیتی
    /// ============================================
    /// </summary>
    public class tblAttachmentAccessLog : BaseEntity
    {
        // ============================================
        // اطلاعات فایل مورد دسترسی
        // ============================================

        /// <summary>
        /// شناسه داخلی فایل پیوست
        /// </summary>
        public long AttachmentId { get; set; }

        /// <summary>
        /// شناسه عمومی فایل پیوست
        /// </summary>
        public Guid AttachmentPublicId { get; set; }

        /// <summary>
        /// نام فایل (در زمان دسترسی)
        /// </summary>
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// نوع فایل
        /// </summary>
        [MaxLength(100)]
        public string? FileType { get; set; }

        /// <summary>
        /// حجم فایل (بایت)
        /// </summary>
        public long? FileSize { get; set; }

        // ============================================
        // نوع دسترسی
        // ============================================

        /// <summary>
        /// نوع دسترسی
        /// 0: View, 1: Download, 2: Print, 3: Share, 4: Export, 5: Delete, 6: Edit, 7: Upload
        /// </summary>
        public int AccessType { get; set; }

        /// <summary>
        /// توضیحات دسترسی
        /// </summary>
        [MaxLength(500)]
        public string? AccessDescription { get; set; }

        // ============================================
        // اطلاعات کاربر
        // ============================================

        /// <summary>
        /// شناسه کاربر
        /// </summary>
        public long? UserId { get; set; }

        /// <summary>
        /// نام کاربری
        /// </summary>
        [MaxLength(100)]
        public string? UserName { get; set; }

        /// <summary>
        /// شناسه گروه کاربری
        /// </summary>
        public long? UserGroupId { get; set; }

        /// <summary>
        /// نام گروه کاربری
        /// </summary>
        [MaxLength(100)]
        public string? UserGroupName { get; set; }

        // ============================================
        // اطلاعات دستگاه و شبکه
        // ============================================

        /// <summary>
        /// آدرس IP
        /// </summary>
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User Agent مرورگر
        /// </summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// نام مرورگر
        /// </summary>
        [MaxLength(100)]
        public string? Browser { get; set; }

        /// <summary>
        /// نسخه مرورگر
        /// </summary>
        [MaxLength(50)]
        public string? BrowserVersion { get; set; }

        /// <summary>
        /// سیستم عامل
        /// </summary>
        [MaxLength(100)]
        public string? OperatingSystem { get; set; }

        /// <summary>
        /// نوع دستگاه
        /// مثال: Desktop, Mobile, Tablet
        /// </summary>
        [MaxLength(50)]
        public string? DeviceType { get; set; }

        // ============================================
        // زمان و مدت دسترسی
        // ============================================

        /// <summary>
        /// تاریخ و زمان دسترسی (UTC)
        /// </summary>
        public DateTime AccessDateTime { get; set; }

        /// <summary>
        /// تاریخ و زمان دسترسی (شمسی)
        /// </summary>
        [MaxLength(25)]
        public string? AccessDateTimePersian { get; set; }

        /// <summary>
        /// مدت زمان دسترسی (میلی‌ثانیه)
        /// برای View و Download
        /// </summary>
        public long? DurationMs { get; set; }

        // ============================================
        // نتیجه دسترسی
        // ============================================

        /// <summary>
        /// آیا دسترسی موفق بود
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// پیام خطا (در صورت شکست)
        /// </summary>
        [MaxLength(2000)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// دلیل رد دسترسی (در صورت عدم مجوز)
        /// </summary>
        [MaxLength(500)]
        public string? AccessDeniedReason { get; set; }

        /// <summary>
        /// کد وضعیت HTTP
        /// </summary>
        public int? HttpStatusCode { get; set; }

        // ============================================
        // اطلاعات انتقال داده
        // ============================================

        /// <summary>
        /// تعداد بایت منتقل شده
        /// </summary>
        public long? BytesTransferred { get; set; }

        /// <summary>
        /// آیا فایل رمزنگاری شده بود
        /// </summary>
        public bool? WasEncrypted { get; set; }

        /// <summary>
        /// آیا صحت فایل بررسی شد
        /// </summary>
        public bool? IntegrityVerified { get; set; }

        /// <summary>
        /// نتیجه بررسی صحت
        /// </summary>
        public bool? IntegrityCheckResult { get; set; }

        // ============================================
        // مشخصات امنیتی فایل در زمان دسترسی
        // ============================================

        /// <summary>
        /// سطح حساسیت فایل در زمان دسترسی
        /// </summary>
        public int? FileSensitivityLevel { get; set; }

        /// <summary>
        /// طبقه‌بندی امنیتی فایل
        /// </summary>
        [MaxLength(100)]
        public string? FileSecurityClassification { get; set; }

        // ============================================
        // Multi-tenancy
        // ============================================

        /// <summary>
        /// شناسه مشتری
        /// </summary>
        public long? tblCustomerId { get; set; }

        /// <summary>
        /// شناسه شعبه
        /// </summary>
        public long? tblShobeId { get; set; }

        // ============================================
        // اطلاعات تکمیلی
        // ============================================

        /// <summary>
        /// شناسه درخواست (Request ID) برای ردیابی
        /// </summary>
        [MaxLength(50)]
        public string? RequestId { get; set; }

        /// <summary>
        /// شناسه نشست کاربر
        /// </summary>
        [MaxLength(100)]
        public string? SessionId { get; set; }

        /// <summary>
        /// اطلاعات اضافی (JSON)
        /// </summary>
        public string? AdditionalInfo { get; set; }

        // ============================================
        // متدهای کمکی
        // ============================================

        /// <summary>
        /// تنظیم تاریخ دسترسی
        /// </summary>
        public void SetAccessDateTime(DateTime dateTime)
        {
            AccessDateTime = dateTime;
            AccessDateTimePersian = BaseEntity.ToPersianDateTime(dateTime);
        }

        /// <summary>
        /// دریافت نوع دسترسی به صورت Enum
        /// </summary>
        public AttachmentAccessType AccessTypeEnum
        {
            get => (AttachmentAccessType)AccessType;
            set => AccessType = (int)value;
        }

        /// <summary>
        /// ایجاد یک رکورد لاگ جدید
        /// </summary>
        public static tblAttachmentAccessLog Create(
            long attachmentId,
            Guid attachmentPublicId,
            string fileName,
            AttachmentAccessType accessType,
            long? userId,
            string? userName,
            string? ipAddress,
            bool isSuccess)
        {
            var log = new tblAttachmentAccessLog
            {
                AttachmentId = attachmentId,
                AttachmentPublicId = attachmentPublicId,
                FileName = fileName,
                AccessType = (int)accessType,
                UserId = userId,
                UserName = userName,
                IpAddress = ipAddress,
                IsSuccess = isSuccess,
                AccessDateTime = DateTime.UtcNow
            };
            log.AccessDateTimePersian = BaseEntity.ToPersianDateTime(DateTime.Now);
            log.SetZamanInsert(DateTime.Now);
            return log;
        }
    }
}
