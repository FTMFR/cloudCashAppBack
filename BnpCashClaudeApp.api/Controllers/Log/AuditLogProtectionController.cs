using BnpCashClaudeApp.api.Attributes;
using BnpCashClaudeApp.api.Helpers;
using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading;

namespace BnpCashClaudeApp.api.Controllers.Log
{
    /// <summary>
    /// کنترلر مدیریت حفاظت از داده‌های ممیزی
    /// پیاده‌سازی الزامات FAU_STG.3.1 و FAU_STG.4.1 از استاندارد ISO 15408
    /// 
    /// FAU_STG.3.1: اقدامات لازم در زمان از دست رفتن داده ممیزی
    /// - بررسی وضعیت سلامت سیستم ذخیره‌سازی
    /// - بازیابی لاگ‌های Fallback
    /// 
    /// FAU_STG.4.1: پیشگیری از اتلاف و از بین رفتن داده ممیزی
    /// - پشتیبان‌گیری دستی
    /// - مشاهده آمار
    /// - اعمال سیاست نگهداری
    /// </summary>  
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("ApiPolicy")]
    public class AuditLogProtectionController : ControllerBase
    {
        private readonly IAuditLogProtectionService _protectionService;
        private readonly IAuditLogService _auditLogService;
        private readonly IDataExportService _dataExportService;
        private readonly ILogger<AuditLogProtectionController> _logger;

        public AuditLogProtectionController(
            IAuditLogProtectionService protectionService,
            IAuditLogService auditLogService,
            IDataExportService dataExportService,
            ILogger<AuditLogProtectionController> logger)
        {
            _protectionService = protectionService;
            _auditLogService = auditLogService;
            _dataExportService = dataExportService;
            _logger = logger;
        }

        #region FAU_STG.3.1 - Storage Health & Recovery

        /// <summary>
        /// دریافت وضعیت سلامت سیستم ذخیره‌سازی ممیزی
        /// FAU_STG.3.1: بررسی وضعیت سیستم
        /// </summary>
        /// <returns>وضعیت سلامت</returns>
        [HttpGet("health")]
        [RequirePermission("AuditLog.Read")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetStorageHealth()
        {
            try
            {
                var status = await _protectionService.GetStorageHealthStatusAsync();

                // ثبت در Audit Log
                await LogActionAsync("AuditStorageHealthCheck", "StorageHealth", "Check", true);

                var response = new
                {
                    status.IsDatabaseHealthy,
                    status.IsFallbackHealthy,
                    status.PendingFallbackLogs,
                    status.LastSuccessfulSave,
                    status.LastFailure,
                    status.FailureCountLast24Hours,
                    status.TotalLogsCount,
                    status.Status,
                    checkedAt = DateTime.UtcNow
                };

                var protectedResponse = await ProtectReadPayloadAsync(response, "AuditLogStorageHealth");
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking storage health");
                return StatusCode(500, new { error = "خطا در بررسی وضعیت سیستم ذخیره‌سازی" });
            }
        }

        /// <summary>
        /// دریافت تعداد لاگ‌های موجود در Fallback Storage
        /// FAU_STG.3.1: بررسی لاگ‌های Fallback
        /// </summary>
        /// <returns>تعداد لاگ‌های Fallback</returns>
        [HttpGet("fallback/count")]
        [RequirePermission("AuditLog.Read")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetFallbackLogCount()
        {
            try
            {
                var count = await _protectionService.GetFallbackLogCountAsync();

                var response = new
                {
                    pendingFallbackLogs = count,
                    checkedAt = DateTime.UtcNow
                };

                var protectedResponse = await ProtectReadPayloadAsync(response, "AuditLogFallbackStatus");
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fallback log count");
                return StatusCode(500, new { error = "خطا در دریافت تعداد لاگ‌های Fallback" });
            }
        }

        /// <summary>
        /// بازیابی لاگ‌های Fallback به دیتابیس اصلی
        /// FAU_STG.3.1: بازیابی لاگ‌های از دست رفته
        /// </summary>
        /// <returns>تعداد لاگ‌های بازیابی شده</returns>
        [HttpPost("fallback/recover")]
        [RequirePermission("AuditLog.Admin")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> RecoverFallbackLogs()
        {
            try
            {
                var recovered = await _protectionService.RecoverFallbackLogsAsync();

                // ثبت در Audit Log
                await LogActionAsync("AuditFallbackRecovery", "FallbackLogs", "Recover", true,
                    $"Recovered {recovered} logs from fallback storage");

                return Ok(new
                {
                    recoveredCount = recovered,
                    recoveredAt = DateTime.UtcNow,
                    message = recovered > 0
                        ? $"{recovered} لاگ با موفقیت بازیابی شد"
                        : "هیچ لاگی برای بازیابی یافت نشد"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recovering fallback logs");
                await LogActionAsync("AuditFallbackRecovery", "FallbackLogs", "Recover", false, ex.Message);
                return StatusCode(500, new { error = "خطا در بازیابی لاگ‌های Fallback" });
            }
        }

        #endregion

        #region FAU_STG.4.1 - Backup & Retention

        /// <summary>
        /// دریافت آمار داده‌های ممیزی
        /// FAU_STG.4.1: بررسی وضعیت داده‌ها
        /// </summary>
        /// <returns>آمار لاگ‌های ممیزی</returns>
        [HttpGet("statistics")]
        [RequirePermission("AuditLog.Read")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var stats = await _protectionService.GetStatisticsAsync();

                var response = new
                {
                    stats.TotalLogs,
                    stats.TodayLogs,
                    stats.Last7DaysLogs,
                    stats.Last30DaysLogs,
                    stats.OldestLog,
                    stats.NewestLog,
                    stats.SuccessfulLogs,
                    stats.FailedLogs,
                    successRate = stats.TotalLogs > 0
                        ? Math.Round((double)stats.SuccessfulLogs / stats.TotalLogs * 100, 2)
                        : 0,
                    stats.BackupsCount,
                    stats.LastBackupDate,
                    stats.ArchivesCount,
                    stats.LastArchiveDate,
                    logsByEventType = stats.LogsByEventType.Take(20).ToDictionary(x => x.Key, x => x.Value),
                    generatedAt = DateTime.UtcNow
                };

                var protectedResponse = await ProtectReadPayloadAsync(response, "AuditLogProtectionStatistics");
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit log statistics");
                return StatusCode(500, new { error = "خطا در دریافت آمار" });
            }
        }

        /// <summary>
        /// ایجاد پشتیبان دستی از داده‌های ممیزی
        /// FAU_STG.4.1: پشتیبان‌گیری
        /// </summary>
        /// <param name="request">تنظیمات پشتیبان‌گیری</param>
        /// <returns>نتیجه پشتیبان‌گیری</returns>
        [HttpPost("backup")]
        [RequirePermission("AuditLog.Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateBackup([FromBody] CreateBackupRequest? request = null)
        {
            try
            {
                var result = await _protectionService.CreateBackupAsync(
                    request?.FromDate,
                    request?.ToDate);

                if (result.Success)
                {
                    await LogActionAsync("AuditBackupCreated", "Backup", result.BackupId ?? "Manual", true,
                        $"Created backup with {result.RecordsCount} records");

                    return Ok(new
                    {
                        result.Success,
                        result.BackupId,
                        result.RecordsCount,
                        result.FileSizeBytes,
                        fileSizeFormatted = FormatFileSize(result.FileSizeBytes),
                        result.FromDate,
                        result.ToDate,
                        result.CreatedAt,
                        message = $"پشتیبان با {result.RecordsCount} رکورد ایجاد شد"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        result.Success,
                        error = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                await LogActionAsync("AuditBackupCreated", "Backup", "Manual", false, ex.Message);
                return StatusCode(500, new { error = "خطا در ایجاد پشتیبان" });
            }
        }

        /// <summary>
        /// دریافت لیست پشتیبان‌های موجود
        /// FAU_STG.4.1: مشاهده پشتیبان‌ها
        /// </summary>
        /// <returns>لیست پشتیبان‌ها</returns>
        [HttpGet("backups")]
        [RequirePermission("AuditLog.Read")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetBackups()
        {
            try
            {
                var backups = await _protectionService.GetBackupsAsync();

                var response = new
                {
                    count = backups.Count(),
                    backups = backups.Select(b => new
                    {
                        b.BackupId,
                        b.FileSizeBytes,
                        fileSizeFormatted = FormatFileSize(b.FileSizeBytes),
                        b.RecordsCount,
                        b.FromDate,
                        b.ToDate,
                        b.CreatedAt,
                        b.Description
                    }),
                    generatedAt = DateTime.UtcNow
                };

                var protectedResponse = await ProtectReadPayloadAsync(response, "AuditLogBackupList");
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting backups list");
                return StatusCode(500, new { error = "خطا در دریافت لیست پشتیبان‌ها" });
            }
        }

        /// <summary>
        /// آرشیو داده‌های قدیمی
        /// FAU_STG.4.1: آرشیو
        /// </summary>
        /// <param name="request">تنظیمات آرشیو</param>
        /// <returns>نتیجه آرشیو</returns>
        [HttpPost("archive")]
        [RequirePermission("AuditLog.Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ArchiveOldLogs([FromBody] ArchiveRequest request)
        {
            try
            {
                if (request.OlderThanDays <= 0)
                {
                    return BadRequest(new { error = "تعداد روز باید بیشتر از صفر باشد" });
                }

                var olderThan = DateTime.UtcNow.AddDays(-request.OlderThanDays);
                var result = await _protectionService.ArchiveOldLogsAsync(olderThan);

                if (result.Success)
                {
                    await LogActionAsync("AuditLogsArchived", "Archive", result.ArchivePath ?? "Manual", true,
                        $"Archived {result.ArchivedCount} logs older than {request.OlderThanDays} days");

                    return Ok(new
                    {
                        result.Success,
                        result.ArchivedCount,
                        result.OlderThan,
                        result.ArchivePath,
                        result.ArchivedAt,
                        message = $"{result.ArchivedCount} لاگ آرشیو شد"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        result.Success,
                        error = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving old logs");
                await LogActionAsync("AuditLogsArchived", "Archive", "Manual", false, ex.Message);
                return StatusCode(500, new { error = "خطا در آرشیو لاگ‌ها" });
            }
        }

        /// <summary>
        /// اعمال سیاست نگهداری
        /// FAU_STG.4.1: سیاست نگهداری
        /// </summary>
        /// <returns>نتیجه اعمال سیاست</returns>
        [HttpPost("retention/apply")]
        [RequirePermission("AuditLog.Admin")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ApplyRetentionPolicy()
        {
            try
            {
                var result = await _protectionService.ApplyRetentionPolicyAsync();

                if (result.Success)
                {
                    await LogActionAsync("AuditRetentionPolicyApplied", "RetentionPolicy", "Apply", true,
                        $"Archived: {result.ArchivedCount}, Deleted: {result.DeletedCount}");

                    return Ok(new
                    {
                        result.Success,
                        result.ArchivedCount,
                        result.DeletedCount,
                        result.RetentionDays,
                        result.ArchiveDays,
                        result.AppliedAt,
                        message = $"سیاست نگهداری اعمال شد. آرشیو: {result.ArchivedCount}، حذف: {result.DeletedCount}"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        result.Success,
                        error = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying retention policy");
                await LogActionAsync("AuditRetentionPolicyApplied", "RetentionPolicy", "Apply", false, ex.Message);
                return StatusCode(500, new { error = "خطا در اعمال سیاست نگهداری" });
            }
        }

        #endregion

        #region Helper Methods

        
        private async Task<T> ProtectReadPayloadAsync<T>(
            T data,
            string entityType,
            string? entityId = null,
            CancellationToken ct = default) where T : class
        {
            var context = new ExportContext
            {
                EntityType = entityType,
                EntityId = entityId,
                UserId = GetUserId(),
                UserName = User.Identity?.Name ?? "Unknown",
                IpAddress = HttpContextHelper.GetIpAddress(HttpContext),
                UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                RequestPath = HttpContext.Request.Path,
                RequestedFormat = "JSON"
            };

            var secured = await _dataExportService.WrapWithSecurityAttributesAsync(data, context, ct);
            return secured.Data;
        }

        private long GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("UserId")?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private async Task LogActionAsync(string eventType, string entityType, string entityId, bool success, string? description = null)
        {
            try
            {
                var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
                var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
                var userName = User.Identity?.Name ?? "Unknown";
                var userId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");

                await _auditLogService.LogEventAsync(
                    eventType: eventType,
                    entityType: entityType,
                    entityId: entityId,
                    isSuccess: success,
                    ipAddress: ipAddress,
                    userName: userName,
                    userId: userId,
                    userAgent: userAgent,
                    description: description);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log action: {EventType}", eventType);
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        #endregion
    }

    #region Request Models

    public class CreateBackupRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class ArchiveRequest
    {
        public int OlderThanDays { get; set; } = 90;
    }

    #endregion
}


