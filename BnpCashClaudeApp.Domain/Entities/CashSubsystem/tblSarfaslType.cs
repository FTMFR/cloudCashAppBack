using BnpCashClaudeApp.Domain.Common;

namespace BnpCashClaudeApp.Domain.Entities.CashSubsystem
{
    /// <summary>
    /// موجودیت نوع سرفصل
    /// ============================================
    /// این جدول برای نگهداری انواع سرفصل‌های حسابداری استفاده می‌شود
    /// مثال: تسهیلات، حساب پس‌انداز، صندوق، بانک، کارمزد و ...
    /// ============================================
    /// </summary>
    public class tblSarfaslType : BaseEntity
    {
        /// <summary>
        /// عنوان نوع سرفصل
        /// </summary>
        public string Title { get; set; } = string.Empty;
    }
}
