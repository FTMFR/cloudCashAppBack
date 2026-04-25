using BnpCashClaudeApp.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace BnpCashClaudeApp.Domain.Entities.AuditLogSubsystem
{
    /// <summary>
    /// جدول جزئیات که جزئیات رویداد را در آن ذخیره می‌شود
    /// </summary>
    public class AuditLogDetail : BaseEntity
    {
        /// <summary>
        /// شناسه داخلی رکورد مستر (long)
        /// </summary>
        public long AuditLogMasterId { get; set; }

        /// <summary>
        /// نام فیلد یا ویژگی
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// مقدار قدیمی
        /// </summary>
        public string? OldValue { get; set; }

        /// <summary>
        /// مقدار جدید
        /// </summary>
        public string? NewValue { get; set; }

        /// <summary>
        /// نوع داده فیلد
        /// </summary>
        public string? DataType { get; set; }

        /// <summary>
        /// توضیحات اضافی
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// ارتباط با جدول مستر
        /// </summary>
        public virtual AuditLogMaster AuditLogMaster { get; set; }
    }
}

