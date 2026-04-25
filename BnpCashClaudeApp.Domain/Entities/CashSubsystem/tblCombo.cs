using BnpCashClaudeApp.Domain.Common;

namespace BnpCashClaudeApp.Domain.Entities.CashSubsystem
{
    /// <summary>
    /// موجودیت Combo - جدول دسته‌بندی‌های عمومی
    /// ============================================
    /// این جدول برای نگهداری مقادیر دسته‌بندی شده استفاده می‌شود
    /// هر دسته با GrpCode مشخص می‌شود
    /// مثال: انواع جنسیت، انواع وضعیت تاهل، انواع تحصیلات و ...
    /// ============================================
    /// </summary>
    public class tblCombo : BaseEntity
    {
        /// <summary>
        /// عنوان آیتم
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// کد گروه - برای دسته‌بندی آیتم‌ها
        /// مثال: 1 = جنسیت، 2 = وضعیت تاهل، 3 = تحصیلات
        /// </summary>
        public int GrpCode { get; set; }

        /// <summary>
        /// توضیحات
        /// </summary>
        public string? Description { get; set; }
    }
}
