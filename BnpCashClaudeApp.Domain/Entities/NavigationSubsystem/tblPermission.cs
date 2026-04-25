using BnpCashClaudeApp.Domain.Common;
using System;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Domain.Entities.NavigationSubsystem
{
    /// <summary>
    /// موجودیت Permission برای سیستم کنترل دسترسی دقیق
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF از استاندارد ISO 15408
    /// هر Permission یک دسترسی خاص را برای یک Resource و Action تعریف می‌کند
    /// ============================================
    /// </summary>
    public class tblPermission : BaseEntity
    {
        /// <summary>
        /// نام یکتای Permission
        /// فرمت: Resource.Action (مثال: Users.Create, AuditLog.Read)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// نام Resource مرتبط (مثال: Users, AuditLog, Security)
        /// </summary>
        public string Resource { get; set; } = string.Empty;

        /// <summary>
        /// نوع عملیات (Create, Read, Update, Delete, Execute, Manage)
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// توضیحات Permission به زبان فارسی
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// آیا این Permission فعال است
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// ترتیب نمایش
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// گروه‌هایی که این Permission را دارند
        /// </summary>
        public virtual ICollection<tblGrpPermission> GroupPermissions { get; set; } = new List<tblGrpPermission>();
        
        /// <summary>
        /// منوهایی که به این Permission نیاز دارند
        /// </summary>
        public virtual ICollection<tblMenuPermission> MenuPermissions { get; set; } = new List<tblMenuPermission>();
    }

    /// <summary>
    /// انواع Actions استاندارد
    /// </summary>
    public static class PermissionActions
    {
        public const string Create = "Create";
        public const string Read = "Read";
        public const string Update = "Update";
        public const string Delete = "Delete";
        public const string Execute = "Execute";
        public const string Manage = "Manage";
        public const string Export = "Export";
        public const string Import = "Import";
        public const string Approve = "Approve";
        public const string Reject = "Reject";
    }

    /// <summary>
    /// Resources استاندارد سیستم
    /// </summary>
    public static class PermissionResources
    {
        public const string Users = "Users";
        public const string Groups = "Groups";
        public const string Menus = "Menus";
        public const string AuditLog = "AuditLog";
        public const string Security = "Security";
        public const string Sessions = "Sessions";
        public const string Reports = "Reports";
        public const string Settings = "Settings";
        public const string Permissions = "Permissions";
    }
}
