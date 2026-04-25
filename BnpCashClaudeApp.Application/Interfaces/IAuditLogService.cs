using BnpCashClaudeApp.Domain.Entities.AuditLogSubsystem;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس برای ثبت و مدیریت Audit Log
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// ثبت یک رویداد Audit Log
        /// </summary>
        Task<long> LogEventAsync(
            string eventType,
            string? entityType = null,
            string? entityId = null,
            bool isSuccess = true,
            string? errorMessage = null,
            string? ipAddress = null,
            string? userName = null,
            long? userId = null,
            string? operatingSystem = null,
            string? userAgent = null,
            string? description = null,
            List<AuditLogDetail>? details = null,
            CancellationToken ct = default);

        /// <summary>
        /// ثبت تغییرات یک موجودیت (برای Update)
        /// </summary>
        Task<long> LogEntityChangeAsync(
            string eventType,
            string entityType,
            string entityId,
            Dictionary<string, (object? oldValue, object? newValue)> changes,
            string? ipAddress = null,
            string? userName = null,
            long? userId = null,
            string? operatingSystem = null,
            string? userAgent = null,
            CancellationToken ct = default);
    }
}

