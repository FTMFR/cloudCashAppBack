using BnpCashClaudeApp.Application.Interfaces;
using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// پیاده‌سازی PasswordHasher با استفاده از BCrypt
    /// BCrypt یک الگوریتم Hash امن است که به صورت خودکار Salt اضافه می‌کند
    /// 
    /// پیاده‌سازی الزام FDP_RIP.2 - پاکسازی اطلاعات باقیمانده
    /// رمز عبور پس از Hash/Verify از حافظه پاک می‌شود
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        private readonly ISecureMemoryService _secureMemoryService;

        public PasswordHasher(ISecureMemoryService secureMemoryService)
        {
            _secureMemoryService = secureMemoryService;
        }

        /// <summary>
        /// Hash کردن پسورد با استفاده از BCrypt
        /// BCrypt به صورت خودکار Salt اضافه می‌کند
        /// 
        /// توجه: رمز عبور اصلی پس از Hash کردن از حافظه پاک می‌شود (FDP_RIP.2)
        /// </summary>
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            try
            {
                // استفاده از BCrypt که به صورت خودکار Salt اضافه می‌کند
                // workFactor = 12 (تعداد دورهای Hash - هرچه بیشتر باشد امن‌تر اما کندتر)
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
                
                // ============================================
                // پاکسازی رمز عبور اصلی از حافظه (FDP_RIP.2)
                // ============================================
                var passwordCopy = password; // کپی برای پاکسازی
                _secureMemoryService.ClearString(ref passwordCopy);
                
                return hashedPassword;
            }
            catch
            {
                // در صورت خطا، رمز عبور را پاک می‌کنیم
                var passwordCopy = password;
                _secureMemoryService.ClearString(ref passwordCopy);
                throw;
            }
        }

        /// <summary>
        /// بررسی صحت پسورد با Hash ذخیره شده
        /// 
        /// توجه: رمز عبور ارائه شده پس از Verify از حافظه پاک می‌شود (FDP_RIP.2)
        /// </summary>
        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            if (string.IsNullOrWhiteSpace(providedPassword))
                return false;

            try
            {
                // بررسی صحت پسورد با استفاده از BCrypt
                var result = BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
                
                // ============================================
                // پاکسازی رمز عبور ارائه شده از حافظه (FDP_RIP.2)
                // ============================================
                var passwordCopy = providedPassword;
                _secureMemoryService.ClearString(ref passwordCopy);
                
                return result;
            }
            catch
            {
                // در صورت خطا، رمز عبور را پاک می‌کنیم
                var passwordCopy = providedPassword;
                _secureMemoryService.ClearString(ref passwordCopy);
                
                // در صورت خطا (مثلاً فرمت Hash نامعتبر)، false برمی‌گردانیم
                return false;
            }
        }

        /// <summary>
        /// Hash کردن پسورد با استفاده از SecureString
        /// این متد برای امنیت بیشتر استفاده می‌شود
        /// </summary>
        public string HashPasswordSecure(System.Security.SecureString securePassword)
        {
            if (securePassword == null || securePassword.Length == 0)
                throw new ArgumentException("Password cannot be null or empty", nameof(securePassword));

            try
            {
                // تبدیل SecureString به رشته موقت
                var password = _secureMemoryService.ConvertFromSecureString(securePassword);
                
                try
                {
                    // Hash کردن
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
                    
                    // پاکسازی رشته موقت
                    _secureMemoryService.ClearString(ref password);
                    
                    return hashedPassword;
                }
                finally
                {
                    // اطمینان از پاکسازی
                    _secureMemoryService.ClearString(ref password);
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// بررسی صحت پسورد با استفاده از SecureString
        /// </summary>
        public bool VerifyPasswordSecure(string hashedPassword, System.Security.SecureString securePassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            if (securePassword == null || securePassword.Length == 0)
                return false;

            try
            {
                // تبدیل SecureString به رشته موقت
                var password = _secureMemoryService.ConvertFromSecureString(securePassword);
                
                try
                {
                    // بررسی صحت
                    var result = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
                    
                    // پاکسازی رشته موقت
                    _secureMemoryService.ClearString(ref password);
                    
                    return result;
                }
                finally
                {
                    // اطمینان از پاکسازی
                    _secureMemoryService.ClearString(ref password);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

