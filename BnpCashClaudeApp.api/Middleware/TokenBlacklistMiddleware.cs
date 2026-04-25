using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.api.Middleware
{
    /// <summary>
    /// Middleware برای بررسی Blacklist توکن‌های JWT
    /// پیاده‌سازی الزامات پروفایل حفاظتی برنامه‌های کاربردی تحت شبکه (ISO 15408)
    /// </summary>
    public class TokenBlacklistMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenBlacklistMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// توجه: ITokenBlacklistService از HttpContext.RequestServices دریافت می‌شود
        /// چون سرویس Scoped است و نمی‌توان آن را در constructor تزریق کرد
        /// </summary>
        public async Task InvokeAsync(HttpContext context, ITokenBlacklistService tokenBlacklistService)
        {
            // ============================================
            // بررسی وجود توکن در Header
            // ============================================
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();

                // ============================================
                // بررسی Blacklist بودن توکن
                // ============================================
                var isBlacklisted = await tokenBlacklistService.IsTokenBlacklistedAsync(token);
                
                if (isBlacklisted)
                {
                    // ============================================
                    // برگرداندن 401 با پیام مناسب
                    // ============================================
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"message\": \"توکن نامعتبر است. لطفاً مجدداً وارد شوید.\", \"code\": \"TOKEN_BLACKLISTED\"}");
                    return;
                }
            }

            // ============================================
            // ادامه Pipeline
            // ============================================
            await _next(context);
        }
    }

    /// <summary>
    /// Extension Method برای ثبت Middleware
    /// </summary>
    public static class TokenBlacklistMiddlewareExtensions
    {
        /// <summary>
        /// استفاده از Middleware بررسی Blacklist توکن
        /// </summary>
        public static IApplicationBuilder UseTokenBlacklist(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenBlacklistMiddleware>();
        }
    }
}
