using BnpCashClaudeApp.Domain.Common;

namespace BnpCashClaudeApp.Domain.Entities.CashSubsystem
{
    /// <summary>
    /// موجودیت انواع حوزه (دسته‌بندی)
    /// ============================================
    /// هر حوزه به یک نوع مشتری (TafsiliType) وصل می‌شود
    /// چند حوزه می‌توانند یک نوع مشتری مشترک داشته باشند
    /// ============================================
    /// </summary>
    public class tblAzaNoe : BaseEntity
    {
        /// <summary>
        /// شناسه شعبه (FK به NavigationDb)
        /// رابطه در سطح Application مدیریت می‌شود
        /// </summary>
        public long tblShobeId { get; set; }

        /// <summary>
        /// عنوان حوزه
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// کد حوزه - یک عدد ساده
        /// </summary>
        public int CodeHoze { get; set; }

        /// <summary>
        /// آیا این حوزه پیش‌فرض است
        /// </summary>
        public bool PishFarz { get; set; } = false;

        /// <summary>
        /// شناسه نوع مشتری (FK)
        /// هر حوزه به یک نوع مشتری وصل می‌شود
        /// </summary>
        public long tblTafsiliTypeId { get; set; }

        /// <summary>
        /// نوع مشتری مرتبط
        /// </summary>
        public virtual tblTafsiliType? TafsiliType { get; set; }

        /// <summary>
        /// وضعیت فعال/غیرفعال (Soft Delete)
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// آیا حذف شده است (Soft Delete)
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}
