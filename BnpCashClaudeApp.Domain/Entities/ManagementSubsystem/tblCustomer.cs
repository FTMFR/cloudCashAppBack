using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BnpCashClaudeApp.Domain.Entities.ManagementSubsystem
{
    /// <summary>
    /// جدول مشتریان
    /// مشتریان حقیقی یا حقوقی که نرم‌افزار خریداری می‌کنند
    /// مثال: "قرض‌الحسنه ساز مانی تک"
    /// </summary>
    public class tblCustomer : BaseEntity
    {
        /// <summary>
        /// نام مشتری (حقیقی یا حقوقی)
        /// مثال: "قرض‌الحسنه ساز مانی تک"
        /// </summary>
        [Required]
        [MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// کد مشتری (یکتا)
        /// مثال: "CUST-001", "MT-1403"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string CustomerCode { get; set; } = string.Empty;

        /// <summary>
        /// نوع مشتری: 1=حقیقی, 2=حقوقی
        /// </summary>
        public int CustomerType { get; set; } = 2;

        /// <summary>
        /// کد ملی (برای حقیقی)
        /// </summary>
        [MaxLength(10)]
        public string? NationalId { get; set; }

        /// <summary>
        /// شماره ثبت (برای حقوقی)
        /// </summary>
        [MaxLength(20)]
        public string? RegistrationNumber { get; set; }

        /// <summary>
        /// شناسه ملی شرکت (برای حقوقی)
        /// </summary>
        [MaxLength(11)]
        public string? CompanyNationalId { get; set; }

        /// <summary>
        /// کد اقتصادی
        /// </summary>
        [MaxLength(20)]
        public string? EconomicCode { get; set; }

        /// <summary>
        /// نام مدیرعامل/مسئول
        /// </summary>
        [MaxLength(200)]
        public string? ManagerName { get; set; }

        /// <summary>
        /// شماره تلفن
        /// </summary>
        [MaxLength(20)]
        public string? Phone { get; set; }

        /// <summary>
        /// شماره موبایل
        /// </summary>
        [MaxLength(15)]
        public string? Mobile { get; set; }

        /// <summary>
        /// فکس
        /// </summary>
        [MaxLength(20)]
        public string? Fax { get; set; }

        /// <summary>
        /// ایمیل
        /// </summary>
        [MaxLength(200)]
        public string? Email { get; set; }

        /// <summary>
        /// وب‌سایت
        /// </summary>
        [MaxLength(300)]
        public string? Website { get; set; }

        /// <summary>
        /// آدرس
        /// </summary>
        [MaxLength(500)]
        public string? Address { get; set; }

        /// <summary>
        /// کد پستی
        /// </summary>
        [MaxLength(10)]
        public string? PostalCode { get; set; }

        /// <summary>
        /// استان
        /// </summary>
        [MaxLength(100)]
        public string? Province { get; set; }

        /// <summary>
        /// شهر
        /// </summary>
        [MaxLength(100)]
        public string? City { get; set; }

        /// <summary>
        /// لوگوی مشتری (مسیر فایل)
        /// </summary>
        [MaxLength(500)]
        public string? LogoPath { get; set; }

        /// <summary>
        /// توضیحات
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// تاریخ عضویت (شمسی)
        /// </summary>
        [MaxLength(25)]
        public string? MembershipDate { get; set; }

        /// <summary>
        /// وضعیت: 1=فعال, 2=غیرفعال, 3=تعلیق, 4=منقضی
        /// </summary>
        public int Status { get; set; } = 1;

        /// <summary>
        /// امتیاز مشتری (برای سیستم وفاداری)
        /// </summary>
        public int? LoyaltyPoints { get; set; }

        /// <summary>
        /// سطح مشتری: 1=عادی, 2=نقره‌ای, 3=طلایی, 4=الماسی
        /// </summary>
        public int CustomerLevel { get; set; } = 1;

        // ============================================
        // Navigation Properties
        // ============================================

        /// <summary>
        /// لیست اشتراک‌های نرم‌افزاری این مشتری
        /// </summary>
        public virtual ICollection<tblCustomerSoftware> CustomerSoftwares { get; set; } = new List<tblCustomerSoftware>();

        /// <summary>
        /// لیست مخاطبین این مشتری
        /// </summary>
        public virtual ICollection<tblCustomerContact> Contacts { get; set; } = new List<tblCustomerContact>();

        /// <summary>
        /// لیست دیتابیس‌های اختصاصی این مشتری
        /// </summary>
        public virtual ICollection<tblDb> Databases { get; set; } = new List<tblDb>();

        /// <summary>
        /// لیست شعب این مشتری
        /// </summary>
        public virtual ICollection<tblShobe> Shobes { get; set; } = new List<tblShobe>();

        /// <summary>
        /// لیست کاربران این مشتری
        /// </summary>
        public virtual ICollection<tblUser> Users { get; set; } = new List<tblUser>();
    }
}
