using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BnpCashClaudeApp.Application.DTOs
{
    /// <summary>
    /// DTO کاربر برای نمایش اطلاعات
    /// ============================================
    /// تمام تاریخ‌ها به صورت شمسی (string) هستند
    /// ============================================
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// شناسه عمومی (GUID) - برای استفاده در API
        /// </summary>
        public Guid PublicId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string MobileNumber { get; set; }
        public int? UserCode { get; set; }
        public string IpAddress { get; set; }

        // ============================================
        // فیلدهای مشتری و شعبه (Multi-tenancy)
        // ============================================

        /// <summary>
        /// شناسه مشتری
        /// </summary>
        public long? tblCustomerId { get; set; }

        /// <summary>
        /// نام مشتری (برای نمایش)
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// شناسه شعبه
        /// </summary>
        public long? tblShobeId { get; set; }

        /// <summary>
        /// نام شعبه (برای نمایش)
        /// </summary>
        public string? ShobeName { get; set; }

        // ============================================
        // فیلدهای تاریخ (به صورت شمسی - string)
        // ============================================

        /// <summary>
        /// تاریخ ایجاد رکورد (شمسی)
        /// </summary>
        public string ZamanInsert { get; set; }

        /// <summary>
        /// تاریخ آخرین ویرایش (شمسی)
        /// </summary>
        public string? ZamanLastEdit { get; set; }

        // ============================================
        // فیلدهای امنیتی (ISO 15408)
        // ============================================

        /// <summary>
        /// وضعیت فعال/غیرفعال بودن کاربر
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// تاریخ آخرین تغییر رمز عبور (شمسی)
        /// </summary>
        public string? PasswordLastChangedAt { get; set; }

        /// <summary>
        /// تاریخ آخرین ورود موفق (شمسی)
        /// </summary>
        public string? LastLoginAt { get; set; }

        /// <summary>
        /// آیا باید رمز عبور تغییر کند
        /// </summary>
        public bool MustChangePassword { get; set; }

        // ============================================
        // اطلاعات گروه کاربری
        // ============================================

        /// <summary>
        /// شناسه عمومی گروه (GUID)
        /// </summary>
        public Guid? GrpPublicId { get; set; }

        /// <summary>
        /// نام گروه کاربری
        /// </summary>
        public string? GrpTitle { get; set; }
    }

    public class CreateUserDto
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        /// <summary>
        /// شماره موبایل (الزامی) - برای بازیابی رمز عبور و MFA
        /// </summary>
        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        public string MobileNumber { get; set; }
        // UserCode به صورت خودکار تولید می‌شود - از ورودی حذف شد
        public string IpAddress { get; set; }
        // گروه انتخاب‌شده توسط کاربر
        public Guid GrpPublicId { get; set; }

        /// <summary>
        /// شناسه مشتری (اختیاری)
        /// null = کاربر سیستمی (Super Admin)
        /// </summary>
        public long? tblCustomerId { get; set; }

        /// <summary>
        /// شناسه شعبه (اختیاری)
        /// null = کاربر سطح مشتری (دسترسی به همه شعب)
        /// </summary>
        public long? tblShobeId { get; set; }
    }

    public class UpdateUserDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        /// <summary>
        /// شماره موبایل (الزامی) - برای بازیابی رمز عبور و MFA
        /// </summary>
        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        public string MobileNumber { get; set; }
        //public string IpAddress { get; set; }

        /// <summary>
        /// شناسه مشتری (اختیاری)
        /// null = کاربر سیستمی (Super Admin)
        /// </summary>
        public long? tblCustomerId { get; set; }

        /// <summary>
        /// شناسه شعبه (اختیاری)
        /// null = کاربر سطح مشتری (دسترسی به همه شعب)
        /// </summary>
        public long? tblShobeId { get; set; }

        /// <summary>
        /// شناسه عمومی گروه (اختیاری) - در صورت ارسال، گروه کاربر به‌روزرسانی می‌شود
        /// </summary>
        public Guid? GrpPublicId { get; set; }
    }

    /// <summary>
    /// DTO تغییر وضعیت کاربر
    /// </summary>
    public class SetUserStatusDto
    {
        /// <summary>
        /// وضعیت فعال/غیرفعال - true برای فعال، false برای غیرفعال
        /// </summary>
        public bool IsActive { get; set; }
    }



    /// <summary>
    /// DTO ریست رمز عبور
    /// </summary>
    public class ResetPasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
        public bool MustChangePassword { get; set; } = true;
    }
}
