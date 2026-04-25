using BnpCashClaudeApp.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace BnpCashClaudeApp.Domain.Entities.AuditLogSubsystem
{
    /// <summary>
    /// جدول مستر که اطلاعات کلی از یک رویداد را نگه می‌دارد
    /// </summary>
    public class AuditLogMaster : BaseEntity
    {
        /// <summary>
        /// تاریخ و زمان رویداد
        /// </summary>
        public DateTime EventDateTime { get; set; }

        /// <summary>
        /// نوع رویداد (مثلاً: Create, Update, Delete, Login, Logout, etc.)
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// هویت موجودیت فعال (در صورتی که کاربرد داشته باشد)
        /// مثلاً: UserId, MenuId, etc.
        /// </summary>
        public string? EntityId { get; set; }

        /// <summary>
        /// نوع موجودیت (مثلاً: User, Menu, Grp, etc.)
        /// </summary>
        public string? EntityType { get; set; }

        /// <summary>
        /// نتیجه رویداد (موفقیت یا شکست)
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// پیام خطا در صورت شکست
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// آدرس IP کاربر
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// نام کاربری
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// شناسه داخلی کاربری (long)
        /// </summary>
        public long? UserId { get; set; }

        /// <summary>
        /// نسخه سیستم عامل
        /// </summary>
        public string? OperatingSystem { get; set; }

        /// <summary>
        /// مرورگر یا کلاینت
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// توضیحات اضافی
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// ارتباط با جدول جزئیات
        /// </summary>
        public virtual ICollection<AuditLogDetail> Details { get; set; }

        public AuditLogMaster()
        {
            Details = new List<AuditLogDetail>();
        }
    }
}

