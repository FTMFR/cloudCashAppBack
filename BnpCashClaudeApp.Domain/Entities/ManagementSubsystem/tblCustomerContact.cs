using BnpCashClaudeApp.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BnpCashClaudeApp.Domain.Entities.ManagementSubsystem
{
    /// <summary>
    /// جدول مخاطبین مشتری
    /// هر مشتری می‌تواند چندین مخاطب (نماینده، پشتیبان، مالی) داشته باشد
    /// </summary>
    public class tblCustomerContact : BaseEntity
    {
        /// <summary>
        /// شناسه مشتری (FK)
        /// </summary>
        public long tblCustomerId { get; set; }

        /// <summary>
        /// نام مخاطب
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// سمت/عنوان شغلی
        /// </summary>
        [MaxLength(100)]
        public string? JobTitle { get; set; }

        /// <summary>
        /// نوع مخاطب: 1=اصلی, 2=فنی, 3=مالی, 4=مدیریتی
        /// </summary>
        public int ContactType { get; set; } = 1;

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
        /// ایمیل
        /// </summary>
        [MaxLength(200)]
        public string? Email { get; set; }

        /// <summary>
        /// تلگرام/واتساپ
        /// </summary>
        [MaxLength(100)]
        public string? Messenger { get; set; }

        /// <summary>
        /// توضیحات
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// آیا مخاطب اصلی است؟
        /// </summary>
        public bool IsPrimary { get; set; } = false;

        /// <summary>
        /// وضعیت فعال/غیرفعال
        /// </summary>
        public bool IsActive { get; set; } = true;

        // ============================================
        // Navigation Properties
        // ============================================

        /// <summary>
        /// مشتری مرتبط
        /// </summary>
        [ForeignKey(nameof(tblCustomerId))]
        public virtual tblCustomer? Customer { get; set; }
    }
}
