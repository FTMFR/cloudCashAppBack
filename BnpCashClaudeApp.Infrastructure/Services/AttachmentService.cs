using BnpCashClaudeApp.Application.Helpers;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.Settings;
using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.AttachSubsystem;
using BnpCashClaudeApp.Domain.Enums;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// پیاده‌سازی سرویس مدیریت فایل‌های پیوست
    /// ============================================
    /// پیاده‌سازی الزامات امنیتی ISO 15408:
    /// - FDP_ITC.2: ورود داده با مشخصه امنیتی
    /// - FDP_ETC.2: خروج داده با مشخصه امنیتی
    /// - FDP_SDI.2: صحت داده ذخیره شده
    /// - FDP_RIP.2: حفاظت اطلاعات باقیمانده
    /// ============================================
    /// </summary>
    public class AttachmentService : IAttachmentService
    {
        private readonly IDbContextFactory<AttachDbContext> _contextFactory;
        private readonly ILogger<AttachmentService>? _logger;
        private readonly IAttachmentAccessLogService? _accessLogService;
        private readonly ISecureMemoryService? _secureMemoryService;
        private readonly IShobeSettingsService? _shobeSettingsService;
        private readonly IWebHostEnvironment? _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly AttachmentSettings _defaultSettings;
        private string _baseStoragePath;
        private const string AttachmentEncryptionAlgorithm = "AES-256-CBC";
        private const string DefenderScannerName = "WindowsDefender";
        private const string BuiltInScannerName = "BuiltIn-EICAR";
        private const string AttachmentEncryptionKeyPath = "Attachment:EncryptionKey";
        private const string GlobalEncryptionKeyPath = "Encryption:Key";
        private const string VirusScanDefenderPathKey = "Attachment:VirusScan:DefenderPath";
        private const string VirusScanTimeoutSecondsKey = "Attachment:VirusScan:ProcessTimeoutSeconds";
        private const string VirusScanEnforceInProductionKey = "Attachment:VirusScan:EnforceInProduction";
        private const string VirusScanRequireOperationalScannerInProductionKey = "Attachment:VirusScan:RequireOperationalScannerInProduction";
        private const string VirusScanRequireConfiguredPathInProductionKey = "Attachment:VirusScan:RequireConfiguredDefenderPathInProduction";
        private const string VirusScanAllowFallbackInProductionKey = "Attachment:VirusScan:AllowBuiltInFallbackInProduction";
        private const FileSensitivityLevel MinimumEncryptedSensitivityLevel = FileSensitivityLevel.Confidential;

        public AttachmentService(
            IDbContextFactory<AttachDbContext> contextFactory,
            IConfiguration configuration,
            IWebHostEnvironment? webHostEnvironment = null,
            ILogger<AttachmentService>? logger = null,
            IAttachmentAccessLogService? accessLogService = null,
            ISecureMemoryService? secureMemoryService = null,
            IShobeSettingsService? shobeSettingsService = null)
        {
            _contextFactory = contextFactory;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _accessLogService = accessLogService;
            _secureMemoryService = secureMemoryService;
            _shobeSettingsService = shobeSettingsService;

            // بارگذاری تنظیمات پیش‌فرض از appsettings.json
            _defaultSettings = new AttachmentSettings();
            configuration.GetSection("Attachment").Bind(_defaultSettings);

            // تعیین مسیر ذخیره‌سازی
            _baseStoragePath = ResolveStoragePath(_defaultSettings.StoragePath);

            if (!Directory.Exists(_baseStoragePath))
            {
                Directory.CreateDirectory(_baseStoragePath);
            }

            _logger?.LogInformation("Attachment storage path initialized: {Path}", _baseStoragePath);
        }

        /// <summary>
        /// تعیین مسیر ذخیره‌سازی با اولویت‌بندی صحیح
        /// 1. اگر مسیر مطلق باشد، همان استفاده می‌شود
        /// 2. اگر با wwwroot شروع شود و IWebHostEnvironment موجود باشد، از WebRootPath استفاده می‌شود
        /// 3. در غیر این صورت از BaseDirectory استفاده می‌شود
        /// </summary>
        private string ResolveStoragePath(string? configuredPath)
        {
            // اگر مسیری تنظیم نشده، از مسیر پیش‌فرض استفاده کن
            if (string.IsNullOrEmpty(configuredPath))
            {
                configuredPath = "attachments";
            }

            // اگر مسیر مطلق است، همان را برگردان
            if (Path.IsPathRooted(configuredPath))
            {
                return configuredPath;
            }

            // اگر با wwwroot شروع می‌شود
            if (configuredPath.StartsWith("wwwroot", StringComparison.OrdinalIgnoreCase))
            {
                // حذف wwwroot از ابتدای مسیر
                var relativePath = configuredPath.Substring("wwwroot".Length).TrimStart('/', '\\');

                // اگر IWebHostEnvironment موجود است و WebRootPath دارد
                if (_webHostEnvironment != null && !string.IsNullOrEmpty(_webHostEnvironment.WebRootPath))
                {
                    return Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
                }

                // Fallback: استفاده از ContentRootPath + wwwroot
                if (_webHostEnvironment != null && !string.IsNullOrEmpty(_webHostEnvironment.ContentRootPath))
                {
                    return Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot", relativePath);
                }
            }

            // Fallback نهایی: استفاده از BaseDirectory
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configuredPath);
        }

        /// <summary>
        /// دریافت تنظیمات از دیتابیس یا پیش‌فرض
        /// </summary>
        private async Task<AttachmentSettingsDto> GetSettingsAsync(Guid? shobePublicId = null, CancellationToken ct = default)
        {
            if (_shobeSettingsService != null)
            {
                return await _shobeSettingsService.GetAttachmentSettingsAsync(shobePublicId, ct);
            }

            // Fallback به تنظیمات پیش‌فرض
            return new AttachmentSettingsDto
            {
                StoragePath = _defaultSettings.StoragePath,
                MaxFileSizeMB = _defaultSettings.MaxFileSizeMB,
                AllowedExtensions = _defaultSettings.AllowedExtensions,
                ValidateMagicBytes = _defaultSettings.ValidateMagicBytes,
                EnableVirusScan = _defaultSettings.EnableVirusScan,
                EnableEncryption = _defaultSettings.EnableEncryption,
                MaxProfileImageSizeMB = _defaultSettings.MaxProfileImageSizeMB,
                AllowedImageExtensions = _defaultSettings.AllowedImageExtensions,
                UseWebRoot = true
            };
        }

        // ============================================
        // عملیات آپلود
        // ============================================

        public async Task<AttachmentUploadResult> UploadAsync(
            Stream fileStream,
            string originalFileName,
            string contentType,
            AttachmentUploadRequest request,
            CancellationToken ct = default)
        {
            try
            {
                // ============================================
                // دریافت تنظیمات از دیتابیس (با قابلیت Multi-tenant)
                // ============================================
                // TODO: در آینده می‌توان shobePublicId را از request دریافت کرد
                var settings = await GetSettingsAsync(null, ct);

                // ============================================
                // اعتبارسنجی فایل (FDP_ITC.2)
                // ============================================
                var allowedExtensions = settings.GetAllowedExtensionsArray();
                var maxSize = settings.MaxFileSizeBytes;

                // اگر تصویر پروفایل است، از تنظیمات تصویر استفاده کن
                if (request.AttachmentType == AttachmentType.ProfileImage)
                {
                    allowedExtensions = settings.GetAllowedImageExtensionsArray();
                    maxSize = settings.MaxProfileImageSizeBytes;
                }

                var validationResult = FileValidationHelper.ValidateFile(
                    fileStream,
                    originalFileName,
                    contentType,
                    allowedExtensions,
                    maxSize,
                    validateMagicBytes: settings.ValidateMagicBytes);

                if (!validationResult.IsValid)
                {
                    _logger?.LogWarning(
                        "[FDP_ITC.2] File validation failed. Name: {Name}, Error: {Error}",
                        originalFileName, validationResult.ErrorMessage);

                    return new AttachmentUploadResult
                    {
                        IsSuccess = false,
                        FileName = originalFileName,
                        ErrorMessage = validationResult.ErrorMessage
                    };
                }

                await using var context = await _contextFactory.CreateDbContextAsync(ct);

                // تولید نام فایل امن
                var fileExtension = Path.GetExtension(originalFileName).TrimStart('.');
                var storedFileName = $"{Guid.NewGuid():N}.{fileExtension}";

                // تعیین مسیر ذخیره‌سازی
                var now = DateTime.Now;
                var storagePath = Path.Combine(
                    now.Year.ToString(),
                    now.Month.ToString("D2"),
                    request.Category ?? "general");

                var fullDirectoryPath = Path.Combine(_baseStoragePath, storagePath);
                Directory.CreateDirectory(fullDirectoryPath);

                var fullFilePath = Path.Combine(fullDirectoryPath, storedFileName);

                // محاسبه هش فایل (FDP_SDI.2)
                string contentHash;
                long fileSize;

                using (var sha256 = SHA256.Create())
                {
                    fileStream.Position = 0;
                    var hashBytes = await sha256.ComputeHashAsync(fileStream, ct);
                    contentHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    fileSize = fileStream.Length;
                }

                // ذخیره فایل
                fileStream.Position = 0;
                await using (var fileStreamOut = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write))
                {
                    await fileStream.CopyToAsync(fileStreamOut, ct);
                }

                // ایجاد رکورد در دیتابیس
                var attachment = new tblAttachment
                {
                    OriginalFileName = originalFileName,
                    StoredFileName = storedFileName,
                    FileExtension = fileExtension,
                    ContentType = contentType,
                    FileSize = fileSize,
                    StoragePath = storagePath,
                    StorageType = (int)StorageType.FileSystem,
                    AttachmentType = (int)request.AttachmentType,
                    Category = request.Category ?? "general",
                    Description = request.Description,
                    EntityType = request.EntityType,
                    EntityId = request.EntityId,
                    EntityPublicId = request.EntityPublicId,
                    SensitivityLevel = (int)request.SensitivityLevel,
                    SecurityClassification = request.SecurityClassification,
                    SecurityLabels = request.SecurityLabels,
                    ContentHash = contentHash,
                    HashAlgorithm = "SHA256",
                    Status = (int)AttachmentStatus.Active,
                    tblCustomerId = request.tblCustomerId,
                    tblShobeId = request.tblShobeId,
                    TblUserGrpIdInsert = request.UploadedByUserId ?? 0
                };

                if (request.ExpiresAt != null)
                    attachment.SetExpiresAt(request.ExpiresAt.Value);

                attachment.SetZamanInsert(now);

                context.tblAttachments.Add(attachment);
                await context.SaveChangesAsync(ct);

                // SECURITY HARDENING: apply post-upload controls (encryption + malware scan) in fail-secure mode.
                var requiresEncryption = ShouldEncryptAtRest(settings, request);
                if (requiresEncryption)
                {
                    var encrypted = await EncryptAsync(attachment.PublicId, keyId: null, ct);
                    if (!encrypted)
                    {
                        await HardDeleteAsync(attachment.PublicId, request.UploadedByUserId, request.IpAddress, ct);
                        return new AttachmentUploadResult
                        {
                            IsSuccess = false,
                            FileName = originalFileName,
                            ErrorMessage = "رمزنگاری فایل انجام نشد و فایل حذف شد (Fail-Secure)."
                        };
                    }
                }

                // SECURITY HARDENING: if malware scanning is enabled (or enforced by production policy),
                // uploaded file must pass scan.
                var requiresVirusScan = settings.EnableVirusScan || ShouldEnforceVirusScanInCurrentEnvironment();
                if (requiresVirusScan)
                {
                    var scanResult = await ScanForVirusAsync(attachment.PublicId, ct);
                    if (!scanResult.IsClean)
                    {
                        await QuarantineAsync(
                            attachment.PublicId,
                            scanResult.ThreatName ?? "Malware detected during upload",
                            ct);

                        return new AttachmentUploadResult
                        {
                            IsSuccess = false,
                            FileName = originalFileName,
                            ErrorMessage = $"فایل مشکوک/آلوده شناسایی شد: {scanResult.ThreatName ?? "UnknownThreat"}"
                        };
                    }
                }

                // ثبت لاگ دسترسی
                if (_accessLogService != null)
                {
                    await _accessLogService.LogUploadAsync(
                        attachment.Id,
                        attachment.PublicId,
                        originalFileName,
                        fileSize,
                        new AttachmentAccessLogRequest
                        {
                            UserId = request.UploadedByUserId,
                            IpAddress = request.IpAddress,
                            CustomerId = request.tblCustomerId,
                            ShobeId = request.tblShobeId,
                            FileType = contentType,
                            FileSize = fileSize,
                            FileSensitivityLevel = (int)request.SensitivityLevel
                        },
                        ct);
                }

                _logger?.LogInformation(
                    "[FDP_ITC.2] File uploaded successfully. Id: {Id}, Name: {Name}, Size: {Size}",
                    attachment.Id, originalFileName, fileSize);

                return new AttachmentUploadResult
                {
                    IsSuccess = true,
                    PublicId = attachment.PublicId,
                    Id = attachment.Id,
                    FileName = originalFileName,
                    FileSize = fileSize,
                    ContentHash = contentHash
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[FDP_ITC.2] File upload failed. Name: {Name}", originalFileName);

                return new AttachmentUploadResult
                {
                    IsSuccess = false,
                    FileName = originalFileName,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<IEnumerable<AttachmentUploadResult>> UploadMultipleAsync(
            IEnumerable<AttachmentUploadItem> items,
            AttachmentUploadRequest request,
            CancellationToken ct = default)
        {
            var results = new List<AttachmentUploadResult>();

            foreach (var item in items)
            {
                var result = await UploadAsync(
                    item.FileStream,
                    item.OriginalFileName,
                    item.ContentType,
                    request,
                    ct);
                results.Add(result);
            }

            return results;
        }

        // ============================================
        // عملیات دانلود
        // ============================================

        public async Task<AttachmentDownloadResult> DownloadAsync(
            Guid publicId,
            long? userId = null,
            string? ipAddress = null,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var attachment = await context.tblAttachments
                .FirstOrDefaultAsync(a => a.PublicId == publicId && a.Status == (int)AttachmentStatus.Active, ct);

            if (attachment == null)
                return AttachmentDownloadResult.Failure("فایل یافت نشد");

            return await DownloadInternalAsync(attachment, userId, ipAddress, ct);
        }

        public async Task<AttachmentDownloadResult> DownloadByIdAsync(
            long id,
            long? userId = null,
            string? ipAddress = null,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var attachment = await context.tblAttachments
                .FirstOrDefaultAsync(a => a.Id == id && a.Status == (int)AttachmentStatus.Active, ct);

            if (attachment == null)
                return AttachmentDownloadResult.Failure("فایل یافت نشد");

            return await DownloadInternalAsync(attachment, userId, ipAddress, ct);
        }

        private async Task<AttachmentDownloadResult> DownloadInternalAsync(
            tblAttachment attachment,
            long? userId,
            string? ipAddress,
            CancellationToken ct)
        {
            var fullPath = Path.Combine(_baseStoragePath, attachment.StoragePath, attachment.StoredFileName);

            if (!File.Exists(fullPath))
            {
                _logger?.LogWarning("[FDP_ETC.2] File not found on disk. Id: {Id}, Path: {Path}",
                    attachment.Id, fullPath);
                return AttachmentDownloadResult.Failure("فایل در سیستم فایل یافت نشد");
            }

            // بررسی صحت (FDP_SDI.2)
            bool integrityVerified = false;
            string? actualHash = null;
            try
            {
                // SECURITY HARDENING: validate integrity on plaintext bytes even when file is encrypted at-rest.
                actualHash = await ComputePlaintextHashAsync(attachment, fullPath, ct);
                integrityVerified = string.Equals(actualHash, attachment.ContentHash, StringComparison.OrdinalIgnoreCase);

                if (!integrityVerified)
                {
                    _logger?.LogWarning(
                        "[FDP_SDI.2] File integrity check failed! Id: {Id}, Expected: {Expected}, Actual: {Actual}",
                        attachment.Id, attachment.ContentHash, actualHash);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[FDP_SDI.2] Integrity check failed due to processing error. Id: {Id}", attachment.Id);
            }

            // به‌روزرسانی اطلاعات دسترسی
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var attachmentToUpdate = await context.tblAttachments.FindAsync(new object[] { attachment.Id }, ct);
            if (attachmentToUpdate != null)
            {
                attachmentToUpdate.DownloadCount++;
                attachmentToUpdate.SetLastAccessedAt(DateTime.Now);
                attachmentToUpdate.LastAccessedFromIp = ipAddress;
                attachmentToUpdate.LastAccessedByUserId = userId;
                await context.SaveChangesAsync(ct);
            }

            // ثبت لاگ دسترسی
            if (_accessLogService != null)
            {
                await _accessLogService.LogDownloadAsync(
                    attachment.Id,
                    attachment.PublicId,
                    attachment.OriginalFileName,
                    attachment.FileSize,
                    new AttachmentAccessLogRequest
                    {
                        UserId = userId,
                        IpAddress = ipAddress,
                        FileType = attachment.ContentType,
                        FileSize = attachment.FileSize,
                        FileSensitivityLevel = attachment.SensitivityLevel,
                        WasEncrypted = attachment.IsEncrypted,
                        IntegrityVerified = integrityVerified,
                        IntegrityCheckResult = integrityVerified
                    },
                    ct);
            }

            // بازگرداندن فایل
            Stream resultStream;
            try
            {
                // SECURITY HARDENING: never return encrypted bytes to API callers.
                resultStream = await OpenPlaintextDownloadStreamAsync(attachment, fullPath, ct);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[FDP_ETC.2] Failed to open file stream for download. Id: {Id}", attachment.Id);
                return AttachmentDownloadResult.Failure("خطا در آماده‌سازی فایل برای دانلود");
            }

            return new AttachmentDownloadResult
            {
                IsSuccess = true,
                FileStream = resultStream,
                FileName = attachment.OriginalFileName,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                WasEncrypted = attachment.IsEncrypted,
                IntegrityVerified = integrityVerified
            };
        }

        public Task<string?> GetTemporaryDownloadUrlAsync(
            Guid publicId,
            TimeSpan expiration,
            CancellationToken ct = default)
        {
            // برای FileSystem پیاده‌سازی نمی‌شود
            // این متد برای Cloud Storage مثل Azure Blob یا S3 کاربرد دارد
            return Task.FromResult<string?>(null);
        }

        // ============================================
        // عملیات جستجو و دریافت
        // ============================================

        public async Task<tblAttachment?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            return await context.tblAttachments.FirstOrDefaultAsync(a => a.PublicId == publicId, ct);
        }

        public async Task<tblAttachment?> GetByIdAsync(long id, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            return await context.tblAttachments.FindAsync(new object[] { id }, ct);
        }

        public async Task<IEnumerable<tblAttachment>> GetByEntityAsync(
            string entityType,
            long entityId,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            return await context.tblAttachments
                .Where(a => a.EntityType == entityType && a.EntityId == entityId && a.Status == (int)AttachmentStatus.Active)
                .OrderByDescending(a => a.Id)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<tblAttachment>> GetByEntityPublicIdAsync(
            string entityType,
            Guid entityPublicId,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            return await context.tblAttachments
                .Where(a => a.EntityType == entityType && a.EntityPublicId == entityPublicId && a.Status == (int)AttachmentStatus.Active)
                .OrderByDescending(a => a.Id)
                .ToListAsync(ct);
        }

        public async Task<AttachmentSearchResult> SearchAsync(
            AttachmentSearchRequest request,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var query = context.tblAttachments.AsQueryable();

            // فیلترها
            if (!string.IsNullOrEmpty(request.FileName))
                query = query.Where(a => a.OriginalFileName.Contains(request.FileName));

            if (!string.IsNullOrEmpty(request.Category))
                query = query.Where(a => a.Category == request.Category);

            if (request.AttachmentType != null)
                query = query.Where(a => a.AttachmentType == (int)request.AttachmentType.Value);

            if (request.Status != null)
                query = query.Where(a => a.Status == (int)request.Status.Value);

            if (request.SensitivityLevel != null)
                query = query.Where(a => a.SensitivityLevel == (int)request.SensitivityLevel.Value);

            if (!string.IsNullOrEmpty(request.EntityType))
                query = query.Where(a => a.EntityType == request.EntityType);

            if (request.EntityId != null)
                query = query.Where(a => a.EntityId == request.EntityId.Value);

            if (request.CustomerId != null)
                query = query.Where(a => a.tblCustomerId == request.CustomerId.Value);

            if (request.ShobeId != null)
                query = query.Where(a => a.tblShobeId == request.ShobeId.Value);

            // شمارش کل
            var totalCount = await query.CountAsync(ct);

            // مرتب‌سازی
            query = request.SortDescending
                ? query.OrderByDescending(a => a.Id)
                : query.OrderBy(a => a.Id);

            // صفحه‌بندی
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            return new AttachmentSearchResult
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        // ============================================
        // عملیات حذف
        // ============================================

        public async Task<bool> SoftDeleteAsync(
            Guid publicId,
            long? userId = null,
            string? ipAddress = null,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var attachment = await context.tblAttachments
                .FirstOrDefaultAsync(a => a.PublicId == publicId, ct);

            if (attachment == null || !attachment.IsDeletable)
                return false;

            attachment.Status = (int)AttachmentStatus.Deleted;
            attachment.SetZamanLastEdit(DateTime.Now);
            attachment.TblUserGrpIdLastEdit = userId;

            await context.SaveChangesAsync(ct);

            // ثبت لاگ
            if (_accessLogService != null)
            {
                await _accessLogService.LogDeleteAsync(
                    attachment.Id,
                    attachment.PublicId,
                    attachment.OriginalFileName,
                    new AttachmentAccessLogRequest
                    {
                        UserId = userId,
                        IpAddress = ipAddress,
                        Description = "Soft delete"
                    },
                    ct);
            }

            return true;
        }

        public async Task<bool> HardDeleteAsync(
            Guid publicId,
            long? userId = null,
            string? ipAddress = null,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var attachment = await context.tblAttachments
                .FirstOrDefaultAsync(a => a.PublicId == publicId, ct);

            if (attachment == null || !attachment.IsDeletable)
                return false;

            // حذف فایل فیزیکی با پاکسازی امن (FDP_RIP.2)
            var fullPath = Path.Combine(_baseStoragePath, attachment.StoragePath, attachment.StoredFileName);

            if (File.Exists(fullPath))
            {
                // پاکسازی امن: نوشتن داده تصادفی قبل از حذف
                try
                {
                    var fileInfo = new FileInfo(fullPath);
                    var fileSize = fileInfo.Length;

                    using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Write))
                    {
                        var randomData = new byte[4096];
                        using var rng = RandomNumberGenerator.Create();

                        // 3 بار بازنویسی با داده تصادفی
                        for (int pass = 0; pass < 3; pass++)
                        {
                            fs.Position = 0;
                            long remaining = fileSize;
                            while (remaining > 0)
                            {
                                rng.GetBytes(randomData);
                                var bytesToWrite = (int)Math.Min(remaining, randomData.Length);
                                fs.Write(randomData, 0, bytesToWrite);
                                remaining -= bytesToWrite;
                            }
                            fs.Flush();
                        }
                    }

                    File.Delete(fullPath);
                    _logger?.LogInformation("[FDP_RIP.2] File securely deleted. Id: {Id}", attachment.Id);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[FDP_RIP.2] Failed to securely delete file. Id: {Id}", attachment.Id);
                }
            }

            // ثبت لاگ قبل از حذف رکورد
            if (_accessLogService != null)
            {
                await _accessLogService.LogDeleteAsync(
                    attachment.Id,
                    attachment.PublicId,
                    attachment.OriginalFileName,
                    new AttachmentAccessLogRequest
                    {
                        UserId = userId,
                        IpAddress = ipAddress,
                        Description = "Hard delete with secure wipe"
                    },
                    ct);
            }

            // حذف از دیتابیس
            context.tblAttachments.Remove(attachment);
            await context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> RestoreAsync(Guid publicId, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var attachment = await context.tblAttachments
                .FirstOrDefaultAsync(a => a.PublicId == publicId && a.Status == (int)AttachmentStatus.Deleted, ct);

            if (attachment == null)
                return false;

            attachment.Status = (int)AttachmentStatus.Active;
            attachment.SetZamanLastEdit(DateTime.Now);

            await context.SaveChangesAsync(ct);

            return true;
        }

        // ============================================
        // عملیات به‌روزرسانی
        // ============================================

        public async Task<bool> UpdateMetadataAsync(
            Guid publicId,
            AttachmentUpdateRequest request,
            long? userId = null,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var attachment = await context.tblAttachments
                .FirstOrDefaultAsync(a => a.PublicId == publicId, ct);

            if (attachment == null)
                return false;

            if (request.Description != null)
                attachment.Description = request.Description;

            if (request.Category != null)
                attachment.Category = request.Category;

            if (request.SecurityClassification != null)
                attachment.SecurityClassification = request.SecurityClassification;

            if (request.SecurityLabels != null)
                attachment.SecurityLabels = request.SecurityLabels;

            if (request.ExpiresAt != null)
                attachment.SetExpiresAt(request.ExpiresAt.Value);

            attachment.SetZamanLastEdit(DateTime.Now);
            attachment.TblUserGrpIdLastEdit = userId;

            await context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> ChangeSensitivityLevelAsync(
            Guid publicId,
            FileSensitivityLevel newLevel,
            long? userId = null,
            string? ipAddress = null,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var attachment = await context.tblAttachments
                .FirstOrDefaultAsync(a => a.PublicId == publicId, ct);

            if (attachment == null)
                return false;

            var oldLevel = attachment.SensitivityLevel;
            attachment.SensitivityLevel = (int)newLevel;
            attachment.SetZamanLastEdit(DateTime.Now);
            attachment.TblUserGrpIdLastEdit = userId;

            await context.SaveChangesAsync(ct);

            // Enforce at-rest encryption for high-sensitivity files even when tenant/global defaults are relaxed.
            if (newLevel >= MinimumEncryptedSensitivityLevel && !attachment.IsEncrypted)
            {
                var encrypted = await EncryptAsync(publicId, attachment.EncryptionKeyId, ct);
                if (!encrypted)
                {
                    attachment.SensitivityLevel = oldLevel;
                    attachment.SetZamanLastEdit(DateTime.Now);
                    attachment.TblUserGrpIdLastEdit = userId;
                    await context.SaveChangesAsync(ct);

                    _logger?.LogError(
                        "[FCS_COP.1.1(3)] Sensitivity change rolled back because encryption failed. Id: {Id}, RequestedLevel: {Level}",
                        attachment.Id,
                        (int)newLevel);

                    return false;
                }
            }

            _logger?.LogInformation(
                "[FDP_ITC.2] Sensitivity level changed. Id: {Id}, From: {Old}, To: {New}",
                attachment.Id, oldLevel, (int)newLevel);

            return true;
        }

        // ============================================
        // عملیات امنیتی (FDP_SDI.2)
        // ============================================

        public async Task<IntegrityCheckResult> VerifyIntegrityAsync(
            Guid publicId,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var attachment = await context.tblAttachments
                .FirstOrDefaultAsync(a => a.PublicId == publicId, ct);

            if (attachment == null)
            {
                return new IntegrityCheckResult
                {
                    PublicId = publicId,
                    IsValid = false,
                    ErrorMessage = "Attachment not found",
                    CheckedAt = DateTime.UtcNow
                };
            }

            var fullPath = Path.Combine(_baseStoragePath, attachment.StoragePath, attachment.StoredFileName);

            if (!File.Exists(fullPath))
            {
                return new IntegrityCheckResult
                {
                    PublicId = publicId,
                    FileName = attachment.OriginalFileName,
                    IsValid = false,
                    ExpectedHash = attachment.ContentHash,
                    ErrorMessage = "File not found on disk",
                    CheckedAt = DateTime.UtcNow
                };
            }

            // SECURITY HARDENING: for encrypted files, hash must be computed on decrypted plaintext.
            string actualHash = await ComputePlaintextHashAsync(attachment, fullPath, ct);

            var isValid = actualHash == attachment.ContentHash;

            // به‌روزرسانی نتیجه بررسی
            attachment.SetLastIntegrityCheckAt(DateTime.Now);
            attachment.LastIntegrityCheckResult = isValid;
            await context.SaveChangesAsync(ct);

            if (!isValid)
            {
                _logger?.LogWarning(
                    "[FDP_SDI.2] Integrity verification failed! Id: {Id}, Expected: {Expected}, Actual: {Actual}",
                    attachment.Id, attachment.ContentHash, actualHash);
            }

            return new IntegrityCheckResult
            {
                PublicId = publicId,
                FileName = attachment.OriginalFileName,
                IsValid = isValid,
                ExpectedHash = attachment.ContentHash,
                ActualHash = actualHash,
                CheckedAt = DateTime.UtcNow
            };
        }

        public async Task<IEnumerable<IntegrityCheckResult>> VerifyAllIntegrityAsync(
            int batchSize = 100,
            CancellationToken ct = default)
        {
            var results = new List<IntegrityCheckResult>();
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var attachments = await context.tblAttachments
                .Where(a => a.Status == (int)AttachmentStatus.Active)
                .Take(batchSize)
                .ToListAsync(ct);

            foreach (var attachment in attachments)
            {
                var result = await VerifyIntegrityAsync(attachment.PublicId, ct);
                results.Add(result);
            }

            return results;
        }

        public async Task<string?> RecalculateHashAsync(Guid publicId, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var attachment = await context.tblAttachments
                .FirstOrDefaultAsync(a => a.PublicId == publicId, ct);

            if (attachment == null)
                return null;

            var fullPath = Path.Combine(_baseStoragePath, attachment.StoragePath, attachment.StoredFileName);

            if (!File.Exists(fullPath))
                return null;

            // SECURITY HARDENING: for encrypted files, hash must be computed on decrypted plaintext.
            string newHash = await ComputePlaintextHashAsync(attachment, fullPath, ct);

            attachment.ContentHash = newHash;
            attachment.SetZamanLastEdit(DateTime.Now);
            await context.SaveChangesAsync(ct);

            return newHash;
        }

        // ============================================
        // عملیات رمزنگاری (پیاده‌سازی ساده)
        // ============================================

        public async Task<bool> EncryptAsync(Guid publicId, Guid? keyId = null, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var attachment = await context.tblAttachments.FirstOrDefaultAsync(a => a.PublicId == publicId, ct);
            if (attachment == null)
                return false;

            if (attachment.IsEncrypted)
                return true;

            var fullPath = Path.Combine(_baseStoragePath, attachment.StoragePath, attachment.StoredFileName);
            if (!File.Exists(fullPath))
            {
                _logger?.LogWarning("[FCS_COP.1.1(3)] Encrypt failed, file not found. Id: {Id}", attachment.Id);
                return false;
            }

            byte[] encryptionKey = Array.Empty<byte>();
            byte[] iv = Array.Empty<byte>();
            try
            {
                // SECURITY HARDENING: require an explicit encryption key from secure config.
                encryptionKey = GetEncryptionKeyOrThrow();
                iv = RandomNumberGenerator.GetBytes(16);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[FCS_COP.1.1(3)] Encrypt failed due to missing/invalid key. Id: {Id}", attachment.Id);
                return false;
            }

            var tempPath = $"{fullPath}.enc.tmp";
            try
            {
                using var aes = CreateAesCipher(encryptionKey, iv);
                await using var input = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await using var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await using (var cryptoStream = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    await input.CopyToAsync(cryptoStream, ct);
                    cryptoStream.FlushFinalBlock();
                }

                File.Move(tempPath, fullPath, true);

                attachment.IsEncrypted = true;
                attachment.EncryptionAlgorithm = AttachmentEncryptionAlgorithm;
                attachment.EncryptionIV = Convert.ToBase64String(iv);
                attachment.EncryptionKeyId = keyId;
                attachment.SetZamanLastEdit(DateTime.Now);
                await context.SaveChangesAsync(ct);

                _logger?.LogInformation("[FCS_COP.1.1(3)] File encrypted successfully. Id: {Id}", attachment.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[FCS_COP.1.1(3)] Encrypt operation failed. Id: {Id}", attachment.Id);
                return false;
            }
            finally
            {
                SafeDeleteIfExists(tempPath);
                ClearSensitiveBytes(encryptionKey);
                ClearSensitiveBytes(iv);
            }
        }

        public async Task<bool> DecryptAsync(Guid publicId, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var attachment = await context.tblAttachments.FirstOrDefaultAsync(a => a.PublicId == publicId, ct);
            if (attachment == null)
                return false;

            if (!attachment.IsEncrypted)
                return true;

            var fullPath = Path.Combine(_baseStoragePath, attachment.StoragePath, attachment.StoredFileName);
            if (!File.Exists(fullPath))
            {
                _logger?.LogWarning("[FCS_COP.1.1(3)] Decrypt failed, file not found. Id: {Id}", attachment.Id);
                return false;
            }

            if (!TryGetEncryptionIv(attachment, out var iv))
            {
                _logger?.LogWarning("[FCS_COP.1.1(3)] Decrypt failed, IV is missing/invalid. Id: {Id}", attachment.Id);
                return false;
            }

            byte[] encryptionKey = Array.Empty<byte>();
            try
            {
                encryptionKey = GetEncryptionKeyOrThrow();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[FCS_COP.1.1(3)] Decrypt failed due to missing/invalid key. Id: {Id}", attachment.Id);
                return false;
            }

            var tempPath = $"{fullPath}.dec.tmp";
            try
            {
                using var aes = CreateAesCipher(encryptionKey, iv);
                await using var encryptedInput = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await using var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await using (var cryptoStream = new CryptoStream(encryptedInput, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    await cryptoStream.CopyToAsync(output, ct);
                }

                var decryptedHash = await ComputeSha256HashAsync(tempPath, ct);
                if (!string.Equals(decryptedHash, attachment.ContentHash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger?.LogWarning(
                        "[FDP_SDI.2] Decrypt produced hash mismatch. Id: {Id}, Expected: {Expected}, Actual: {Actual}",
                        attachment.Id,
                        attachment.ContentHash,
                        decryptedHash);
                    return false;
                }

                File.Move(tempPath, fullPath, true);

                attachment.IsEncrypted = false;
                attachment.EncryptionAlgorithm = null;
                attachment.EncryptionIV = null;
                attachment.EncryptionKeyId = null;
                attachment.SetZamanLastEdit(DateTime.Now);
                await context.SaveChangesAsync(ct);

                _logger?.LogInformation("[FCS_COP.1.1(3)] File decrypted successfully. Id: {Id}", attachment.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[FCS_COP.1.1(3)] Decrypt operation failed. Id: {Id}", attachment.Id);
                return false;
            }
            finally
            {
                SafeDeleteIfExists(tempPath);
                ClearSensitiveBytes(encryptionKey);
                ClearSensitiveBytes(iv);
            }
        }

        public async Task<VirusScanResult> ScanForVirusAsync(Guid publicId, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var attachment = await context.tblAttachments.FirstOrDefaultAsync(a => a.PublicId == publicId, ct);
            if (attachment == null)
            {
                return new VirusScanResult
                {
                    IsClean = false,
                    ThreatName = "AttachmentNotFound",
                    ScannerName = BuiltInScannerName,
                    ScannedAt = DateTime.UtcNow
                };
            }

            var fullPath = Path.Combine(_baseStoragePath, attachment.StoragePath, attachment.StoredFileName);
            if (!File.Exists(fullPath))
            {
                return new VirusScanResult
                {
                    IsClean = false,
                    ThreatName = "FileNotFound",
                    ScannerName = BuiltInScannerName,
                    ScannedAt = DateTime.UtcNow
                };
            }

            // SECURITY HARDENING: if scanner cannot provide a clean result, default to fail-secure.
            var scanResult = new VirusScanResult
            {
                IsClean = false,
                ThreatName = "ScanNotExecuted",
                ScannerName = BuiltInScannerName,
                ScannedAt = DateTime.UtcNow
            };

            var defenderScan = await TryScanWithWindowsDefenderAsync(fullPath, ct);
            if (defenderScan.Executed)
            {
                scanResult.IsClean = defenderScan.IsClean;
                scanResult.ThreatName = defenderScan.ThreatName;
                scanResult.ScannerName = DefenderScannerName;
            }
            else
            {
                var scannerUnavailabilityReason = defenderScan.ThreatName ?? "ScannerUnavailable";
                if (ShouldRequireOperationalScanner())
                {
                    scanResult.IsClean = false;
                    scanResult.ThreatName = scannerUnavailabilityReason;
                    scanResult.ScannerName = DefenderScannerName;

                    _logger?.LogError(
                        "[FDP_ITC.2] Operational malware scanner is required but unavailable. Id: {Id}, Reason: {Reason}",
                        attachment.Id,
                        scannerUnavailabilityReason);
                }
                else if (CanUseBuiltInFallbackScanner())
                {
                    var eicarDetected = await ContainsEicarSignatureAsync(fullPath, ct);
                    scanResult.IsClean = !eicarDetected;
                    scanResult.ThreatName = eicarDetected ? "EICAR-Test-Signature" : null;
                    scanResult.ScannerName = BuiltInScannerName;
                }
                else
                {
                    scanResult.IsClean = false;
                    scanResult.ThreatName = scannerUnavailabilityReason;
                    scanResult.ScannerName = DefenderScannerName;
                }
            }

            attachment.IsVirusScanned = true;
            attachment.VirusScanResult = scanResult.IsClean;
            attachment.SetVirusScannedAt(DateTime.Now);
            attachment.SetZamanLastEdit(DateTime.Now);

            if (!scanResult.IsClean)
            {
                _logger?.LogWarning(
                    "[FDP_ITC.2] Malware scan failed. Id: {Id}, Threat: {Threat}, Scanner: {Scanner}",
                    attachment.Id,
                    scanResult.ThreatName,
                    scanResult.ScannerName);
            }

            await context.SaveChangesAsync(ct);
            return scanResult;
        }

        private bool IsProductionEnvironment()
        {
            return string.Equals(
                _webHostEnvironment?.EnvironmentName,
                "Production",
                StringComparison.OrdinalIgnoreCase);
        }

        private bool ShouldEnforceVirusScanInCurrentEnvironment()
        {
            if (!IsProductionEnvironment())
                return false;

            return _configuration.GetValue<bool>(VirusScanEnforceInProductionKey, true);
        }

        private bool ShouldRequireOperationalScanner()
        {
            if (!IsProductionEnvironment())
                return false;

            return _configuration.GetValue<bool>(VirusScanRequireOperationalScannerInProductionKey, true);
        }

        private bool ShouldRequireConfiguredDefenderPath()
        {
            if (!IsProductionEnvironment())
                return false;

            return _configuration.GetValue<bool>(VirusScanRequireConfiguredPathInProductionKey, true);
        }

        private bool CanUseBuiltInFallbackScanner()
        {
            if (!IsProductionEnvironment())
                return true;

            return _configuration.GetValue<bool>(VirusScanAllowFallbackInProductionKey, false);
        }

        private int GetVirusScanTimeoutSeconds()
        {
            var configured = _configuration.GetValue<int?>(VirusScanTimeoutSecondsKey) ?? 120;
            return Math.Clamp(configured, 5, 1800);
        }

        private static bool ShouldEncryptAtRest(AttachmentSettingsDto settings, AttachmentUploadRequest request)
        {
            return settings.EnableEncryption ||
                   request.ShouldEncrypt ||
                   request.SensitivityLevel >= MinimumEncryptedSensitivityLevel;
        }

        private byte[] GetEncryptionKeyOrThrow()
        {
            var keyString = _configuration[AttachmentEncryptionKeyPath];
            if (string.IsNullOrWhiteSpace(keyString))
                keyString = _configuration[GlobalEncryptionKeyPath];

            if (string.IsNullOrWhiteSpace(keyString))
            {
                throw new InvalidOperationException(
                    $"Attachment encryption key is not configured. Set '{AttachmentEncryptionKeyPath}' or '{GlobalEncryptionKeyPath}'.");
            }

            if (TryReadBase64Key(keyString, out var base64Key))
            {
                if (base64Key.Length == 32)
                    return base64Key;

                using var sha256 = SHA256.Create();
                return sha256.ComputeHash(base64Key);
            }

            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
            }
        }

        private static bool TryReadBase64Key(string keyMaterial, out byte[] keyBytes)
        {
            keyBytes = Array.Empty<byte>();
            try
            {
                keyBytes = Convert.FromBase64String(keyMaterial);
                return keyBytes.Length > 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static Aes CreateAesCipher(byte[] key, byte[] iv)
        {
            var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }

        private bool TryGetEncryptionIv(tblAttachment attachment, out byte[] iv)
        {
            iv = Array.Empty<byte>();
            if (string.IsNullOrWhiteSpace(attachment.EncryptionIV))
                return false;

            try
            {
                iv = Convert.FromBase64String(attachment.EncryptionIV);
                return iv.Length == 16;
            }
            catch
            {
                return false;
            }
        }

        private static void SafeDeleteIfExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!File.Exists(path))
                return;

            try
            {
                File.Delete(path);
            }
            catch
            {
                // ignore cleanup errors
            }
        }

        private static void SafeKillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // ignore best-effort cleanup errors
            }
        }

        private void ClearSensitiveBytes(byte[] data)
        {
            if (data == null || data.Length == 0)
                return;

            if (_secureMemoryService != null)
            {
                _secureMemoryService.ClearBytes(data);
                return;
            }

            Array.Clear(data, 0, data.Length);
        }

        private async Task<string> ComputeSha256HashAsync(string filePath, CancellationToken ct)
        {
            using var sha256 = SHA256.Create();
            await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var hashBytes = await sha256.ComputeHashAsync(fs, ct);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        private async Task<string> ComputePlaintextHashAsync(tblAttachment attachment, string fullPath, CancellationToken ct)
        {
            if (!attachment.IsEncrypted)
                return await ComputeSha256HashAsync(fullPath, ct);

            if (!TryGetEncryptionIv(attachment, out var iv))
                throw new InvalidOperationException($"Encrypted attachment {attachment.Id} has invalid IV metadata.");

            byte[] key = GetEncryptionKeyOrThrow();
            try
            {
                using var aes = CreateAesCipher(key, iv);
                await using var encryptedStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await using var cryptoStream = new CryptoStream(encryptedStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var sha256 = SHA256.Create();
                var hashBytes = await sha256.ComputeHashAsync(cryptoStream, ct);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
            finally
            {
                ClearSensitiveBytes(key);
                ClearSensitiveBytes(iv);
            }
        }

        private async Task<Stream> OpenPlaintextDownloadStreamAsync(tblAttachment attachment, string fullPath, CancellationToken ct)
        {
            if (!attachment.IsEncrypted)
                return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (!TryGetEncryptionIv(attachment, out var iv))
                throw new InvalidOperationException($"Encrypted attachment {attachment.Id} has invalid IV metadata.");

            byte[] key = GetEncryptionKeyOrThrow();
            try
            {
                using var aes = CreateAesCipher(key, iv);
                await using var encryptedStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await using var cryptoStream = new CryptoStream(encryptedStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                var output = new MemoryStream();
                await cryptoStream.CopyToAsync(output, ct);
                output.Position = 0;
                return output;
            }
            finally
            {
                ClearSensitiveBytes(key);
                ClearSensitiveBytes(iv);
            }
        }

        private async Task<(bool Executed, bool IsClean, string? ThreatName)> TryScanWithWindowsDefenderAsync(
            string fullPath,
            CancellationToken ct)
        {
            var candidates = new List<string>();
            var configuredPath = _configuration[VirusScanDefenderPathKey];
            var requireConfiguredPath = ShouldRequireConfiguredDefenderPath();

            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                if (File.Exists(configuredPath))
                {
                    candidates.Add(configuredPath);
                }
                else if (requireConfiguredPath)
                {
                    _logger?.LogError(
                        "[FDP_ITC.2] Configured Defender path is not valid. Key: {ConfigKey}, Path: {Path}",
                        VirusScanDefenderPathKey,
                        configuredPath);
                    return (false, false, "ConfiguredDefenderPathNotFound");
                }
            }
            else if (requireConfiguredPath)
            {
                _logger?.LogError(
                    "[FDP_ITC.2] Defender path is required in production but not configured. Key: {ConfigKey}",
                    VirusScanDefenderPathKey);
                return (false, false, "DefenderPathNotConfigured");
            }

            if (candidates.Count == 0)
            {
                candidates.Add(@"C:\Program Files\Windows Defender\MpCmdRun.exe");
                candidates.Add(@"C:\Program Files\Microsoft Defender\MpCmdRun.exe");
            }

            var scannerPath = candidates.FirstOrDefault(File.Exists);
            if (string.IsNullOrWhiteSpace(scannerPath))
                return (false, false, "WindowsDefenderNotFound");

            var timeoutSeconds = GetVirusScanTimeoutSeconds();

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = scannerPath,
                        Arguments = $"-Scan -ScanType 3 -File \"{fullPath}\" -DisableRemediation",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                if (!process.Start())
                    return (true, false, "WindowsDefenderStartFailed");

                var stdOutTask = process.StandardOutput.ReadToEndAsync();
                var stdErrTask = process.StandardError.ReadToEndAsync();

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                try
                {
                    await process.WaitForExitAsync(linkedCts.Token);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
                {
                    SafeKillProcess(process);
                    _logger?.LogWarning(
                        "[FDP_ITC.2] Windows Defender scan timed out after {TimeoutSeconds} seconds. File: {File}",
                        timeoutSeconds,
                        fullPath);
                    return (true, false, "WindowsDefenderScanTimeout");
                }

                var stdOut = await stdOutTask;
                var stdErr = await stdErrTask;

                if (process.ExitCode == 0)
                    return (true, true, null);

                if (process.ExitCode == 2)
                    return (true, false, "WindowsDefenderThreatDetected");

                _logger?.LogWarning(
                    "[FDP_ITC.2] Windows Defender returned exit code {ExitCode}. StdOut: {StdOut}. StdErr: {StdErr}",
                    process.ExitCode,
                    stdOut,
                    stdErr);

                return (true, false, "WindowsDefenderScanError");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[FDP_ITC.2] Windows Defender scan execution failed.");
                return (true, false, "WindowsDefenderScanError");
            }
        }

        private async Task<bool> ContainsEicarSignatureAsync(string fullPath, CancellationToken ct)
        {
            const string eicar = "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";
            var signature = Encoding.ASCII.GetBytes(eicar);
            var buffer = new byte[8192];
            var matched = 0;

            await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            int read;
            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                for (var i = 0; i < read; i++)
                {
                    if (buffer[i] == signature[matched])
                    {
                        matched++;
                        if (matched == signature.Length)
                            return true;
                    }
                    else
                    {
                        matched = buffer[i] == signature[0] ? 1 : 0;
                    }
                }
            }

            return false;
        }

        public async Task<bool> QuarantineAsync(Guid publicId, string reason, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var attachment = await context.tblAttachments
                .FirstOrDefaultAsync(a => a.PublicId == publicId, ct);

            if (attachment == null)
                return false;

            attachment.Status = (int)AttachmentStatus.Quarantined;
            attachment.Description = $"Quarantined: {reason}";
            attachment.SetZamanLastEdit(DateTime.Now);

            await context.SaveChangesAsync(ct);

            _logger?.LogWarning("[Security] File quarantined. Id: {Id}, Reason: {Reason}", attachment.Id, reason);

            return true;
        }

        // ============================================
        // عملیات آرشیو
        // ============================================

        public async Task<bool> ArchiveAsync(Guid publicId, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var attachment = await context.tblAttachments
                .FirstOrDefaultAsync(a => a.PublicId == publicId, ct);

            if (attachment == null)
                return false;

            attachment.Status = (int)AttachmentStatus.Archived;
            attachment.SetZamanLastEdit(DateTime.Now);

            await context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> UnarchiveAsync(Guid publicId, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var attachment = await context.tblAttachments
                .FirstOrDefaultAsync(a => a.PublicId == publicId && a.Status == (int)AttachmentStatus.Archived, ct);

            if (attachment == null)
                return false;

            attachment.Status = (int)AttachmentStatus.Active;
            attachment.SetZamanLastEdit(DateTime.Now);

            await context.SaveChangesAsync(ct);

            return true;
        }

        // ============================================
        // آمار و گزارش
        // ============================================

        public async Task<StorageStatistics> GetStorageStatisticsAsync(
            long? customerId = null,
            long? shobeId = null,
            CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var query = context.tblAttachments.AsQueryable();

            if (customerId != null)
                query = query.Where(a => a.tblCustomerId == customerId.Value);

            if (shobeId != null)
                query = query.Where(a => a.tblShobeId == shobeId.Value);

            var attachments = await query.ToListAsync(ct);

            return new StorageStatistics
            {
                TotalFiles = attachments.Count,
                TotalSize = attachments.Sum(a => a.FileSize),
                ActiveFiles = attachments.Count(a => a.Status == (int)AttachmentStatus.Active),
                ArchivedFiles = attachments.Count(a => a.Status == (int)AttachmentStatus.Archived),
                DeletedFiles = attachments.Count(a => a.Status == (int)AttachmentStatus.Deleted),
                EncryptedFiles = attachments.Count(a => a.IsEncrypted),
                FilesByType = attachments
                    .GroupBy(a => ((AttachmentType)a.AttachmentType).ToString())
                    .ToDictionary(g => g.Key, g => (long)g.Count()),
                SizeByType = attachments
                    .GroupBy(a => ((AttachmentType)a.AttachmentType).ToString())
                    .ToDictionary(g => g.Key, g => g.Sum(a => a.FileSize))
            };
        }

        public async Task<IEnumerable<tblAttachment>> GetExpiredFilesAsync(CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var now = BaseEntity.GetNowPersian();

            return await context.tblAttachments
                .Where(a => a.ExpiresAt != null && 
                           a.Status == (int)AttachmentStatus.Active &&
                           string.Compare(a.ExpiresAt, now) < 0)
                .ToListAsync(ct);
        }

        public async Task<int> CleanupExpiredFilesAsync(CancellationToken ct = default)
        {
            var expiredFiles = await GetExpiredFilesAsync(ct);
            var count = 0;

            foreach (var file in expiredFiles)
            {
                var deleted = await SoftDeleteAsync(file.PublicId, null, null, ct);
                if (deleted) count++;
            }

            _logger?.LogInformation("Cleaned up {Count} expired files", count);

            return count;
        }
    }
}

