using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.api.Middleware
{
    /// <summary>
    /// Middleware برای افزودن Security Headers به تمام پاسخ‌ها
    /// پیاده‌سازی الزامات پروفایل حفاظتی برنامه‌های کاربردی تحت شبکه (ISO 15408)
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // ============================================
            // الزام: حفاظت از داده‌های کاربری
            // افزودن Security Headers به Response
            // ============================================

            // ============================================
            // X-Frame-Options: جلوگیری از حملات Clickjacking
            // مقدار DENY: جلوگیری از نمایش صفحه در iframe
            // ============================================
            context.Response.Headers["X-Frame-Options"] = "DENY";

            // ============================================
            // X-Content-Type-Options: جلوگیری از MIME Type Sniffing
            // مقدار nosniff: مرورگر باید از Content-Type header استفاده کند
            // ============================================
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            // ============================================
            // X-XSS-Protection: حفاظت در برابر حملات XSS
            // مقدار 1; mode=block: فیلتر XSS فعال و در صورت تشخیص، صفحه مسدود می‌شود
            // ============================================
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

            // ============================================
            // Content-Security-Policy: محدود کردن منابع قابل بارگذاری
            // default-src 'self': فقط از همان دامنه بارگذاری شود
            // script-src: محدود کردن اسکریپت‌ها
            // style-src: محدود کردن استایل‌ها
            // img-src: محدود کردن تصاویر
            // font-src: محدود کردن فونت‌ها
            // ============================================
            var cspPolicy = _configuration.GetValue<string>("SecurityHeaders:ContentSecurityPolicy")
                ?? "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' https: data:; frame-ancestors 'none';";
            context.Response.Headers["Content-Security-Policy"] = cspPolicy;

            // ============================================
            // Strict-Transport-Security (HSTS): اجبار استفاده از HTTPS
            // max-age: مدت زمان (به ثانیه) که مرورگر باید از HTTPS استفاده کند
            // includeSubDomains: شامل زیردامنه‌ها هم می‌شود
            // preload: برای قرار گرفتن در لیست HSTS Preload
            // فقط برای اتصال‌های HTTPS اعمال می‌شود
            // ============================================
            if (context.Request.IsHttps)
            {
                var hstsMaxAge = _configuration.GetValue<int>("SecurityHeaders:HstsMaxAge", 31536000); // 1 سال به ثانیه
                var includeSubDomains = _configuration.GetValue<bool>("SecurityHeaders:HstsIncludeSubDomains", true);
                var preload = _configuration.GetValue<bool>("SecurityHeaders:HstsPreload", true);

                var hstsValue = $"max-age={hstsMaxAge}";
                if (includeSubDomains)
                    hstsValue += "; includeSubDomains";
                if (preload)
                    hstsValue += "; preload";

                context.Response.Headers["Strict-Transport-Security"] = hstsValue;
            }

            // ============================================
            // Referrer-Policy: کنترل اطلاعات Referrer
            // strict-origin-when-cross-origin: ارسال origin برای درخواست‌های cross-origin HTTPS
            // ============================================
            var referrerPolicy = _configuration.GetValue<string>("SecurityHeaders:ReferrerPolicy")
                ?? "strict-origin-when-cross-origin";
            context.Response.Headers["Referrer-Policy"] = referrerPolicy;

            // ============================================
            // Permissions-Policy (جایگزین Feature-Policy): کنترل دسترسی به قابلیت‌های مرورگر
            // geolocation=(): غیرفعال کردن دسترسی به موقعیت مکانی
            // microphone=(): غیرفعال کردن دسترسی به میکروفون
            // camera=(): غیرفعال کردن دسترسی به دوربین
            // ============================================
            var permissionsPolicy = _configuration.GetValue<string>("SecurityHeaders:PermissionsPolicy")
                ?? "geolocation=(), microphone=(), camera=(), payment=(), usb=(), magnetometer=(), gyroscope=(), accelerometer=()";
            context.Response.Headers["Permissions-Policy"] = permissionsPolicy;

            // ============================================
            // X-Permitted-Cross-Domain-Policies: کنترل دسترسی Flash/PDF
            // none: جلوگیری از خواندن داده‌ها توسط Flash و PDF
            // ============================================
            context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";

            // ============================================
            // X-Download-Options: جلوگیری از باز کردن فایل در Internet Explorer
            // noopen: فایل‌ها باید ذخیره شوند، نه باز شوند
            // ============================================
            context.Response.Headers["X-Download-Options"] = "noopen";

            // ============================================
            // Cache-Control: کنترل caching برای صفحات حساس
            // برای API endpoints، معمولاً caching غیرفعال می‌شود
            // ============================================
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // برای API: no-store برای جلوگیری از cache کردن اطلاعات حساس
                if (!context.Response.Headers.ContainsKey("Cache-Control"))
                {
                    context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, proxy-revalidate";
                    context.Response.Headers["Pragma"] = "no-cache";
                    context.Response.Headers["Expires"] = "0";
                }
            }

            // ============================================
            // حذف هدرهای غیرضروری که اطلاعات سرور را فاش می‌کنند
            // ============================================
            context.Response.OnStarting(() =>
            {
                // حذف Server header
                context.Response.Headers.Remove("Server");
                
                // حذف X-Powered-By header
                context.Response.Headers.Remove("X-Powered-By");
                
                // حذف X-AspNet-Version header
                context.Response.Headers.Remove("X-AspNet-Version");
                
                // حذف X-AspNetMvc-Version header
                context.Response.Headers.Remove("X-AspNetMvc-Version");
                
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}

