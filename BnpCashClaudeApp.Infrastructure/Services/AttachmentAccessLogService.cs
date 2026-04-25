using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.AuditLogSubsystem;
using BnpCashClaudeApp.Domain.Enums;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// پیاده‌سازی سرویس لاگ دسترسی به فایل‌های پیوست
    /// ============================================
    /// پیاده‌سازی الزامات امنیتی ISO 15408:
    /// - FAU_GEN.1: تولید داده ممیزی
    /// - FAU_GEN.2: مرتبط نمودن هویت کاربر
    /// - FTA_TAH.1: سوابق دسترسی به محصول
    /// ============================================
    /// </summary>
    public class AttachmentAccessLogService : IAttachmentAccessLogService
    {
        private readonly IDbContextFactory<LogDbContext> _contextFactory;
        private readonly ILogger<AttachmentAccessLogService>? _logger;
        private readonly string _fallbackDirectory;
        private const int MaxRetryAttempts = 3;
        private static readonly SemaphoreSlim _fallbackLock = new(1, 1);

        public AttachmentAccessLogService(
            IDbContextFactory<LogDbContext> contextFactory,
            ILogger<AttachmentAccessLogService>? logger = null)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _fallbackDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "attachment-access-log-fallback");

            if (!Directory.Exists(_fallbackDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_fallbackDirectory);
                }
                catch { /* Ignore */ }
            }
        }

        // ============================================
        // ثبت لاگ
        // ============================================

        public async Task<long> LogAccessAsync(
            long attachmentId,
            Guid attachmentPublicId,
            string fileName,
            AttachmentAccessType accessType,
            bool isSuccess,
            AttachmentAccessLogRequest request,
            CancellationToken ct = default)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    await using var context = await _contextFactory.CreateDbContextAsync(ct);

                    var now = DateTime.UtcNow;
                    var log = new tblAttachmentAccessLog
                    {
                        AttachmentId = attachmentId,
                        AttachmentPublicId = attachmentPublicId,
                        FileName = fileName,
                        FileType = request.FileType,
                        FileSize = request.FileSize,
                        AccessType = (int)accessType,
                        AccessDescription = request.Description,
                        UserId = request.UserId,
                        UserName = request.UserName,
                        UserGroupId = request.UserGroupId,
                        UserGroupName = request.UserGroupName,
                        IpAddress = request.IpAddress,
                        UserAgent = request.UserAgent,
                        Browser = request.Browser,
                        BrowserVersion = request.BrowserVersion,
                        OperatingSystem = request.OperatingSystem,
                        DeviceType = request.DeviceType,
                        AccessDateTime = now,
                        IsSuccess = isSuccess,
                        ErrorMessage = request.ErrorMessage,
                        FileSensitivityLevel = request.FileSensitivityLevel,
                        FileSecurityClassification = request.FileSecurityClassification,
                        WasEncrypted = request.WasEncrypted,
                        IntegrityVerified = request.IntegrityVerified,
                        IntegrityCheckResult = request.IntegrityCheckResult,
                        tblCustomerId = request.CustomerId,
                        tblShobeId = request.ShobeId,
                        RequestId = request.RequestId,
                        SessionId = request.SessionId,
                        AdditionalInfo = request.AdditionalInfo,
                        TblUserGrpIdInsert = request.UserId ?? 0
                    };

                    log.SetZamanInsert(DateTime.Now);
                    log.SetAccessDateTime(DateTime.Now);

                    context.tblAttachmentAccessLogs.Add(log);
                    await context.SaveChangesAsync(ct);

                    return log.Id;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger?.LogWarning(ex,
                        "[FAU_GEN.1] Attachment access log save failed. Attempt {Attempt}/{Max}. File: {FileName}",
                        attempt, MaxRetryAttempts, fileName);

                    if (attempt < MaxRetryAttempts)
                    {
                        var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                        await Task.Delay(delay, ct);
                    }
                }
            }

            // Fallback به فایل سیستم
            _logger?.LogError(lastException,
                "[FAU_GEN.1] All retry attempts failed. Using fallback storage. File: {FileName}",
                fileName);

            try
            {
                await SaveToFallbackAsync(new
                {
                    AttachmentId = attachmentId,
                    AttachmentPublicId = attachmentPublicId,
                    FileName = fileName,
                    AccessType = accessType.ToString(),
                    IsSuccess = isSuccess,
                    Request = request,
                    FailedAt = DateTime.UtcNow,
                    FailureReason = lastException?.Message
                }, ct);
            }
            catch (Exception fallbackEx)
            {
                _logger?.LogCritical(fallbackEx,
                    "[FAU_GEN.1] CRITICAL: Both database and fallback storage failed! File: {FileName}",
                    fileName);
            }

            return -1;
        }

        public async Task<long> LogViewAsync(
            long attachmentId,
            Guid attachmentPublicId,
            string fileName,
            AttachmentAccessLogRequest request,
            CancellationToken ct = default)
        {
            return await LogAccessAsync(
                attachmentId,
                attachmentPublicId,
                fileName,
                AttachmentAccessType.View,
                true,
                request,
                ct);
        }

        public async Task<long> LogDownloadAsync(
            long attachmentId,
            Guid attachmentPublicId,
            string fileName,
            long bytesTransferred,
            AttachmentAccessLogRequest request,
            CancellationToken ct = default)
        {
            request.AdditionalInfo = JsonSerializer.Serialize(new { BytesTransferred = bytesTransferred });
            return await LogAccessAsync(
                attachmentId,
                attachmentPublicId,
                fileName,
                AttachmentAccessType.Download,
                true,
                request,
                ct);
        }

        public async Task<long> LogUploadAsync(
            long attachmentId,
            Guid attachmentPublicId,
            string fileName,
            long fileSize,
            AttachmentAccessLogRequest request,
            CancellationToken ct = default)
        {
            request.FileSize = fileSize;
            return await LogAccessAsync(
                attachmentId,
                attachmentPublicId,
                fileName,
                AttachmentAccessType.Upload,
                true,
                request,
                ct);
        }

        public async Task<long> LogDeleteAsync(
            long attachmentId,
            Guid attachmentPublicId,
            string fileName,
            AttachmentAccessLogRequest request,
            CancellationToken ct = default)
        {
            return await LogAccessAsync(
                attachmentId,
                attachmentPublicId,
                fileName,
                AttachmentAccessType.Delete,
                true,
                request,
                ct);
        }

        public async Task<long> LogAccessDeniedAsync(
            long attachmentId,
            Guid attachmentPublicId,
            string fileName,
            AttachmentAccessType accessType,
            string deniedReason,
            AttachmentAccessLogRequest request,
            CancellationToken ct = default)
        {
            request.ErrorMessage = deniedReason;
            
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var now = DateTime.UtcNow;
            var log = new tblAttachmentAccessLog
            {
                AttachmentId = attachmentId,
                AttachmentPublicId = attachmentPublicId,
                FileName = fileName,
                FileType = request.FileType,
                AccessType = (int)accessType,
                UserId = request.UserId,
                UserName = request.UserName,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                AccessDateTime = now,
                IsSuccess = false,
                AccessDeniedReason = deniedReason,
                ErrorMessage = request.ErrorMessage,
                tblCustomerId = request.CustomerId,
                tblShobeId = request.ShobeId,
                HttpStatusCode = 403,
                TblUserGrpIdInsert = request.UserId ?? 0
            };

            log.SetZamanInsert(DateTime.Now);
            log.SetAccessDateTime(DateTime.Now);

            context.tblAttachmentAccessLogs.Add(log);
            await context.SaveChangesAsync(ct);

            return log.Id;
        }

        // ============================================
        // جستجو و دریافت لاگ‌ها
        // ============================================

        public async Task<IEnumerable<tblAttachmentAccessLog>> GetByAttachmentAsync(
            Guid attachmentPublicId,
            int? limit = null,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var query = context.tblAttachmentAccessLogs
                .Where(l => l.AttachmentPublicId == attachmentPublicId)
                .OrderByDescending(l => l.AccessDateTime);

            if (limit.HasValue)
                return await query.Take(limit.Value).ToListAsync(ct);

            return await query.ToListAsync(ct);
        }

        public async Task<IEnumerable<tblAttachmentAccessLog>> GetByUserAsync(
            long userId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? limit = null,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var query = context.tblAttachmentAccessLogs
                .Where(l => l.UserId == userId);

            if (fromDate.HasValue)
                query = query.Where(l => l.AccessDateTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.AccessDateTime <= toDate.Value);

            query = query.OrderByDescending(l => l.AccessDateTime);

            if (limit.HasValue)
                return await query.Take(limit.Value).ToListAsync(ct);

            return await query.ToListAsync(ct);
        }

        public async Task<AttachmentAccessLogSearchResult> SearchAsync(
            AttachmentAccessLogSearchRequest request,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var query = context.tblAttachmentAccessLogs.AsQueryable();

            // فیلترها
            if (request.AttachmentPublicId.HasValue)
                query = query.Where(l => l.AttachmentPublicId == request.AttachmentPublicId.Value);

            if (request.AttachmentId.HasValue)
                query = query.Where(l => l.AttachmentId == request.AttachmentId.Value);

            if (!string.IsNullOrEmpty(request.FileName))
                query = query.Where(l => l.FileName.Contains(request.FileName));

            if (request.AccessType != null)
                query = query.Where(l => l.AccessType == (int)request.AccessType.Value);

            if (request.UserId.HasValue)
                query = query.Where(l => l.UserId == request.UserId.Value);

            if (!string.IsNullOrEmpty(request.UserName))
                query = query.Where(l => l.UserName != null && l.UserName.Contains(request.UserName));

            if (!string.IsNullOrEmpty(request.IpAddress))
                query = query.Where(l => l.IpAddress != null && l.IpAddress.Contains(request.IpAddress));

            if (request.IsSuccess.HasValue)
                query = query.Where(l => l.IsSuccess == request.IsSuccess.Value);

            if (request.CustomerId.HasValue)
                query = query.Where(l => l.tblCustomerId == request.CustomerId.Value);

            if (request.ShobeId.HasValue)
                query = query.Where(l => l.tblShobeId == request.ShobeId.Value);

            if (request.FromDate.HasValue)
                query = query.Where(l => l.AccessDateTime >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(l => l.AccessDateTime <= request.ToDate.Value);

            // شمارش کل
            var totalCount = await query.CountAsync(ct);

            // مرتب‌سازی
            query = request.SortDescending
                ? query.OrderByDescending(l => l.AccessDateTime)
                : query.OrderBy(l => l.AccessDateTime);

            // صفحه‌بندی
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            return new AttachmentAccessLogSearchResult
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<IEnumerable<tblAttachmentAccessLog>> GetFailedAccessesAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? limit = null,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var query = context.tblAttachmentAccessLogs
                .Where(l => !l.IsSuccess);

            if (fromDate.HasValue)
                query = query.Where(l => l.AccessDateTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.AccessDateTime <= toDate.Value);

            query = query.OrderByDescending(l => l.AccessDateTime);

            if (limit.HasValue)
                return await query.Take(limit.Value).ToListAsync(ct);

            return await query.ToListAsync(ct);
        }

        public async Task<IEnumerable<tblAttachmentAccessLog>> GetByIpAddressAsync(
            string ipAddress,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? limit = null,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var query = context.tblAttachmentAccessLogs
                .Where(l => l.IpAddress == ipAddress);

            if (fromDate.HasValue)
                query = query.Where(l => l.AccessDateTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.AccessDateTime <= toDate.Value);

            query = query.OrderByDescending(l => l.AccessDateTime);

            if (limit.HasValue)
                return await query.Take(limit.Value).ToListAsync(ct);

            return await query.ToListAsync(ct);
        }

        // ============================================
        // تحلیل و آمار
        // ============================================

        public async Task<AttachmentAccessStatistics> GetStatisticsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            long? customerId = null,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var query = context.tblAttachmentAccessLogs.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(l => l.AccessDateTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.AccessDateTime <= toDate.Value);

            if (customerId.HasValue)
                query = query.Where(l => l.tblCustomerId == customerId.Value);

            var logs = await query.ToListAsync(ct);

            return new AttachmentAccessStatistics
            {
                TotalAccesses = logs.Count,
                SuccessfulAccesses = logs.Count(l => l.IsSuccess),
                FailedAccesses = logs.Count(l => !l.IsSuccess),
                TotalDownloads = logs.Count(l => l.AccessType == (int)AttachmentAccessType.Download),
                TotalViews = logs.Count(l => l.AccessType == (int)AttachmentAccessType.View),
                TotalUploads = logs.Count(l => l.AccessType == (int)AttachmentAccessType.Upload),
                TotalDeletes = logs.Count(l => l.AccessType == (int)AttachmentAccessType.Delete),
                UniqueUsers = logs.Where(l => l.UserId.HasValue).Select(l => l.UserId).Distinct().Count(),
                UniqueFiles = logs.Select(l => l.AttachmentId).Distinct().Count(),
                TotalBytesTransferred = logs.Where(l => l.BytesTransferred.HasValue).Sum(l => l.BytesTransferred ?? 0),
                AccessesByType = logs.GroupBy(l => ((AttachmentAccessType)l.AccessType).ToString())
                    .ToDictionary(g => g.Key, g => (long)g.Count()),
                AccessesByDay = logs.GroupBy(l => l.AccessDateTime.Date.ToString("yyyy-MM-dd"))
                    .ToDictionary(g => g.Key, g => (long)g.Count())
            };
        }

        public async Task<IEnumerable<TopAccessedFile>> GetTopAccessedFilesAsync(
            int top = 10,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var query = context.tblAttachmentAccessLogs.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(l => l.AccessDateTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.AccessDateTime <= toDate.Value);

            return await query
                .GroupBy(l => new { l.AttachmentId, l.AttachmentPublicId, l.FileName })
                .Select(g => new TopAccessedFile
                {
                    AttachmentId = g.Key.AttachmentId,
                    AttachmentPublicId = g.Key.AttachmentPublicId,
                    FileName = g.Key.FileName,
                    AccessCount = g.Count(),
                    DownloadCount = g.Count(l => l.AccessType == (int)AttachmentAccessType.Download),
                    ViewCount = g.Count(l => l.AccessType == (int)AttachmentAccessType.View),
                    LastAccessedAt = g.Max(l => l.AccessDateTime)
                })
                .OrderByDescending(f => f.AccessCount)
                .Take(top)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<TopFileAccessUser>> GetTopUsersAsync(
            int top = 10,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var query = context.tblAttachmentAccessLogs
                .Where(l => l.UserId.HasValue);

            if (fromDate.HasValue)
                query = query.Where(l => l.AccessDateTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.AccessDateTime <= toDate.Value);

            return await query
                .GroupBy(l => new { l.UserId, l.UserName })
                .Select(g => new TopFileAccessUser
                {
                    UserId = g.Key.UserId ?? 0,
                    UserName = g.Key.UserName ?? "",
                    TotalAccesses = g.Count(),
                    DownloadCount = g.Count(l => l.AccessType == (int)AttachmentAccessType.Download),
                    UploadCount = g.Count(l => l.AccessType == (int)AttachmentAccessType.Upload),
                    LastAccessAt = g.Max(l => l.AccessDateTime)
                })
                .OrderByDescending(u => u.TotalAccesses)
                .Take(top)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<SuspiciousAccessPattern>> DetectSuspiciousActivityAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            var patterns = new List<SuspiciousAccessPattern>();

            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var query = context.tblAttachmentAccessLogs.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(l => l.AccessDateTime >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.AccessDateTime <= toDate.Value);

            // الگوی 1: دسترسی‌های ناموفق زیاد از یک IP
            var failedByIp = await query
                .Where(l => !l.IsSuccess && l.IpAddress != null)
                .GroupBy(l => l.IpAddress)
                .Where(g => g.Count() >= 10)
                .Select(g => new { IpAddress = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            foreach (var item in failedByIp)
            {
                patterns.Add(new SuspiciousAccessPattern
                {
                    PatternType = "HighFailedAccessFromIp",
                    Description = $"تعداد زیاد دسترسی ناموفق از IP: {item.IpAddress}",
                    IpAddress = item.IpAddress,
                    OccurrenceCount = item.Count,
                    Severity = item.Count >= 50 ? "High" : "Medium"
                });
            }

            // الگوی 2: دانلود انبوه توسط یک کاربر
            var bulkDownloads = await query
                .Where(l => l.AccessType == (int)AttachmentAccessType.Download && l.UserId.HasValue)
                .GroupBy(l => new { l.UserId, l.UserName })
                .Where(g => g.Count() >= 50)
                .Select(g => new { g.Key.UserId, g.Key.UserName, Count = g.Count() })
                .ToListAsync(ct);

            foreach (var item in bulkDownloads)
            {
                patterns.Add(new SuspiciousAccessPattern
                {
                    PatternType = "BulkDownload",
                    Description = $"دانلود انبوه توسط کاربر: {item.UserName}",
                    UserId = item.UserId?.ToString(),
                    UserName = item.UserName,
                    OccurrenceCount = item.Count,
                    Severity = item.Count >= 100 ? "High" : "Medium"
                });
            }

            // الگوی 3: دسترسی در ساعات غیرعادی (بین 00:00 تا 06:00)
            var offHoursAccess = await query
                .Where(l => l.AccessDateTime.Hour >= 0 && l.AccessDateTime.Hour < 6 && l.UserId.HasValue)
                .GroupBy(l => new { l.UserId, l.UserName })
                .Where(g => g.Count() >= 5)
                .Select(g => new { g.Key.UserId, g.Key.UserName, Count = g.Count() })
                .ToListAsync(ct);

            foreach (var item in offHoursAccess)
            {
                patterns.Add(new SuspiciousAccessPattern
                {
                    PatternType = "OffHoursAccess",
                    Description = $"دسترسی در ساعات غیرعادی توسط: {item.UserName}",
                    UserId = item.UserId?.ToString(),
                    UserName = item.UserName,
                    OccurrenceCount = item.Count,
                    Severity = "Low"
                });
            }

            return patterns;
        }

        // ============================================
        // نگهداری و پاکسازی
        // ============================================

        public async Task<int> CleanupOldLogsAsync(
            int retentionDays,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            var oldLogs = await context.tblAttachmentAccessLogs
                .Where(l => l.AccessDateTime < cutoffDate)
                .ToListAsync(ct);

            if (oldLogs.Any())
            {
                context.tblAttachmentAccessLogs.RemoveRange(oldLogs);
                await context.SaveChangesAsync(ct);

                _logger?.LogInformation(
                    "Cleaned up {Count} old attachment access logs older than {Days} days",
                    oldLogs.Count, retentionDays);
            }

            return oldLogs.Count;
        }

        public async Task<int> ArchiveLogsAsync(
            int olderThanDays,
            string archivePath,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

            var logsToArchive = await context.tblAttachmentAccessLogs
                .Where(l => l.AccessDateTime < cutoffDate)
                .ToListAsync(ct);

            if (!logsToArchive.Any())
                return 0;

            // ذخیره در فایل آرشیو
            var archiveFileName = $"attachment_access_log_archive_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            var fullPath = Path.Combine(archivePath, archiveFileName);

            Directory.CreateDirectory(archivePath);

            var json = JsonSerializer.Serialize(logsToArchive, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(fullPath, json, ct);

            // حذف از دیتابیس
            context.tblAttachmentAccessLogs.RemoveRange(logsToArchive);
            await context.SaveChangesAsync(ct);

            _logger?.LogInformation(
                "Archived {Count} attachment access logs to {Path}",
                logsToArchive.Count, fullPath);

            return logsToArchive.Count;
        }

        // ============================================
        // متدهای کمکی
        // ============================================

        private async Task SaveToFallbackAsync(object entry, CancellationToken ct)
        {
            await _fallbackLock.WaitAsync(ct);
            try
            {
                var fileName = $"access_log_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.json";
                var filePath = Path.Combine(_fallbackDirectory, fileName);

                var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(filePath, json, ct);
            }
            finally
            {
                _fallbackLock.Release();
            }
        }
    }
}
