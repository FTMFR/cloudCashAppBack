using BnpCashClaudeApp.Domain.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BnpCashClaudeApp.Domain.Entities.ManagementSubsystem
{
    /// <summary>
    /// جدول اشتراک مشتری روی نرم‌افزار
    /// این جدول قلب راهبری است و مشخص می‌کند هر مشتری چه نرم‌افزاری با چه پلنی دارد
    /// مثال: مشتری "ساز مانی تک" نرم‌افزار "قرض‌الحسنه" با پلن "۱۰۰ عضو" دارد
    /// </summary>
    public class tblCustomerSoftware : BaseEntity
    {
        /// <summary>
        /// شناسه مشتری (FK)
        /// </summary>
        public long tblCustomerId { get; set; }

        /// <summary>
        /// شناسه نرم‌افزار (FK)
        /// </summary>
        public long tblSoftwareId { get; set; }

        /// <summary>
        /// شناسه پلن (FK)
        /// </summary>
        public long tblPlanId { get; set; }

        /// <summary>
        /// کد لایسنس یکتا
        /// مثال: "LIC-QARZ-MT-1403-001"
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string LicenseKey { get; set; } = string.Empty;

        /// <summary>
        /// تعداد لایسنس/ظرفیت خریداری شده
        /// مثال: ۱۰۰ (عضو)
        /// </summary>
        public int LicenseCount { get; set; } = 1;

        /// <summary>
        /// تعداد مصرف شده از ظرفیت
        /// </summary>
        public int UsedCount { get; set; } = 0;

        /// <summary>
        /// تاریخ شروع اشتراک (شمسی)
        /// </summary>
        [MaxLength(25)]
        public string? StartDate { get; set; }

        /// <summary>
        /// تاریخ پایان اشتراک (شمسی)
        /// null = دائمی
        /// </summary>
        [MaxLength(25)]
        public string? EndDate { get; set; }

        /// <summary>
        /// نوع اشتراک: 1=دائمی, 2=ماهانه, 3=سالانه
        /// </summary>
        public int SubscriptionType { get; set; } = 1;

        /// <summary>
        /// وضعیت: 1=فعال, 2=غیرفعال, 3=تعلیق, 4=منقضی
        /// </summary>
        public int Status { get; set; } = 1;

        /// <summary>
        /// نسخه نرم‌افزار نصب شده
        /// </summary>
        [MaxLength(20)]
        public string? InstalledVersion { get; set; }

        /// <summary>
        /// تاریخ آخرین فعال‌سازی (شمسی)
        /// </summary>
        [MaxLength(25)]
        public string? LastActivationDate { get; set; }

        /// <summary>
        /// IP آخرین فعال‌سازی
        /// </summary>
        [MaxLength(50)]
        public string? LastActivationIp { get; set; }

        /// <summary>
        /// تعداد فعال‌سازی‌ها
        /// </summary>
        public int ActivationCount { get; set; } = 0;

        /// <summary>
        /// حداکثر تعداد فعال‌سازی مجاز
        /// null = نامحدود
        /// </summary>
        public int? MaxActivations { get; set; }

        /// <summary>
        /// تنظیمات اختصاصی این مشتری به صورت JSON
        /// </summary>
        [MaxLength(4000)]
        public string? CustomSettingsJson { get; set; }

        /// <summary>
        /// توضیحات
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// مبلغ پرداختی (تومان)
        /// </summary>
        [Column(TypeName = "decimal(18,0)")]
        public decimal? PaidAmount { get; set; }

        /// <summary>
        /// تخفیف اعمال شده (درصد)
        /// </summary>
        public int? DiscountPercent { get; set; }

        // ============================================
        // Navigation Properties
        // ============================================

        /// <summary>
        /// مشتری
        /// </summary>
        [ForeignKey(nameof(tblCustomerId))]
        public virtual tblCustomer? Customer { get; set; }

        /// <summary>
        /// نرم‌افزار
        /// </summary>
        [ForeignKey(nameof(tblSoftwareId))]
        public virtual tblSoftware? Software { get; set; }

        /// <summary>
        /// پلن
        /// </summary>
        [ForeignKey(nameof(tblPlanId))]
        public virtual tblPlan? Plan { get; set; }

        /// <summary>
        /// لیست دیتابیس‌های مرتبط با این اشتراک
        /// </summary>
        public virtual ICollection<tblCustomerSoftwareDb> CustomerSoftwareDbs { get; set; } = new List<tblCustomerSoftwareDb>();
    }
}
