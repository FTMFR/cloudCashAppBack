using BnpCashClaudeApp.Domain.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BnpCashClaudeApp.Domain.Entities.ManagementSubsystem
{
    /// <summary>
    /// جدول دیتابیس‌ها
    /// هر نرم‌افزار/مشتری می‌تواند یک یا چند دیتابیس داشته باشد
    /// این جدول اطلاعات اتصال به دیتابیس‌ها را نگهداری می‌کند
    /// </summary>
    public class tblDb : BaseEntity
    {
        /// <summary>
        /// شناسه مشتری (FK) - اختیاری
        /// اگر دیتابیس مختص یک مشتری باشد
        /// </summary>
        public long? tblCustomerId { get; set; }

        /// <summary>
        /// شناسه نرم‌افزار (FK)
        /// </summary>
        public long tblSoftwareId { get; set; }

        /// <summary>
        /// نام دیتابیس (نمایشی)
        /// مثال: "دیتابیس قرض‌الحسنه "
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// کد دیتابیس (یکتا)
        /// مثال: "DB_QARZ_MT_001"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string DbCode { get; set; } = string.Empty;

        /// <summary>
        /// نام سرور
        /// مثال: "localhost", "192.168.1.100", "sql.example.com"
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string ServerName { get; set; } = string.Empty;

        /// <summary>
        /// پورت سرور
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// نام دیتابیس در سرور
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// نام کاربری دیتابیس
        /// </summary>
        [MaxLength(100)]
        public string? Username { get; set; }

        /// <summary>
        /// رمز عبور دیتابیس (رمزگذاری شده)
        /// </summary>
        [MaxLength(500)]
        public string? EncryptedPassword { get; set; }

        /// <summary>
        /// Connection String کامل (رمزگذاری شده) - اختیاری
        /// اگر تنظیم شود، از این استفاده می‌شود
        /// </summary>
        [MaxLength(2000)]
        public string? EncryptedConnectionString { get; set; }

        /// <summary>
        /// نوع دیتابیس: 1=SqlServer, 2=MySQL, 3=PostgreSQL, 4=Oracle, 5=SQLite
        /// </summary>
        public int DbType { get; set; } = 1;

        /// <summary>
        /// محیط: 1=Development, 2=Test, 3=Staging, 4=Production
        /// </summary>
        public int Environment { get; set; } = 4;

        /// <summary>
        /// آیا دیتابیس مشترک بین چند مشتری است؟
        /// اگر بله، از TenantId برای جداسازی استفاده می‌شود
        /// </summary>
        public bool IsShared { get; set; } = false;

        /// <summary>
        /// TenantId برای دیتابیس‌های مشترک
        /// </summary>
        [MaxLength(50)]
        public string? TenantId { get; set; }

        /// <summary>
        /// آیا دیتابیس اصلی مشتری است؟
        /// </summary>
        public bool IsPrimary { get; set; } = true;

        /// <summary>
        /// آیا دیتابیس فقط خواندنی است؟ (Replica)
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// حداکثر حجم مجاز (مگابایت)
        /// null = نامحدود
        /// </summary>
        public int? MaxSizeMB { get; set; }

        /// <summary>
        /// حجم فعلی (مگابایت)
        /// </summary>
        public int? CurrentSizeMB { get; set; }

        /// <summary>
        /// تاریخ آخرین بکاپ (شمسی)
        /// </summary>
        [MaxLength(25)]
        public string? LastBackupDate { get; set; }

        /// <summary>
        /// تاریخ آخرین تست اتصال (شمسی)
        /// </summary>
        [MaxLength(25)]
        public string? LastConnectionTestDate { get; set; }

        /// <summary>
        /// نتیجه آخرین تست اتصال
        /// </summary>
        public bool? LastConnectionTestResult { get; set; }

        /// <summary>
        /// توضیحات
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// وضعیت: 1=فعال, 2=غیرفعال, 3=در حال نگهداری
        /// </summary>
        public int Status { get; set; } = 1;

        /// <summary>
        /// ترتیب نمایش
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        // ============================================
        // Navigation Properties
        // ============================================

        /// <summary>
        /// مشتری مالک دیتابیس
        /// </summary>
        [ForeignKey(nameof(tblCustomerId))]
        public virtual tblCustomer? Customer { get; set; }

        /// <summary>
        /// نرم‌افزار مرتبط
        /// </summary>
        [ForeignKey(nameof(tblSoftwareId))]
        public virtual tblSoftware? Software { get; set; }

        /// <summary>
        /// لیست اشتراک‌های مرتبط با این دیتابیس
        /// </summary>
        public virtual ICollection<tblCustomerSoftwareDb> CustomerSoftwareDbs { get; set; } = new List<tblCustomerSoftwareDb>();
    }
}
