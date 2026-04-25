using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.ManagementSubsystem;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BnpCashClaudeApp.Domain.Entities.NavigationSubsystem
{
    /// <summary>
    /// موجودیت منو
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF از استاندارد ISO 15408
    /// منوها با Permission ها ارتباط دارند
    /// ============================================
    /// </summary>
    public class tblMenu : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string? Path { get; set; }
        /// <summary>
        /// آیکون منو
        /// </summary>
        public string? Icon { get; set; }
        /// <summary>
        /// شناسه داخلی منوی والد (long)
        /// </summary>
        public long? ParentId { get; set; }
        public bool IsMenu { get; set; }

        /// <summary>
        /// شناسه نرم‌افزار
        /// null = منوی عمومی (راهبری سیستم - برای همه نرم‌افزارها)
        /// </summary>
        public long? tblSoftwareId { get; set; }

        /// <summary>
        /// نرم‌افزار مربوطه
        /// </summary>
        [ForeignKey(nameof(tblSoftwareId))]
        public virtual tblSoftware? Software { get; set; }
        
        /// <summary>
        /// منوی والد
        /// </summary>
        public virtual tblMenu? Parent { get; set; }
        
        /// <summary>
        /// زیرمنوها
        /// </summary>
        public virtual ICollection<tblMenu> Children { get; set; } = new List<tblMenu>();
        
        /// <summary>
        /// Permission های مورد نیاز برای دسترسی به این منو
        /// </summary>
        public virtual ICollection<tblMenuPermission> MenuPermissions { get; set; } = new List<tblMenuPermission>();
    }
}
