using System;
using System.Threading;
using System.Threading.Tasks;
using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// پیاده‌سازی سرویس قفل حساب کاربری
    /// پیاده‌سازی الزامات پروفایل حفاظتی برنامه‌های کاربردی تحت شبکه (ISO 15408)
    /// </summary>
    public class AccountLockoutService : IAccountLockoutService
    {
        private readonly IMemoryCache _cache;
        private readonly IAuditLogService _auditLogService;
        private readonly AccountLockoutSettings _settings;
        private readonly ILogger<AccountLockoutService> _logger;

        private const string FailedAttemptsKeyPrefix = "FailedAttempts_";
        private const string LockoutEndKeyPrefix = "LockoutEnd_";
        private const string PermanentLockoutKeyPrefix = "PermanentLockout_";

        public AccountLockoutService(
            IMemoryCache cache,
            IAuditLogService auditLogService,
            IConfiguration configuration,
            ILogger<AccountLockoutService> logger)
        {
            _cache = cache;
            _auditLogService = auditLogService;
            _logger = logger;
            
            _settings = new AccountLockoutSettings();
            var section = configuration.GetSection("AccountLockout");
            if (section.Exists())
            {
                _settings.MaxFailedAttempts = section.GetValue("MaxFailedAttempts", 5);
                _settings.LockoutDurationMinutes = section.GetValue("LockoutDurationMinutes", 15);
                _settings.FailedAttemptResetMinutes = section.GetValue("FailedAttemptResetMinutes", 30);
                _settings.EnablePermanentLockout = section.GetValue("EnablePermanentLockout", true);
                _settings.PermanentLockoutThreshold = section.GetValue("PermanentLockoutThreshold", 10);
            }
        }

        /// <summary>
        /// بررسی وضعیت قفل حساب کاربر
        /// </summary>
        public Task<LockoutStatus> GetLockoutStatusAsync(string username)
        {
            var normalizedUsername = NormalizeUsername(username);
            var status = new LockoutStatus();

            // بررسی قفل دائمی
            if (_cache.TryGetValue(PermanentLockoutKeyPrefix + normalizedUsername, out bool isPermanentlyLocked) && isPermanentlyLocked)
            {
                status.IsLockedOut = true;
                status.FailedAttempts = _settings.PermanentLockoutThreshold;
                status.RemainingAttempts = 0;
                status.LockoutEndTime = null;
                return Task.FromResult(status);
            }

            // بررسی قفل موقت
            if (_cache.TryGetValue(LockoutEndKeyPrefix + normalizedUsername, out DateTime lockoutEnd))
            {
                if (lockoutEnd > DateTime.UtcNow)
                {
                    status.IsLockedOut = true;
                    status.LockoutEndTime = lockoutEnd;
                    status.RemainingLockoutSeconds = (int)(lockoutEnd - DateTime.UtcNow).TotalSeconds;
                    status.FailedAttempts = _settings.MaxFailedAttempts;
                    status.RemainingAttempts = 0;
                    return Task.FromResult(status);
                }
                else
                {
                    _cache.Remove(LockoutEndKeyPrefix + normalizedUsername);
                    _cache.Remove(FailedAttemptsKeyPrefix + normalizedUsername);
                }
            }

            // دریافت تعداد تلاش‌های ناموفق
            var failedAttempts = 0;
            if (_cache.TryGetValue(FailedAttemptsKeyPrefix + normalizedUsername, out int attempts))
            {
                failedAttempts = attempts;
            }

            status.IsLockedOut = false;
            status.FailedAttempts = failedAttempts;
            status.RemainingAttempts = Math.Max(0, _settings.MaxFailedAttempts - failedAttempts);

            return Task.FromResult(status);
        }

        /// <summary>
        /// ثبت تلاش ورود ناموفق
        /// </summary>
        public async Task<LockoutStatus> RecordFailedAttemptAsync(
            string username,
            string? ipAddress = null,
            CancellationToken ct = default)
        {
            var normalizedUsername = NormalizeUsername(username);

            var currentStatus = await GetLockoutStatusAsync(username);
            if (currentStatus.IsLockedOut)
            {
                return currentStatus;
            }

            // افزایش شمارنده تلاش‌های ناموفق
            var failedAttempts = currentStatus.FailedAttempts + 1;

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(_settings.FailedAttemptResetMinutes));

            _cache.Set(FailedAttemptsKeyPrefix + normalizedUsername, failedAttempts, cacheOptions);

            _logger.LogWarning(
                "Failed login attempt {AttemptNumber}/{MaxAttempts} for user {Username} from IP {IP}",
                failedAttempts,
                _settings.MaxFailedAttempts,
                username,
                ipAddress ?? "Unknown");

            // ثبت در Audit Log
            await _auditLogService.LogEventAsync(
                eventType: "FailedLoginAttempt",
                entityType: "User",
                entityId: username,
                isSuccess: false,
                errorMessage: $"تلاش ورود ناموفق شماره {failedAttempts}",
                ipAddress: ipAddress,
                userName: username,
                description: $"تلاش ورود ناموفق {failedAttempts} از {_settings.MaxFailedAttempts}",
                ct: ct);

            // بررسی رسیدن به حد آستانه قفل
            if (failedAttempts >= _settings.MaxFailedAttempts)
            {
                // بررسی قفل دائمی
                if (_settings.EnablePermanentLockout && failedAttempts >= _settings.PermanentLockoutThreshold)
                {
                    _cache.Set(PermanentLockoutKeyPrefix + normalizedUsername, true);

                    _logger.LogError(
                        "Account {Username} has been PERMANENTLY locked after {Attempts} failed attempts",
                        username,
                        failedAttempts);

                    await _auditLogService.LogEventAsync(
                        eventType: "AccountPermanentlyLocked",
                        entityType: "User",
                        entityId: username,
                        isSuccess: false,
                        errorMessage: $"حساب کاربری به صورت دائمی قفل شد پس از {failedAttempts} تلاش ناموفق",
                        ipAddress: ipAddress,
                        userName: username,
                        description: "حساب کاربری به صورت دائمی قفل شد",
                        ct: ct);

                    return new LockoutStatus
                    {
                        IsLockedOut = true,
                        FailedAttempts = failedAttempts,
                        RemainingAttempts = 0,
                        LockoutEndTime = null
                    };
                }

                // قفل موقت
                var lockoutEnd = DateTime.UtcNow.AddMinutes(_settings.LockoutDurationMinutes);
                _cache.Set(LockoutEndKeyPrefix + normalizedUsername, lockoutEnd,
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(lockoutEnd));

                _logger.LogWarning(
                    "Account {Username} has been locked until {LockoutEnd} after {Attempts} failed attempts",
                    username,
                    lockoutEnd,
                    failedAttempts);

                await _auditLogService.LogEventAsync(
                    eventType: "AccountLocked",
                    entityType: "User",
                    entityId: username,
                    isSuccess: false,
                    errorMessage: $"حساب کاربری قفل شد تا {lockoutEnd:yyyy-MM-dd HH:mm:ss}",
                    ipAddress: ipAddress,
                    userName: username,
                    description: $"حساب کاربری به مدت {_settings.LockoutDurationMinutes} دقیقه قفل شد",
                    ct: ct);

                return new LockoutStatus
                {
                    IsLockedOut = true,
                    FailedAttempts = failedAttempts,
                    RemainingAttempts = 0,
                    LockoutEndTime = lockoutEnd,
                    RemainingLockoutSeconds = (int)(lockoutEnd - DateTime.UtcNow).TotalSeconds
                };
            }

            return new LockoutStatus
            {
                IsLockedOut = false,
                FailedAttempts = failedAttempts,
                RemainingAttempts = _settings.MaxFailedAttempts - failedAttempts
            };
        }

        /// <summary>
        /// ریست کردن شمارنده تلاش‌های ناموفق
        /// </summary>
        public Task ResetFailedAttemptsAsync(string username, CancellationToken ct = default)
        {
            var normalizedUsername = NormalizeUsername(username);
            _cache.Remove(FailedAttemptsKeyPrefix + normalizedUsername);
            _cache.Remove(LockoutEndKeyPrefix + normalizedUsername);
            return Task.CompletedTask;
        }

        /// <summary>
        /// باز کردن قفل حساب (توسط مدیر)
        /// </summary>
        public async Task UnlockAccountAsync(
            string username,
            string adminUsername,
            CancellationToken ct = default)
        {
            var normalizedUsername = NormalizeUsername(username);
            _cache.Remove(FailedAttemptsKeyPrefix + normalizedUsername);
            _cache.Remove(LockoutEndKeyPrefix + normalizedUsername);
            _cache.Remove(PermanentLockoutKeyPrefix + normalizedUsername);

            _logger.LogInformation(
                "Account {Username} has been unlocked by admin {AdminUsername}",
                username,
                adminUsername);

            await _auditLogService.LogEventAsync(
                eventType: "AccountUnlocked",
                entityType: "User",
                entityId: username,
                isSuccess: true,
                userName: adminUsername,
                description: $"حساب کاربری {username} توسط مدیر {adminUsername} باز شد",
                ct: ct);
        }

        /// <summary>
        /// دریافت تنظیمات قفل حساب
        /// </summary>
        public AccountLockoutSettings GetSettings() => _settings;

        private string NormalizeUsername(string username)
        {
            return username?.Trim().ToLowerInvariant() ?? string.Empty;
        }
    }
}
