using BnpCashClaudeApp.Domain.Common;

namespace BnpCashClaudeApp.Domain.Entities.CashSubsystem
{
    /// <summary>
    /// موجودیت پروتکل سرفصل
    /// ============================================
    /// این جدول برای نگهداری پروتکل‌های مختلف سرفصل استفاده می‌شود
    /// هر پروتکل می‌تواند ساختار JSON سرفصل‌های پیش‌فرض خود را داشته باشد
    /// مثال: پروتکل بانک مرکزی، سازمان اقتصاد، پیش‌فرض سیستم و ...
    /// ============================================
    /// </summary>
    public class tblSarfaslProtocol : BaseEntity
    {
        /// <summary>
        /// عنوان پروتکل
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// کد پروتکل (یکتا)
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// توضیحات پروتکل
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// ساختار JSON سرفصل‌های پیش‌فرض این پروتکل
        /// </summary>
        public string? DefaultSarfaslJson { get; set; }

        /// <summary>
        /// آیا این پروتکل پیش‌فرض سیستم است
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// وضعیت فعال/غیرفعال
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
