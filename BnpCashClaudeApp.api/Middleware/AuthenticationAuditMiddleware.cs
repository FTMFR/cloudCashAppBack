using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.api.Extensions;
using BnpCashClaudeApp.api.Helpers;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.api.Middleware
{
    /// <summary>
    /// Middleware برای ثبت خودکار رویدادهای دسترسی به endpointها
    /// همه درخواست‌های احراز هویت‌شده (GET, POST, PUT, DELETE و ...) لاگ می‌شوند
    /// </summary>
    public class AuthenticationAuditMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationAuditMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
        {
            // اگر مسیر Login است، از Middleware رد می‌شود (در Controller ثبت می‌شود)
            if (context.Request.Path.StartsWithSegments("/api/Auth/login", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // ثبت لاگ برای همه endpointهای احراز هویت‌شده (GET, POST, PUT, DELETE و ...)
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.GetUserId();
                var userName = context.User.GetUserName();
                var ipAddress = HttpContextHelper.GetIpAddress(context);
                var userAgent = HttpContextHelper.GetUserAgent(context);
                var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
                var method = context.Request.Method;
                var path = context.Request.Path.Value ?? "";

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await auditLogService.LogEventAsync(
                            eventType: "Authentication",
                            entityType: "User",
                            entityId: userId?.ToString(),
                            isSuccess: true,
                            ipAddress: ipAddress,
                            userName: userName,
                            userId: userId,
                            operatingSystem: operatingSystem,
                            userAgent: userAgent,
                            description: $"{method} {path}",
                            ct: default);
                    }
                    catch
                    {
                        // در صورت خطا، لاگ نمی‌کنیم تا بر عملکرد تأثیر نگذارد
                    }
                });
            }

            await _next(context);
        }
    }
}

