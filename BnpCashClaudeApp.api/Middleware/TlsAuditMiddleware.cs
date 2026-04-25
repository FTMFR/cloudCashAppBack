using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.api.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.api.Middleware
{
    /// <summary>
    /// Middleware برای ثبت رویدادهای TLS/HTTPS در Audit Log
    /// پیاده‌سازی الزامات پروفایل حفاظتی برنامه‌های کاربردی تحت شبکه (ISO 15408)
    /// </summary>
    public class TlsAuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TlsAuditMiddleware> _logger;

        public TlsAuditMiddleware(RequestDelegate next, ILogger<TlsAuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
        {
            // ============================================
            // ثبت اطلاعات اتصال TLS/HTTPS
            // ============================================
            try
            {
                if (context.Request.IsHttps)
                {
                    var ipAddress = HttpContextHelper.GetIpAddress(context);
                    var connectionFeature = context.Features.Get<ITlsConnectionFeature>();

                    // ============================================
                    // ثبت اطلاعات TLS Connection
                    // ============================================
                    string? tlsProtocolVersion = context.Request.Protocol;

                    _logger.LogDebug(
                        "TLS Connection: Protocol={Protocol}, IP={IP}",
                        tlsProtocolVersion,
                        ipAddress);

                    // ============================================
                    // بررسی گواهینامه کلاینت (اگر موجود باشد)
                    // ============================================
                    if (connectionFeature != null)
                    {
                        try
                        {
                            var clientCertificate = await connectionFeature.GetClientCertificateAsync(context.RequestAborted);
                            
                            if (clientCertificate != null)
                            {
                                // ============================================
                                // ثبت رویداد گواهینامه کلاینت در Audit Log
                                // ============================================
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        await auditLogService.LogEventAsync(
                                            eventType: "TlsClientCertificate",
                                            entityType: "X509Certificate",
                                            entityId: clientCertificate.Thumbprint,
                                            isSuccess: true,
                                            ipAddress: ipAddress,
                                            description: $"گواهینامه کلاینت دریافت شد: {clientCertificate.Subject}, " +
                                                        $"صادرکننده: {clientCertificate.Issuer}, " +
                                                        $"انقضا: {clientCertificate.NotAfter:yyyy-MM-dd}",
                                            ct: default);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "خطا در ثبت Audit Log برای گواهینامه کلاینت");
                                    }
                                });

                                _logger.LogInformation(
                                    "Client Certificate: Subject={Subject}, Thumbprint={Thumbprint}, Expiry={Expiry}",
                                    clientCertificate.Subject,
                                    clientCertificate.Thumbprint,
                                    clientCertificate.NotAfter);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "گواهینامه کلاینت در دسترس نیست");
                        }
                    }

                    // ============================================
                    // ثبت رویداد اتصال TLS موفق در Audit Log
                    // فقط برای درخواست‌های خاص ثبت می‌شود (نه همه درخواست‌ها)
                    // ============================================
                    if (ShouldLogTlsEvent(context))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await auditLogService.LogEventAsync(
                                    eventType: "TlsConnection",
                                    entityType: "Connection",
                                    isSuccess: true,
                                    ipAddress: ipAddress,
                                    description: $"اتصال TLS/HTTPS برقرار شد - " +
                                                $"مسیر: {context.Request.Path}, " +
                                                $"پروتکل: {tlsProtocolVersion ?? "نامشخص"}",
                                    ct: default);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "خطا در ثبت Audit Log برای اتصال TLS");
                            }
                        });
                    }
                }
                else
                {
                    // ============================================
                    // ثبت هشدار برای اتصال‌های غیر-HTTPS
                    // ============================================
                    var ipAddress = HttpContextHelper.GetIpAddress(context);
                    
                    _logger.LogWarning(
                        "Non-HTTPS connection detected: IP={IP}, Path={Path}",
                        ipAddress,
                        context.Request.Path);

                    // ثبت در Audit Log برای اتصال‌های حساس غیر-HTTPS
                    if (IsSensitivePath(context.Request.Path))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await auditLogService.LogEventAsync(
                                    eventType: "InsecureConnection",
                                    entityType: "Connection",
                                    isSuccess: false,
                                    errorMessage: "اتصال HTTP غیرامن برای مسیر حساس",
                                    ipAddress: ipAddress,
                                    description: $"اتصال HTTP (غیرامن) شناسایی شد - مسیر: {context.Request.Path}",
                                    ct: default);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "خطا در ثبت Audit Log برای اتصال غیرامن");
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در TlsAuditMiddleware");
            }

            await _next(context);
        }

        /// <summary>
        /// تعیین اینکه آیا باید رویداد TLS ثبت شود
        /// برای جلوگیری از ثبت بیش از حد، فقط برای مسیرهای خاص ثبت می‌شود
        /// </summary>
        private bool ShouldLogTlsEvent(HttpContext context)
        {
            // فقط برای مسیرهای حساس ثبت کن
            var path = context.Request.Path.Value?.ToLower() ?? "";

            return path.Contains("/auth/login") ||
                   path.Contains("/api/users") ||
                   path.Contains("/api/seed");
        }

        /// <summary>
        /// تعیین مسیرهای حساس که نباید از HTTP استفاده کنند
        /// </summary>
        private bool IsSensitivePath(PathString path)
        {
            var pathValue = path.Value?.ToLower() ?? "";

            return pathValue.Contains("/auth") ||
                   pathValue.Contains("/login") ||
                   pathValue.Contains("/users") ||
                   pathValue.Contains("/api/");
        }
    }
}
