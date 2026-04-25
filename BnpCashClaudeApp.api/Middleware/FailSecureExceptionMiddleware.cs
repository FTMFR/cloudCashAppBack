using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BnpCashClaudeApp.Application.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.api.Middleware
{
    /// <summary>
    /// Middleware برای مدیریت استثناهای مدیریت نشده با رویکرد Fail-Secure
    /// ============================================
    /// پیاده‌سازی الزام FPT_FLS.1.1 (الزام 46)
    /// حفظ وضعیت امن در زمان شکست نرم‌افزاری
    /// ============================================
    /// 
    /// این Middleware تضمین می‌کند که:
    /// 1. در صورت بروز Exception مدیریت نشده، سیستم به حالت امن می‌رود
    /// 2. هیچ اطلاعات فنی (Stack Trace, Exception Details) به کاربر افشا نمی‌شود
    /// 3. تمام رویدادهای شکست در Audit Log ثبت می‌شوند
    /// 4. صحت داده‌ها و خط‌مشی کنترل دسترسی حفظ می‌شود
    /// </summary>
    public class FailSecureExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<FailSecureExceptionMiddleware> _logger;

        public FailSecureExceptionMiddleware(
            RequestDelegate next,
            ILogger<FailSecureExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService, IFailSecureService failSecureService)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException)
            {
                // درخواست توسط کاربر لغو شده - این یک شکست امنیتی نیست
                _logger.LogInformation("Request cancelled by client: {Path}", context.Request.Path);
                
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = 499; // Client Closed Request
                }
            }
            catch (FluentValidation.ValidationException validationEx)
            {
                // ============================================
                // مدیریت خطاهای اعتبارسنجی FluentValidation
                // این خطاها از MediatR Pipeline می‌آیند و
                // باید به صورت 400 Bad Request برگردانده شوند
                // ============================================
                await HandleValidationExceptionAsync(context, validationEx);
            }
            catch (ArgumentException argEx)
            {
                // ============================================
                // مدیریت خطاهای اعتبارسنجی سطح Handler
                // ============================================
                await HandleArgumentExceptionAsync(context, argEx);
            }
            catch (Exception ex)
            {
                await HandleFailSecureAsync(context, ex, auditLogService, failSecureService);
            }
        }

        /// <summary>
        /// مدیریت خطاهای اعتبارسنجی FluentValidation
        /// پاسخ: 400 Bad Request با لیست خطاها
        /// </summary>
        private async Task HandleValidationExceptionAsync(HttpContext context, FluentValidation.ValidationException ex)
        {
            _logger.LogWarning(
                "Validation failed for {Path}: {Errors}",
                context.Request.Path,
                string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)));

            if (context.Response.HasStarted) return;

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json; charset=utf-8";

            var response = new
            {
                success = false,
                message = "اطلاعات وارد شده معتبر نیست",
                errors = ex.Errors.Select(e => new
                {
                    field = e.PropertyName,
                    message = e.ErrorMessage
                })
            };

            await context.Response.WriteAsJsonAsync(response);
        }

        /// <summary>
        /// مدیریت خطاهای ArgumentException از Handler ها
        /// پاسخ: 400 Bad Request
        /// </summary>
        private async Task HandleArgumentExceptionAsync(HttpContext context, ArgumentException ex)
        {
            _logger.LogWarning(
                "Argument validation failed for {Path}: {Message}",
                context.Request.Path,
                ex.Message);

            if (context.Response.HasStarted) return;

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json; charset=utf-8";

            var response = new
            {
                success = false,
                message = ex.Message
            };

            await context.Response.WriteAsJsonAsync(response);
        }

        /// <summary>
        /// مدیریت Fail-Secure برای استثناهای مدیریت نشده
        /// </summary>
        private async Task HandleFailSecureAsync(HttpContext context, Exception ex, IAuditLogService auditLogService, IFailSecureService failSecureService)
        {
            // ============================================
            // FPT_FLS.1.1: Fail-Secure State
            // شکست نرم‌افزاری - سیستم به حالت امن می‌رود
            // ============================================

            // تعیین نوع شکست
            var failureType = DetermineFailureType(ex);

            _logger.LogCritical(ex,
                "FAIL-SECURE ACTIVATED: {FailureType} | Path: {Path} | Method: {Method} | Exception: {ExceptionType}",
                failureType,
                context.Request.Path,
                context.Request.Method,
                ex.GetType().Name);

            // ثبت در Audit Log (یا فایل اگر دیتابیس قطع است)
            await LogSecurityFailureAsync(context, ex, failureType, auditLogService, failSecureService);

            // پاسخ امن - بدون افشای اطلاعات داخلی
            await SendSecureResponseAsync(context, failureType);
        }

        /// <summary>
        /// تعیین نوع شکست بر اساس نوع Exception
        /// </summary>
        private string DetermineFailureType(Exception ex)
        {
            return ex switch
            {
                // شکست‌های مرتبط با پایگاه داده
                Microsoft.Data.SqlClient.SqlException => "DatabaseConnectionFailure",
                InvalidOperationException ioe when ioe.Message.Contains("database") => "DatabaseOperationFailure",
                
                // شکست‌های مرتبط با حافظه
                OutOfMemoryException => "MemoryExhaustion",
                StackOverflowException => "StackOverflow",
                
                // شکست‌های مرتبط با فایل
                System.IO.IOException => "FileSystemFailure",
                
                // شکست‌های مرتبط با شبکه
                System.Net.Http.HttpRequestException => "NetworkFailure",
                TimeoutException => "OperationTimeout",
                
                // شکست‌های مرتبط با احراز هویت
                UnauthorizedAccessException => "AuthorizationFailure",
                
                // سایر شکست‌ها
                _ => "UnhandledSystemFailure"
            };
        }

        /// <summary>
        /// ثبت رویداد شکست در Audit Log (یا فایل اگر دیتابیس قطع است)
        /// </summary>
        private async Task LogSecurityFailureAsync(
            HttpContext context,
            Exception ex,
            string failureType,
            IAuditLogService auditLogService,
            IFailSecureService failSecureService)
        {
            try
            {
                var userId = GetUserIdFromContext(context);
                var userName = context.User?.Identity?.Name;
                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                var userAgent = context.Request.Headers["User-Agent"].ToString();

                await auditLogService.LogEventAsync(
                    eventType: "SystemFailure",
                    entityType: "System",
                    entityId: failureType,
                    isSuccess: false,
                    errorMessage: $"FAIL-SECURE: {failureType} - {ex.GetType().Name}",
                    ipAddress: ipAddress,
                    userName: userName,
                    userId: userId,
                    userAgent: userAgent,
                    description: $"FAIL-SECURE activated | Path: {context.Request.Path} | Method: {context.Request.Method} | FailureType: {failureType}",
                    ct: default);
            }
            catch (Exception logEx)
            {
                // ============================================
                // دیتابیس قطع است - ذخیره در فایل
                // ============================================
                _logger.LogError(logEx, "Database unavailable. Logging failure to file. Original failure: {FailureType}", failureType);
                
                try
                {
                    await failSecureService.LogFailureToFileAsync(
                        failureType: failureType,
                        operationName: $"{context.Request.Method} {context.Request.Path}",
                        details: $"Exception: {ex.GetType().Name}, User: {context.User?.Identity?.Name ?? "Anonymous"}, IP: {context.Connection.RemoteIpAddress}");
                }
                catch (Exception fileEx)
                {
                    _logger.LogError(fileEx, "Failed to log failure to file as well.");
                }
            }
        }

        /// <summary>
        /// ارسال پاسخ امن بدون افشای اطلاعات داخلی
        /// </summary>
        private async Task SendSecureResponseAsync(HttpContext context, string failureType)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started. Cannot send fail-secure response.");
                return;
            }

            // تعیین Status Code بر اساس نوع شکست
            var statusCode = failureType switch
            {
                "AuthorizationFailure" => StatusCodes.Status403Forbidden,
                "OperationTimeout" => StatusCodes.Status504GatewayTimeout,
                "DatabaseConnectionFailure" => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";

            // پیام امن بدون افشای جزئیات فنی
            var response = new
            {
                success = false,
                error = GetSecureErrorMessage(failureType),
                message = "درخواست شما قابل پردازش نیست. لطفاً بعداً تلاش کنید.",
                // ============================================
                // هیچ اطلاعات فنی افشا نمی‌شود:
                // - Stack Trace
                // - Exception Message
                // - Inner Exception
                // - Database Details
                // - Server Configuration
                // ============================================
                timestamp = DateTime.UtcNow,
                requestId = context.TraceIdentifier
            };

            await context.Response.WriteAsJsonAsync(response);
        }

        /// <summary>
        /// دریافت پیام خطای امن بدون افشای اطلاعات داخلی
        /// </summary>
        private string GetSecureErrorMessage(string failureType)
        {
            return failureType switch
            {
                "DatabaseConnectionFailure" => "سرویس موقتاً در دسترس نیست",
                "DatabaseOperationFailure" => "خطا در پردازش درخواست",
                "MemoryExhaustion" => "سرویس موقتاً در دسترس نیست",
                "FileSystemFailure" => "خطا در پردازش درخواست",
                "NetworkFailure" => "خطا در ارتباط با سرویس‌های خارجی",
                "OperationTimeout" => "زمان پردازش درخواست به پایان رسید",
                "AuthorizationFailure" => "دسترسی غیرمجاز",
                _ => "خطای داخلی سرور"
            };
        }

        /// <summary>
        /// دریافت شناسه کاربر از Context
        /// </summary>
        private int? GetUserIdFromContext(HttpContext context)
        {
            var userIdClaim = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
                return userId;
            return null;
        }
    }
}

