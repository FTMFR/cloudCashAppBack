using BnpCashClaudeApp.Domain.Common;
using System;

namespace BnpCashClaudeApp.Domain.Entities.NavigationSubsystem
{
    /// <summary>
    /// جدول ارتباط بین گروه و Permission
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF از استاندارد ISO 15408
    /// تعیین می‌کند که هر گروه چه Permission هایی دارد
    /// ============================================
    /// </summary>
    public class tblGrpPermission : BaseEntity
    {
        /// <summary>
        /// شناسه داخلی گروه (long)
        /// </summary>
        public long tblGrpId { get; set; }

        /// <summary>
        /// شناسه داخلی Permission (long)
        /// </summary>
        public long tblPermissionId { get; set; }

        /// <summary>
        /// آیا این Permission به گروه اعطا شده است
        /// true: دسترسی دارد
        /// false: دسترسی ندارد (برای Deny صریح)
        /// </summary>
        public bool IsGranted { get; set; } = true;

        /// <summary>
        /// تاریخ اعطای دسترسی
        /// </summary>
        public DateTime? GrantedAt { get; set; }

        /// <summary>
        /// شناسه داخلی کاربری که این دسترسی را اعطا کرده (long)
        /// </summary>
        public long? GrantedBy { get; set; }

        /// <summary>
        /// توضیحات یا دلیل اعطا/عدم اعطای دسترسی
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Navigation Property به گروه
        /// </summary>
        public virtual tblGrp tblGrp { get; set; } = null!;

        /// <summary>
        /// Navigation Property به Permission
        /// </summary>
        public virtual tblPermission tblPermission { get; set; } = null!;
    }
}
