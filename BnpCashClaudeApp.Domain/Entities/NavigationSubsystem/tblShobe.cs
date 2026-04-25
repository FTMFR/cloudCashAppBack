using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.ManagementSubsystem;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BnpCashClaudeApp.Domain.Entities.NavigationSubsystem
{
    /// <summary>
    /// موجودیت شعبه
    /// ============================================
    /// مدیریت اطلاعات شعب سازمان
    /// هر شعبه متعلق به یک مشتری است
    /// ============================================
    /// </summary>
    public class tblShobe : BaseEntity
    {
        /// <summary>
        /// شناسه مشتری (FK)
        /// هر شعبه متعلق به یک مشتری است
        /// </summary>
        public long? tblCustomerId { get; set; }

        /// <summary>
        /// نام شعبه
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// کد شعبه (یکتا)
        /// </summary>
        public int ShobeCode { get; set; }

        /// <summary>
        /// آدرس شعبه
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// شماره تلفن شعبه
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// کد پستی
        /// </summary>
        public string? PostalCode { get; set; }

        /// <summary>
        /// شناسه شعبه والد (برای ساختار سلسله‌مراتبی)
        /// </summary>
        public long? ParentId { get; set; }

        /// <summary>
        /// آیا شعبه فعال است
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// توضیحات
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// ترتیب نمایش
        /// </summary>
        public int DisplayOrder { get; set; }

        // ============================================
        // Navigation Properties
        // ============================================

        /// <summary>
        /// شعبه والد
        /// </summary>
        public virtual tblShobe? Parent { get; set; }

        /// <summary>
        /// زیرشعب
        /// </summary>
        public virtual ICollection<tblShobe> Children { get; set; } = new List<tblShobe>();

        /// <summary>
        /// مشتری مالک این شعبه
        /// </summary>
        [ForeignKey(nameof(tblCustomerId))]
        public virtual tblCustomer? Customer { get; set; }

        /// <summary>
        /// کاربران این شعبه
        /// </summary>
        public virtual ICollection<tblUser> Users { get; set; } = new List<tblUser>();

        /// <summary>
        /// گروه‌های کاربری این شعبه
        /// </summary>
        public virtual ICollection<tblGrp> Groups { get; set; } = new List<tblGrp>();
    }
}

