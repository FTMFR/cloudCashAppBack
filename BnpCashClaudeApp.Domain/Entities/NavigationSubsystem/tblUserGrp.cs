using BnpCashClaudeApp.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BnpCashClaudeApp.Domain.Entities.NavigationSubsystem
{
    /// <summary>
    /// جدول ارتباط کاربر و گروه
    /// ============================================
    /// تاریخ‌ها به صورت شمسی ذخیره می‌شوند
    /// ============================================
    /// </summary>
    public class tblUserGrp : BaseEntity
    {
        /// <summary>
        /// شناسه داخلی کاربر (long)
        /// </summary>
        public long tblUserId { get; set; }
        
        /// <summary>
        /// شناسه داخلی گروه (long)
        /// </summary>
        public long tblGrpId { get; set; }

        /// <summary>
        /// تاریخ انتساب (به صورت شمسی)
        /// </summary>
        public string AssignmentDate { get; set; } = BaseEntity.GetNowPersian();

        public virtual tblUser tblUser { get; set; }
        public virtual tblGrp tblGrp { get; set; }
    }
}
