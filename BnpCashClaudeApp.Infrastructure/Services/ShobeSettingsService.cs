using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// سرویس مدیریت تنظیمات شعبه از دیتابیس
    /// تنظیمات با Caching مدیریت می‌شوند برای کارایی بهتر
    /// </summary>
    public class ShobeSettingsService : IShobeSettingsService
    {
        private readonly NavigationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ShobeSettingsService> _logger;

        private const string SmsSettingsKeyPrefix = "SmsSettings";
        private const string SmsSettingKey = "SmsSettings";

        public ShobeSettingsService(
            NavigationDbContext context,
            IMemoryCache cache,
            ILogger<ShobeSettingsService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// دریافت تنظیمات SMS
        /// </summary>
        public async Task<SmsSettings> GetSmsSettingsAsync(Guid? shobePublicId = null, CancellationToken ct = default)
        {
            var cacheKey = GetCacheKey(SmsSettingsKeyPrefix, shobePublicId);

            // ============================================
            // ابتدا از Cache می‌خوانیم
            // ============================================
            if (_cache.TryGetValue(cacheKey, out SmsSettings? cachedSettings) && cachedSettings != null)
            {
                _logger.LogDebug("SMS settings loaded from cache for Shobe: {ShobeId}", shobePublicId?.ToString() ?? "Global");
                return cachedSettings;
            }

            // ============================================
            // خواندن از دیتابیس
            // ============================================
            long? tblShobeId = null;
            if (shobePublicId.HasValue)
            {
                var shobe = await _context.tblShobes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.PublicId == shobePublicId.Value, ct);
                
                if (shobe != null)
                {
                    tblShobeId = shobe.Id;
                }
            }

            var setting = await _context.tblShobeSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => 
                    s.SettingKey == SmsSettingKey && 
                    s.TblShobeId == tblShobeId && 
                    s.IsActive, ct);

            SmsSettings settings;

            if (setting != null && !string.IsNullOrWhiteSpace(setting.SettingValue))
            {
                try
                {
                    settings = JsonSerializer.Deserialize<SmsSettings>(setting.SettingValue)
                        ?? GetDefaultSmsSettings();

                    _logger.LogDebug("SMS settings loaded from database for Shobe: {ShobeId}", shobePublicId?.ToString() ?? "Global");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing SMS settings, using defaults");
                    settings = GetDefaultSmsSettings();
                }
            }
            else
            {
                // اگر تنظیمات برای شعبه خاص یافت نشد، تنظیمات عمومی را بررسی می‌کنیم
                if (shobePublicId.HasValue)
                {
                    _logger.LogDebug("SMS settings not found for Shobe {ShobeId}, trying global settings", shobePublicId);
                    return await GetSmsSettingsAsync(null, ct);
                }

                _logger.LogWarning("SMS settings not found in database, using defaults");
                settings = GetDefaultSmsSettings();
            }

            // ============================================
            // ذخیره در Cache
            // ============================================
            _cache.Set(cacheKey, settings, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) // Cache برای 30 دقیقه
            });

            return settings;
        }

        /// <summary>
        /// دریافت مقدار یک تنظیم SMS خاص
        /// </summary>
        public async Task<string> GetSmsSettingValueAsync(string settingKey, string defaultValue, Guid? shobePublicId = null, CancellationToken ct = default)
        {
            var settings = await GetSmsSettingsAsync(shobePublicId, ct);
            
            return settingKey switch
            {
                "BaseUrl" => settings.BaseUrl,
                "ApiKey" => settings.ApiKey,
                "SenderNumber" => settings.SenderNumber,
                "MessageTemplate" => settings.MessageTemplate,
                _ => defaultValue
            };
        }

        /// <summary>
        /// دریافت مقدار یک تنظیم SMS عددی
        /// </summary>
        public async Task<int> GetSmsIntSettingValueAsync(string settingKey, int defaultValue, Guid? shobePublicId = null, CancellationToken ct = default)
        {
            var settings = await GetSmsSettingsAsync(shobePublicId, ct);
            
            return settingKey switch
            {
                "OtpLength" => settings.OtpLength,
                "OtpExpirySeconds" => settings.OtpExpirySeconds,
                _ => defaultValue
            };
        }

        /// <summary>
        /// پاکسازی کش تنظیمات SMS
        /// </summary>
        public void InvalidateCache(Guid? shobePublicId = null)
        {
            var cacheKey = GetCacheKey(SmsSettingsKeyPrefix, shobePublicId);
            _cache.Remove(cacheKey);
            _logger.LogDebug("SMS settings cache invalidated for Shobe: {ShobeId}", shobePublicId?.ToString() ?? "Global");
        }

        /// <summary>
        /// دریافت کلید Cache
        /// </summary>
        private string GetCacheKey(string prefix, Guid? shobePublicId)
        {
            return shobePublicId.HasValue 
                ? $"{prefix}_{shobePublicId.Value}" 
                : $"{prefix}_Global";
        }

        /// <summary>
        /// دریافت تنظیمات پیش‌فرض SMS
        /// </summary>
        private SmsSettings GetDefaultSmsSettings()
        {
            return new SmsSettings
            {
                BaseUrl = "http://ssmss.ir/webservice/rest/sms_send",
                // SECURITY HARDENING: never keep provider API keys hardcoded in source.
                ApiKey = string.Empty,
                SenderNumber = "10001425",
                OtpLength = 6,
                OtpExpirySeconds = 120,
                MessageTemplate = "کد تایید شما: {0}\nاین کد تا {1} ثانیه معتبر است."
            };
        }

        // ============================================
        // Attachment Settings
        // ============================================

        private const string AttachmentSettingsKeyPrefix = "AttachmentSettings";
        private const string AttachmentSettingKey = "AttachmentSettings";

        /// <summary>
        /// دریافت تنظیمات Attachment
        /// </summary>
        public async Task<AttachmentSettingsDto> GetAttachmentSettingsAsync(Guid? shobePublicId = null, CancellationToken ct = default)
        {
            var cacheKey = GetCacheKey(AttachmentSettingsKeyPrefix, shobePublicId);

            // ابتدا از Cache می‌خوانیم
            if (_cache.TryGetValue(cacheKey, out AttachmentSettingsDto? cachedSettings) && cachedSettings != null)
            {
                _logger.LogDebug("Attachment settings loaded from cache for Shobe: {ShobeId}", shobePublicId?.ToString() ?? "Global");
                return cachedSettings;
            }

            // خواندن از دیتابیس
            long? tblShobeId = null;
            if (shobePublicId.HasValue)
            {
                var shobe = await _context.tblShobes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.PublicId == shobePublicId.Value, ct);
                
                if (shobe != null)
                {
                    tblShobeId = shobe.Id;
                }
            }

            var setting = await _context.tblShobeSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => 
                    s.SettingKey == AttachmentSettingKey && 
                    s.TblShobeId == tblShobeId && 
                    s.IsActive, ct);

            AttachmentSettingsDto settings;

            if (setting != null && !string.IsNullOrWhiteSpace(setting.SettingValue))
            {
                try
                {
                    settings = JsonSerializer.Deserialize<AttachmentSettingsDto>(setting.SettingValue)
                        ?? GetDefaultAttachmentSettings();

                    _logger.LogDebug("Attachment settings loaded from database for Shobe: {ShobeId}", shobePublicId?.ToString() ?? "Global");
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing Attachment settings, using defaults");
                    settings = GetDefaultAttachmentSettings();
                }
            }
            else
            {
                // اگر تنظیمات برای شعبه خاص یافت نشد، تنظیمات عمومی را بررسی می‌کنیم
                if (shobePublicId.HasValue)
                {
                    _logger.LogDebug("Attachment settings not found for Shobe {ShobeId}, trying global settings", shobePublicId);
                    return await GetAttachmentSettingsAsync(null, ct);
                }

                _logger.LogDebug("Attachment settings not found in database, using defaults");
                settings = GetDefaultAttachmentSettings();
            }

            // ذخیره در Cache
            _cache.Set(cacheKey, settings, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

            return settings;
        }

        /// <summary>
        /// ذخیره تنظیمات Attachment
        /// </summary>
        public async Task<bool> SaveAttachmentSettingsAsync(AttachmentSettingsDto settings, Guid? shobePublicId = null, long? userId = null, CancellationToken ct = default)
        {
            try
            {
                long? tblShobeId = null;
                if (shobePublicId.HasValue)
                {
                    var shobe = await _context.tblShobes
                        .FirstOrDefaultAsync(s => s.PublicId == shobePublicId.Value, ct);
                    
                    if (shobe != null)
                    {
                        tblShobeId = shobe.Id;
                    }
                }

                var existingSetting = await _context.tblShobeSettings
                    .FirstOrDefaultAsync(s => 
                        s.SettingKey == AttachmentSettingKey && 
                        s.TblShobeId == tblShobeId, ct);

                var jsonValue = JsonSerializer.Serialize(settings);

                if (existingSetting != null)
                {
                    existingSetting.SettingValue = jsonValue;
                    existingSetting.TblUserGrpIdLastEdit = userId ?? 0;
                    existingSetting.SetZamanLastEdit(DateTime.Now);
                }
                else
                {
                    var newSetting = new tblShobeSetting
                    {
                        TblShobeId = tblShobeId,
                        SettingKey = AttachmentSettingKey,
                        SettingName = "تنظیمات فایل‌های پیوست",
                        Description = "تنظیمات مربوط به آپلود و مدیریت فایل‌های پیوست",
                        SettingValue = jsonValue,
                        SettingType = ShobeSettingType.Attachment,
                        TblUserGrpIdInsert = userId ?? 0
                    };
                    newSetting.SetZamanInsert(DateTime.Now);

                    _context.tblShobeSettings.Add(newSetting);
                }

                await _context.SaveChangesAsync(ct);

                // پاکسازی کش
                InvalidateAttachmentCache(shobePublicId);

                _logger.LogInformation("Attachment settings saved for Shobe: {ShobeId}", shobePublicId?.ToString() ?? "Global");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Attachment settings");
                return false;
            }
        }

        /// <summary>
        /// پاکسازی کش تنظیمات Attachment
        /// </summary>
        public void InvalidateAttachmentCache(Guid? shobePublicId = null)
        {
            var cacheKey = GetCacheKey(AttachmentSettingsKeyPrefix, shobePublicId);
            _cache.Remove(cacheKey);
            _logger.LogDebug("Attachment settings cache invalidated for Shobe: {ShobeId}", shobePublicId?.ToString() ?? "Global");
        }

        /// <summary>
        /// دریافت تنظیمات پیش‌فرض Attachment
        /// </summary>
        private AttachmentSettingsDto GetDefaultAttachmentSettings()
        {
            return new AttachmentSettingsDto
            {
                StoragePath = "wwwroot/attachments",
                MaxFileSizeMB = 50,
                AllowedExtensions = ".pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png,.gif,.webp",
                ValidateMagicBytes = true,
                EnableVirusScan = false,
                EnableEncryption = false,
                MaxProfileImageSizeMB = 5,
                AllowedImageExtensions = ".jpg,.jpeg,.png,.gif,.webp",
                UseWebRoot = true
            };
        }
    }
}
