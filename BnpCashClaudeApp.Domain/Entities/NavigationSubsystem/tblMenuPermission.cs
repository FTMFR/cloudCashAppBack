using BnpCashClaudeApp.Domain.Common;
using System;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Domain.Entities.NavigationSubsystem
{
    /// <summary>
    /// جدول ارتباط بین منو و Permission
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF از استاندارد ISO 15408
    /// تعیین Permission های مورد نیاز برای دسترسی به هر منو
    /// ============================================
    /// </summary>
    public class tblMenuPermission : BaseEntity
    {
        /// <summary>
        /// شناسه داخلی منو (long)
        /// </summary>
        public long tblMenuId { get; set; }
        
        /// <summary>
        /// شناسه داخلی Permission (long)
        /// </summary>
        public long tblPermissionId { get; set; }
        
        /// <summary>
        /// آیا این Permission الزامی است؟
        /// اگر true باشد: منطق AND (همه Permission ها باید وجود داشته باشند)
        /// اگر false باشد: منطق OR (داشتن یکی از Permission ها کافی است)
        /// </summary>
        public bool IsRequired { get; set; } = true;
        
        /// <summary>
        /// توضیحات اضافی
        /// </summary>
        public string? Notes { get; set; }
        
        /// <summary>
        /// Navigation Property به منو
        /// </summary>
        public virtual tblMenu tblMenu { get; set; } = null!;
        
        /// <summary>
        /// Navigation Property به Permission
        /// </summary>
        public virtual tblPermission tblPermission { get; set; } = null!;
    }
}

