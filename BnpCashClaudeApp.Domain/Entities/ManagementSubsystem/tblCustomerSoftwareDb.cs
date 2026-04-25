using BnpCashClaudeApp.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BnpCashClaudeApp.Domain.Entities.ManagementSubsystem
{
    /// <summary>
    /// جدول ارتباط اشتراک مشتری با دیتابیس
    /// یک اشتراک می‌تواند به چند دیتابیس متصل باشد (مثلاً Production و Backup)
    /// </summary>
    public class tblCustomerSoftwareDb : BaseEntity
    {
        /// <summary>
        /// شناسه اشتراک مشتری-نرم‌افزار (FK)
        /// </summary>
        public long tblCustomerSoftwareId { get; set; }

        /// <summary>
        /// شناسه دیتابیس (FK)
        /// </summary>
        public long tblDbId { get; set; }

        /// <summary>
        /// آیا دیتابیس اصلی این اشتراک است؟
        /// </summary>
        public bool IsPrimary { get; set; } = true;

        /// <summary>
        /// نوع استفاده: 1=اصلی, 2=بکاپ, 3=گزارش‌گیری, 4=تست
        /// </summary>
        public int UsageType { get; set; } = 1;

        /// <summary>
        /// تاریخ اتصال (شمسی)
        /// </summary>
        [MaxLength(25)]
        public string? ConnectedDate { get; set; }

        /// <summary>
        /// توضیحات
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// وضعیت فعال/غیرفعال
        /// </summary>
        public bool IsActive { get; set; } = true;

        // ============================================
        // Navigation Properties
        // ============================================

        /// <summary>
        /// اشتراک مشتری-نرم‌افزار
        /// </summary>
        [ForeignKey(nameof(tblCustomerSoftwareId))]
        public virtual tblCustomerSoftware? CustomerSoftware { get; set; }

        /// <summary>
        /// دیتابیس
        /// </summary>
        [ForeignKey(nameof(tblDbId))]
        public virtual tblDb? Db { get; set; }
    }
}
