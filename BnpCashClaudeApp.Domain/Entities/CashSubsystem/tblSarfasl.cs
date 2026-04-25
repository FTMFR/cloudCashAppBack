using BnpCashClaudeApp.Domain.Common;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Domain.Entities.CashSubsystem
{
    /// <summary>
    /// موجودیت سرفصل حسابداری
    /// ============================================
    /// این جدول برای نگهداری سرفصل‌های حسابداری استفاده می‌شود
    /// هر سرفصل می‌تواند زیرمجموعه داشته باشد (سلسله‌مراتبی)
    /// ============================================
    /// </summary>
    public class tblSarfasl : BaseEntity
    {
        /// <summary>
        /// شناسه شعبه (FK به NavigationDb)
        /// پیش‌فرض: 1
        /// </summary>
        public long tblShobeId { get; set; } = 1;

        /// <summary>
        /// شناسه والد (Self-Referencing برای سلسله‌مراتب)
        /// </summary>
        public long? ParentId { get; set; }

        /// <summary>
        /// شناسه نوع سرفصل (FK به tblSarfaslType)
        /// </summary>
        public long? tblSarfaslTypeId { get; set; }

        /// <summary>
        /// شناسه پروتکل سرفصل (FK به tblSarfaslProtocol)
        /// </summary>
        public long? tblSarfaslProtocolId { get; set; }

        /// <summary>
        /// کد سرفصل - الزامی
        /// </summary>
        public string CodeSarfasl { get; set; } = string.Empty;

        /// <summary>
        /// عنوان سرفصل
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// توضیحات
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// دارای جزء تفصیلی
        /// </summary>
        public bool WithJoze { get; set; } = false;

        /// <summary>
        /// وضعیت زیرگروه (FK به tblCombo با GrpCode=15)
        /// </summary>
        public long? tblComboIdVazeiatZirGrp { get; set; }

        /// <summary>
        /// تعداد ارقام زیرگروه
        /// </summary>
        public int? TedadArghamZirGrp { get; set; }

        /// <summary>
        /// میزان اعتبار بدهکار
        /// </summary>
        public decimal MizanEtebarBedehkar { get; set; } = 0;

        /// <summary>
        /// میزان اعتبار بستانکار
        /// </summary>
        public decimal MizanEtebarBestankar { get; set; } = 0;

        /// <summary>
        /// کنترل عملیات (FK به tblCombo با GrpCode=16)
        /// </summary>
        public long? tblComboIdControlAmaliat { get; set; }

        /// <summary>
        /// عدم نمایش در تراز
        /// </summary>
        public bool NotShowInTaraz { get; set; } = false;

        // ============================================
        // Navigation Properties
        // ============================================

        /// <summary>
        /// سرفصل والد
        /// </summary>
        public virtual tblSarfasl? Parent { get; set; }

        /// <summary>
        /// زیرمجموعه‌ها
        /// </summary>
        public virtual ICollection<tblSarfasl>? Children { get; set; }

        /// <summary>
        /// نوع سرفصل
        /// </summary>
        public virtual tblSarfaslType? SarfaslType { get; set; }

        /// <summary>
        /// پروتکل سرفصل
        /// </summary>
        public virtual tblSarfaslProtocol? SarfaslProtocol { get; set; }
    }
}
