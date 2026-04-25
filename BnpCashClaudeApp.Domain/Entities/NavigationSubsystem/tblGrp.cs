using BnpCashClaudeApp.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BnpCashClaudeApp.Domain.Entities.NavigationSubsystem
{
    /// <summary>
    /// موجودیت گروه کاربری
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF از استاندارد ISO 15408
    /// گروه‌ها مجموعه‌ای از کاربران با دسترسی‌های مشابه هستند
    /// ============================================
    /// </summary>
    public class tblGrp : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public int? GrpCode { get; set; }
        
        /// <summary>
        /// توضیحات گروه
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// آیا گروه فعال است
        /// </summary>
        public bool IsActive { get; set; } = true;


        /// <summary>
        /// برای ساختار سلسله‌مراتبی
        /// </summary>
        public long? ParentId { get; set; }

        public virtual tblGrp? Parent { get; set; }


        public virtual ICollection<tblGrp> Children { get; set; } = new List<tblGrp>();

        /// <summary>
        /// شناسه شعبه (FK)
        /// اگر null باشد، این گروه برای همه شعبات قابل استفاده است
        /// </summary>
        public long? tblShobeId { get; set; }

        /// <summary>
        /// شعبه مرتبط با این گروه
        /// </summary>
        [ForeignKey(nameof(tblShobeId))]
        public virtual tblShobe? Shobe { get; set; }

        /// <summary>
        /// Permission های این گروه
        /// </summary>
        public virtual ICollection<tblGrpPermission> GroupPermissions { get; set; } = new List<tblGrpPermission>();
        
        /// <summary>
        /// کاربران عضو این گروه
        /// </summary>
        public virtual ICollection<tblUserGrp> UserGroups { get; set; } = new List<tblUserGrp>();
    }
}
