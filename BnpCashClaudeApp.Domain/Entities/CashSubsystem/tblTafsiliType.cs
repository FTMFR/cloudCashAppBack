using BnpCashClaudeApp.Domain.Common;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Domain.Entities.CashSubsystem
{
    /// <summary>
    /// موجودیت انواع مشتری (انواع تفصیلی)
    /// ============================================
    /// هر نوع مشتری می‌تواند شامل چندین حوزه (AzaNoe) باشد
    /// ساختار سلسله‌مراتبی با ParentId
    /// ============================================
    /// </summary>
    public class tblTafsiliType : BaseEntity
    {
        /// <summary>
        /// شناسه شعبه (FK به NavigationDb)
        /// رابطه در سطح Application مدیریت می‌شود
        /// </summary>
        public long tblShobeId { get; set; }

        /// <summary>
        /// شناسه والد برای ساختار سلسله‌مراتبی
        /// </summary>
        public long? ParentId { get; set; }

        /// <summary>
        /// نوع تفصیلی والد
        /// </summary>
        public virtual tblTafsiliType? Parent { get; set; }

        /// <summary>
        /// زیرمجموعه‌های این نوع تفصیلی
        /// </summary>
        public virtual ICollection<tblTafsiliType> Children { get; set; } = new List<tblTafsiliType>();

        /// <summary>
        /// عنوان نوع مشتری
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// کد نوع تفصیلی - در Handler تولید می‌شود و در کل سیستم یکتا است
        /// </summary>
        public int CodeTafsiliType { get; set; }

        /// <summary>
        /// وضعیت فعال/غیرفعال (Soft Delete)
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// آیا حذف شده است (Soft Delete)
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// حوزه‌های مرتبط با این نوع مشتری
        /// </summary>
        public virtual ICollection<tblAzaNoe> AzaNoeList { get; set; } = new List<tblAzaNoe>();
    }
}
