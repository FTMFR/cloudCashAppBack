using BnpCashClaudeApp.api.Extensions;
using BnpCashClaudeApp.api.Helpers;
using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.api.Controllers.Base
{
    /// <summary>
    /// Base Controller با قابلیت ثبت رویداد ممیزی (FAU_GEN.1, FAU_GEN.2.1)
    /// کنترلرهایی که عملیات Create/Update/Delete دارند از این کلاس ارث‌بری کنند
    /// </summary>
    public abstract class AuditControllerBase : ControllerBase
    {
        protected readonly IAuditLogService AuditLogService;

        protected AuditControllerBase(IAuditLogService auditLogService)
        {
            AuditLogService = auditLogService;
        }

        /// <summary>
        /// ثبت رویداد ممیزی برای عملیات‌های Create, Update, Delete
        /// </summary>
        /// <param name="eventType">نوع رویداد: Create, Update, Delete و...</param>
        /// <param name="entityType">نوع موجودیت: Shobe, User, Menu و...</param>
        /// <param name="entityId">شناسه موجودیت (اختیاری)</param>
        /// <param name="isSuccess">نتیجه عملیات</param>
        /// <param name="errorMessage">پیام خطا در صورت شکست (اختیاری)</param>
        /// <param name="description">توضیحات دلخواه (اختیاری - در غیر این صورت خودکار تولید می‌شود)</param>
        protected async Task LogAuditEventAsync(
            string eventType,
            string entityType,
            string? entityId,
            bool isSuccess,
            string? errorMessage = null,
            string? description = null)
        {
            try
            {
                var finalDescription = description ?? $"{eventType} {entityType}" + (entityId != null ? $" - Id: {entityId}" : "");
                await AuditLogService.LogEventAsync(
                    eventType: eventType,
                    entityType: entityType,
                    entityId: entityId,
                    isSuccess: isSuccess,
                    errorMessage: errorMessage,
                    ipAddress: HttpContextHelper.GetIpAddress(HttpContext),
                    userName: User?.GetUserName(),
                    userId: User?.GetUserId(),
                    operatingSystem: HttpContextHelper.GetOperatingSystem(HttpContextHelper.GetUserAgent(HttpContext)),
                    userAgent: HttpContextHelper.GetUserAgent(HttpContext),
                    description: finalDescription,
                    ct: default);
            }
            catch
            {
                // ثبت Audit Log نباید مانع از ادامه عملیات شود
            }
        }
    }
}
