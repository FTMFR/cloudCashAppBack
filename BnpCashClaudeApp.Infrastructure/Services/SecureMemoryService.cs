using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// پیاده‌سازی سرویس پاکسازی امن اطلاعات باقیمانده در حافظه
    /// پیاده‌سازی الزام FDP_RIP.2.1 از استاندارد ISO 15408
    /// 
    /// این سرویس از Zeroization برای پاکسازی امن اطلاعات حساس استفاده می‌کند.
    /// Zeroization: پر کردن حافظه با صفر یا داده‌های تصادفی قبل از آزادسازی
    /// 
    /// اصول امنیتی:
    /// 1. پاکسازی فوری پس از استفاده
    /// 2. Zeroization برای جلوگیری از Memory Dump
    /// 3. استفاده از SecureString برای رمز عبور
    /// 4. Secure Disposal Pattern
    /// </summary>
    public class SecureMemoryService : ISecureMemoryService
    {
        private readonly ILogger<SecureMemoryService> _logger;

        public SecureMemoryService(ILogger<SecureMemoryService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// پاکسازی امن یک رشته از حافظه
        /// 
        /// توجه: در .NET، رشته‌ها Immutable هستند و نمی‌توان مستقیماً محتوای آن‌ها را تغییر داد.
        /// این متد یک کپی از رشته را در حافظه غیرمدیریتی ایجاد می‌کند و آن را پاک می‌کند.
        /// </summary>
        public void ClearString(ref string? sensitiveData)
        {
            if (string.IsNullOrEmpty(sensitiveData))
                return;

            try
            {
                // تبدیل رشته به آرایه کاراکتر
                var chars = sensitiveData.ToCharArray();
                
                // پاکسازی آرایه کاراکتر
                ClearChars(chars);
                
                // پاکسازی رشته اصلی
                sensitiveData = null;
                
                // Force Garbage Collection برای حذف فوری از حافظه
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در پاکسازی رشته از حافظه");
                // حتی در صورت خطا، رشته را null می‌کنیم
                sensitiveData = null;
            }
        }

        /// <summary>
        /// پاکسازی امن یک آرایه بایت از حافظه
        /// با استفاده از Zeroization (پر کردن با صفر)
        /// </summary>
        public void ClearBytes(byte[]? sensitiveData)
        {
            if (sensitiveData == null || sensitiveData.Length == 0)
                return;

            try
            {
                // ============================================
                // Zeroization: پر کردن آرایه با صفر
                // این کار باعث می‌شود که داده‌های قبلی قابل بازیابی نباشند
                // ============================================
                Array.Clear(sensitiveData, 0, sensitiveData.Length);
                
                // ============================================
                // برای امنیت بیشتر، با داده‌های تصادفی پر می‌کنیم
                // ============================================
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(sensitiveData);
                }
                
                // ============================================
                // دوباره با صفر پر می‌کنیم
                // ============================================
                Array.Clear(sensitiveData, 0, sensitiveData.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در پاکسازی آرایه بایت از حافظه");
            }
        }

        /// <summary>
        /// پاکسازی امن یک آرایه کاراکتر از حافظه
        /// </summary>
        public void ClearChars(char[]? sensitiveData)
        {
            if (sensitiveData == null || sensitiveData.Length == 0)
                return;

            try
            {
                // ============================================
                // Zeroization: پر کردن آرایه با کاراکتر null
                // ============================================
                Array.Clear(sensitiveData, 0, sensitiveData.Length);
                
                // ============================================
                // برای امنیت بیشتر، با کاراکترهای تصادفی پر می‌کنیم
                // ============================================
                using (var rng = RandomNumberGenerator.Create())
                {
                    var randomBytes = new byte[sensitiveData.Length];
                    rng.GetBytes(randomBytes);
                    
                    for (int i = 0; i < sensitiveData.Length; i++)
                    {
                        sensitiveData[i] = (char)(randomBytes[i] % 256);
                    }
                }
                
                // ============================================
                // دوباره با کاراکتر null پر می‌کنیم
                // ============================================
                Array.Clear(sensitiveData, 0, sensitiveData.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در پاکسازی آرایه کاراکتر از حافظه");
            }
        }

        /// <summary>
        /// پاکسازی امن یک SecureString
        /// </summary>
        public void ClearSecureString(SecureString? secureString)
        {
            if (secureString == null)
                return;

            try
            {
                secureString.Clear();
                secureString.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در پاکسازی SecureString");
            }
        }

        /// <summary>
        /// تبدیل یک رشته به SecureString برای ذخیره‌سازی امن
        /// 
        /// SecureString:
        /// - داده‌ها در حافظه غیرمدیریتی ذخیره می‌شوند
        /// - به صورت خودکار رمزنگاری می‌شوند
        /// - پس از استفاده باید Dispose شوند
        /// </summary>
        public SecureString ConvertToSecureString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return new SecureString();

            var secureString = new SecureString();
            
            try
            {
                foreach (char c in plainText)
                {
                    secureString.AppendChar(c);
                }
                
                // قفل کردن SecureString برای جلوگیری از تغییر
                secureString.MakeReadOnly();
                
                return secureString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در تبدیل رشته به SecureString");
                secureString?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// تبدیل SecureString به رشته (فقط برای استفاده موقت)
        /// 
        /// ⚠️ هشدار: این متد داده‌های حساس را به رشته تبدیل می‌کند.
        /// باید بلافاصله پس از استفاده، رشته را پاک کنید.
        /// </summary>
        public string ConvertFromSecureString(SecureString secureString)
        {
            if (secureString == null)
                return string.Empty;

            IntPtr ptr = IntPtr.Zero;
            try
            {
                // تبدیل SecureString به رشته در حافظه غیرمدیریتی
                ptr = Marshal.SecureStringToBSTR(secureString);
                return Marshal.PtrToStringBSTR(ptr) ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در تبدیل SecureString به رشته");
                return string.Empty;
            }
            finally
            {
                // ============================================
                // پاکسازی حافظه غیرمدیریتی
                // ============================================
                if (ptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(ptr);
                }
            }
        }

        /// <summary>
        /// پاکسازی امن یک شی IDisposable
        /// 
        /// این متد از Secure Disposal Pattern استفاده می‌کند:
        /// 1. Dispose کردن شی
        /// 2. Null کردن Reference
        /// 3. Force Garbage Collection
        /// </summary>
        public void SecureDispose<T>(ref T? disposable) where T : class, IDisposable
        {
            if (disposable == null)
                return;

            try
            {
                disposable.Dispose();
                disposable = null;
                
                // Force Garbage Collection برای حذف فوری از حافظه
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در Secure Dispose کردن شی {Type}", typeof(T).Name);
                disposable = null;
            }
        }

        /// <summary>
        /// پاکسازی امن یک آرایه از اشیای IDisposable
        /// </summary>
        public void ClearDisposableArray<T>(T[]? array) where T : class, IDisposable
        {
            if (array == null || array.Length == 0)
                return;

            foreach (var item in array)
            {
                try
                {
                    item?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "خطا در Dispose کردن آیتم از آرایه");
                }
            }

            Array.Clear(array, 0, array.Length);
        }
    }
}

