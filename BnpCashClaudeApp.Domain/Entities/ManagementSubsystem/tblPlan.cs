using BnpCashClaudeApp.Domain.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BnpCashClaudeApp.Domain.Entities.ManagementSubsystem
{
    /// <summary>
    /// جدول پلن‌های نرم‌افزار
    /// هر نرم‌افزار می‌تواند چندین پلن داشته باشد (تعداد کاربر، تعداد عضو، امکانات)
    /// مثال: پلن ۵۰ عضو، پلن ۱۰۰ عضو، پلن نامحدود
    /// </summary>
    public class tblPlan : BaseEntity
    {
        /// <summary>
        /// شناسه نرم‌افزار (FK)
        /// </summary>
        public long tblSoftwareId { get; set; }

        /// <summary>
        /// نام پلن
        /// مثال: "پلن پایه ۱۰۰ عضو"
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// کد یکتای پلن
        /// مثال: "BASIC_100", "PRO_500", "UNLIMITED"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// توضیحات پلن
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// حداکثر تعداد اعضا
        /// null = نامحدود
        /// </summary>
        public int? MaxMemberCount { get; set; }

        /// <summary>
        /// حداکثر تعداد کاربران سیستمی
        /// null = نامحدود
        /// </summary>
        public int? MaxUserCount { get; set; }

        /// <summary>
        /// حداکثر تعداد شعبه
        /// null = نامحدود
        /// </summary>
        public int? MaxBranchCount { get; set; }

        /// <summary>
        /// حداکثر حجم دیتابیس (مگابایت)
        /// null = نامحدود
        /// </summary>
        public int? MaxDbSizeMB { get; set; }

        /// <summary>
        /// حداکثر تعداد تراکنش روزانه
        /// null = نامحدود
        /// </summary>
        public int? MaxDailyTransactions { get; set; }

        /// <summary>
        /// لیست امکانات پلن به صورت JSON
        /// مثال: ["backup", "report", "sms", "api"]
        /// </summary>
        [MaxLength(2000)]
        public string? FeaturesJson { get; set; }

        /// <summary>
        /// قیمت پایه (تومان)
        /// </summary>
        [Column(TypeName = "decimal(18,0)")]
        public decimal? BasePrice { get; set; }

        /// <summary>
        /// قیمت ماهانه (تومان)
        /// </summary>
        [Column(TypeName = "decimal(18,0)")]
        public decimal? MonthlyPrice { get; set; }

        /// <summary>
        /// قیمت سالانه (تومان)
        /// </summary>
        [Column(TypeName = "decimal(18,0)")]
        public decimal? YearlyPrice { get; set; }

        /// <summary>
        /// نوع پلن: 1=تک‌کاربره, 2=چندکاربره, 3=سازمانی
        /// </summary>
        public int PlanType { get; set; } = 1;

        /// <summary>
        /// وضعیت فعال/غیرفعال
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// آیا پلن پیش‌فرض است؟
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// ترتیب نمایش
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        // ============================================
        // Navigation Properties
        // ============================================

        /// <summary>
        /// نرم‌افزار مرتبط
        /// </summary>
        [ForeignKey(nameof(tblSoftwareId))]
        public virtual tblSoftware? Software { get; set; }

        /// <summary>
        /// لیست اشتراک‌های مشتریان با این پلن
        /// </summary>
        public virtual ICollection<tblCustomerSoftware> CustomerSoftwares { get; set; } = new List<tblCustomerSoftware>();
    }
}
