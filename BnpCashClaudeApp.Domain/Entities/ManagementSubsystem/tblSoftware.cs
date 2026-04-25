using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BnpCashClaudeApp.Domain.Entities.ManagementSubsystem
{
    /// <summary>
    /// جدول نرم‌افزارها/محصولات
    /// هر محصول نرم‌افزاری که شرکت تولید می‌کند در این جدول ثبت می‌شود
    /// مثال: نرم‌افزار قرض‌الحسنه، نرم‌افزار حسابداری، و...
    /// </summary>
    public class tblSoftware : BaseEntity
    {
        /// <summary>
        /// نام نرم‌افزار
        /// مثال: "نرم‌افزار قرض‌الحسنه"
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// کد یکتای نرم‌افزار
        /// برای استفاده در سیستم‌ها و کانفیگ
        /// مثال: "QARZ", "ACC", "LOAN"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// نسخه فعلی نرم‌افزار
        /// مثال: "1.5.2"
        /// </summary>
        [MaxLength(20)]
        public string? CurrentVersion { get; set; }

        /// <summary>
        /// توضیحات نرم‌افزار
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// آیکون نرم‌افزار (مسیر فایل یا نام کلاس CSS)
        /// </summary>
        [MaxLength(200)]
        public string? Icon { get; set; }

        /// <summary>
        /// آدرس وب‌سایت نرم‌افزار
        /// </summary>
        [MaxLength(500)]
        public string? WebsiteUrl { get; set; }

        /// <summary>
        /// آدرس دانلود نرم‌افزار
        /// </summary>
        [MaxLength(500)]
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// وضعیت فعال/غیرفعال
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// ترتیب نمایش
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        // ============================================
        // Navigation Properties
        // ============================================

        /// <summary>
        /// لیست پلن‌های این نرم‌افزار
        /// </summary>
        public virtual ICollection<tblPlan> Plans { get; set; } = new List<tblPlan>();

        /// <summary>
        /// لیست اشتراک‌های مشتریان روی این نرم‌افزار
        /// </summary>
        public virtual ICollection<tblCustomerSoftware> CustomerSoftwares { get; set; } = new List<tblCustomerSoftware>();

        /// <summary>
        /// لیست دیتابیس‌های مرتبط با این نرم‌افزار
        /// </summary>
        public virtual ICollection<tblDb> Databases { get; set; } = new List<tblDb>();

        /// <summary>
        /// لیست منوهای این نرم‌افزار
        /// </summary>
        public virtual ICollection<tblMenu> Menus { get; set; } = new List<tblMenu>();
    }
}
