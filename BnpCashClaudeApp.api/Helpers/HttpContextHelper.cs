using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace BnpCashClaudeApp.api.Helpers
{
    /// <summary>
    /// Helper برای استخراج اطلاعات از HttpContext
    /// </summary>
    public static class HttpContextHelper
    {
        /// <summary>
        /// استخراج IP Address از HttpContext
        /// </summary>
        public static string? GetIpAddress(HttpContext? httpContext)
        {
            if (httpContext == null) return null;

            // بررسی X-Forwarded-For برای Proxy/Load Balancer
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                if (ips.Length > 0)
                    return ips[0].Trim();
            }

            // بررسی X-Real-IP
            var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
                return realIp;

            // استفاده از RemoteIpAddress
            return httpContext.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// استخراج User Agent از HttpContext
        /// </summary>
        public static string? GetUserAgent(HttpContext? httpContext)
        {
            if (httpContext == null) return null;
            return httpContext.Request.Headers["User-Agent"].FirstOrDefault();
        }

        /// <summary>
        /// استخراج Operating System از User Agent
        /// </summary>
        public static string? GetOperatingSystem(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return null;

            // تشخیص سیستم عامل از User Agent
            if (userAgent.Contains("Windows NT"))
                return "Windows";
            if (userAgent.Contains("Mac OS X"))
                return "macOS";
            if (userAgent.Contains("Linux"))
                return "Linux";
            if (userAgent.Contains("Android"))
                return "Android";
            if (userAgent.Contains("iOS") || userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
                return "iOS";

            return "Unknown";
        }

        /// <summary>
        /// استخراج UserId از Claims
        /// JWT از ClaimTypes.NameIdentifier استفاده می‌کند، نه "UserId"
        /// </summary>
        public static long? GetUserId(HttpContext? httpContext)
        {
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return null;

            // اول ClaimTypes.NameIdentifier (مطابق JWT در AuthController)
            var userIdClaim = httpContext.User.Claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.NameIdentifier || c.Type == "UserId");
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
                return userId;

            return null;
        }

        /// <summary>
        /// استخراج UserName از Claims
        /// </summary>
        public static string? GetUserName(HttpContext? httpContext)
        {
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return null;

            return httpContext.User.Identity.Name;
        }
    }
}

