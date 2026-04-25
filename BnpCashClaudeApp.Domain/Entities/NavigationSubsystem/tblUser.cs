using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.ManagementSubsystem;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BnpCashClaudeApp.Domain.Entities.NavigationSubsystem
{
    /// <summary>
    /// موجودیت کاربر
    /// ============================================
    /// پیاده‌سازی الزامات امنیتی پروفایل حفاظتی (ISO 15408)
    /// تاریخ‌ها به صورت شمسی ذخیره می‌شوند
    /// ============================================
    /// </summary>
    public class tblUser : BaseEntity
    {
        // ============================================
        // روابط با مشتری و شعبه (Multi-tenancy)
        // ============================================

        /// <summary>
        /// شناسه مشتری (FK) - اختیاری
        /// هر کاربر متعلق به یک مشتری است
        /// null = کاربر سیستمی (Super Admin)
        /// </summary>
        public long? tblCustomerId { get; set; }

        /// <summary>
        /// شناسه شعبه (FK) - اختیاری
        /// هر کاربر می‌تواند به یک شعبه خاص تعلق داشته باشد
        /// null = کاربر سطح مشتری (دسترسی به همه شعب مشتری)
        /// </summary>
        public long? tblShobeId { get; set; }

        public string UserName { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string MobileNumber { get; set; }
        public int? UserCode { get; set; }
        public string IpAddress { get; set; }

        // ============================================
        // فیلدهای امنیتی اضافه شده (ISO 15408)
        // ============================================

        /// <summary>
        /// وضعیت فعال/غیرفعال بودن کاربر
        /// الزام: امکان غیرفعال کردن حساب کاربری
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// تاریخ آخرین تغییر رمز عبور (شمسی)
        /// الزام: بررسی انقضای رمز عبور
        /// </summary>
        public string? PasswordLastChangedAt { get; set; }

        /// <summary>
        /// تاریخ آخرین ورود موفق (شمسی)
        /// الزام: نمایش اطلاعات آخرین ورود به کاربر
        /// </summary>
        public string? LastLoginAt { get; set; }

        /// <summary>
        /// آیا باید رمز عبور تغییر کند (Force Password Change)
        /// الزام: اجبار به تغییر رمز عبور در اولین ورود
        /// </summary>
        public bool MustChangePassword { get; set; } = false;

        // ============================================
        // فیلدهای MFA (FIA_UAU.5 - احرازهویت چندگانه)
        // ============================================

        /// <summary>
        /// آیا MFA برای این کاربر فعال است
        /// </summary>
        public bool IsMfaEnabled { get; set; } = false;

        /// <summary>
        /// کلید مخفی TOTP (رمزنگاری شده - Base64)
        /// </summary>
        public string? MfaSecretKey { get; set; }

        /// <summary>
        /// کدهای بازیابی MFA (هش شده، جداشده با ;)
        /// </summary>
        public string? MfaRecoveryCodes { get; set; }

        /// <summary>
        /// تاریخ فعال‌سازی MFA (شمسی)
        /// </summary>
        public string? MfaEnabledAt { get; set; }

        /// <summary>
        /// تاریخ آخرین استفاده از MFA (شمسی)
        /// </summary>
        public string? MfaLastUsedAt { get; set; }

        // ============================================
        // تصویر پروفایل
        // ============================================

        /// <summary>
        /// شناسه تصویر پروفایل (PublicId از tblAttachment)
        /// </summary>
        public Guid? ProfilePictureId { get; set; }

        /// <summary>
        /// تنظیم تاریخ آخرین تغییر رمز عبور از DateTime میلادی
        /// </summary>
        public void SetPasswordLastChangedAt(DateTime dateTime)
        {
            PasswordLastChangedAt = BaseEntity.ToPersianDateTime(dateTime);
        }

        /// <summary>
        /// تنظیم تاریخ آخرین ورود از DateTime میلادی
        /// </summary>
        public void SetLastLoginAt(DateTime dateTime)
        {
            LastLoginAt = BaseEntity.ToPersianDateTime(dateTime);
        }

        /// <summary>
        /// تنظیم تاریخ فعال‌سازی MFA
        /// </summary>
        public void SetMfaEnabledAt(DateTime dateTime)
        {
            MfaEnabledAt = BaseEntity.ToPersianDateTime(dateTime);
        }

        /// <summary>
        /// تنظیم تاریخ آخرین استفاده از MFA
        /// </summary>
        public void SetMfaLastUsedAt(DateTime dateTime)
        {
            MfaLastUsedAt = BaseEntity.ToPersianDateTime(dateTime);
        }

        // ============================================
        // Navigation Properties
        // ============================================

        /// <summary>
        /// مشتری مالک این کاربر
        /// </summary>
        [ForeignKey(nameof(tblCustomerId))]
        public virtual tblCustomer? Customer { get; set; }

        /// <summary>
        /// شعبه‌ای که کاربر در آن کار می‌کند
        /// </summary>
        [ForeignKey(nameof(tblShobeId))]
        public virtual tblShobe? Shobe { get; set; }
    }
}
