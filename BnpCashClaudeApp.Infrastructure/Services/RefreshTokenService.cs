using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Entities.SecuritySubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly NavigationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuditLogService _auditLogService;
        private readonly ISecuritySettingsService _securitySettingsService;
        private readonly ISecureMemoryService _secureMemoryService;
        private readonly ILogger<RefreshTokenService> _logger;
        private readonly int _refreshTokenExpiryDays;

        /// <summary>
        /// بازه زمانی (ثانیه) که استفاده مجدد از Refresh Token به عنوان Race Condition
        /// (تب‌های همزمان) در نظر گرفته می‌شود و نه حمله امنیتی.
        /// </summary>
        private const int ReuseGracePeriodSeconds = 10;

        public RefreshTokenService(
            NavigationDbContext context,
            IConfiguration configuration,
            IAuditLogService auditLogService,
            ISecuritySettingsService securitySettingsService,
            ISecureMemoryService secureMemoryService,
            ILogger<RefreshTokenService> logger)
        {
            _context = context;
            _configuration = configuration;
            _auditLogService = auditLogService;
            _securitySettingsService = securitySettingsService;
            _secureMemoryService = secureMemoryService;
            _logger = logger;
            _refreshTokenExpiryDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpiryDays", 7);
        }

        /// <summary>
        /// تولید توکن امن با پاکسازی آرایه بایت از حافظه (FDP_RIP.2)
        /// </summary>
        private string GenerateSecureToken()
        {
            var randomBytes = new byte[64];
            try
            {
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }
                return Convert.ToBase64String(randomBytes);
            }
            finally
            {
                // ============================================
                // پاکسازی آرایه بایت از حافظه (FDP_RIP.2)
                // ============================================
                _secureMemoryService.ClearBytes(randomBytes);
            }
        }

        public async Task<string> GenerateRefreshTokenAsync(
            long userId,
            string ipAddress,
            string userAgent,
            string operatingSystem,
            CancellationToken ct = default)
        {
            // ============================================
            // FTA_MCS.1 - محدودیت نشست همزمان
            // ============================================
            var maxConcurrentSessions = await _securitySettingsService.GetIntSettingAsync(
                "MaxConcurrentSessions",
                3, // پیش‌فرض: 3 نشست همزمان
                ct);

            // شمارش نشست‌های فعال کاربر
            var activeSessions = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && !rt.IsUsed && rt.ExpiresAt > DateTime.UtcNow)
                .OrderBy(rt => rt.ZamanInsert)
                .ToListAsync(ct);

            // اگر تعداد نشست‌ها از حد مجاز بیشتر است
            if (activeSessions.Count >= maxConcurrentSessions)
            {
                // باطل کردن قدیمی‌ترین نشست‌ها
                var sessionsToRevoke = activeSessions.Take(activeSessions.Count - maxConcurrentSessions + 1);
                foreach (var session in sessionsToRevoke)
                {
                    session.IsRevoked = true;
                    session.RevokedAt = DateTime.UtcNow;
                    session.RevokedReason = $"محدودیت نشست همزمان - حداکثر {maxConcurrentSessions} نشست";
                    session.ZamanLastEdit = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                }

                await _auditLogService.LogEventAsync(
                    eventType: "SessionLimit",
                    entityType: "RefreshToken",
                    entityId: userId.ToString(),
                    isSuccess: true,
                    ipAddress: ipAddress,
                    userId: userId,
                    description: $"باطل کردن {sessionsToRevoke.Count()} نشست قدیمی - محدودیت همزمان: {maxConcurrentSessions}",
                    ct: ct);
            }

            var token = GenerateSecureToken();

            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                OperatingSystem = operatingSystem,
                IsRevoked = false,
                IsUsed = false,
                ZamanInsert = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                // Use real actor identity instead of hardcoded value.
                TblUserGrpIdInsert = userId
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync(ct);

            await _auditLogService.LogEventAsync(
                eventType: "RefreshToken",
                entityType: "RefreshToken",
                entityId: refreshToken.Id.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userId: userId,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"ایجاد Refresh Token جدید - انقضا: {_refreshTokenExpiryDays} روز - نشست‌های فعال: {activeSessions.Count + 1}/{maxConcurrentSessions}",
                ct: ct);

            return token;
        }

        /// <summary>
        /// اعتبارسنجی و استفاده از Refresh Token
        /// پیاده‌سازی الزام FDP_RIP.2 - پاکسازی توکن از حافظه پس از استفاده
        /// Grace Period: برای جلوگیری از Revoke شدن تمام توکن‌ها در Race Condition تب‌های همزمان
        /// </summary>
        public async Task<RefreshTokenValidationResult> ValidateAndUseRefreshTokenAsync(
            string token,
            string ipAddress,
            CancellationToken ct = default)
        {
            try
            {
                var refreshToken = await _context.RefreshTokens
                    .Where(rt => rt.Token == token)
                    .FirstOrDefaultAsync(ct);

                if (refreshToken == null)
                {
                    await _auditLogService.LogEventAsync(
                        eventType: "RefreshToken",
                        entityType: "RefreshToken",
                        entityId: null,
                        isSuccess: false,
                        errorMessage: "Refresh Token یافت نشد",
                        ipAddress: ipAddress,
                        description: "تلاش برای استفاده از Refresh Token نامعتبر",
                        ct: ct);

                    return RefreshTokenValidationResult.Failed(
                        RefreshTokenValidationStatus.NotFound,
                        "Refresh Token یافت نشد");
                }

                if (refreshToken.IsUsed)
                {
                    var secondsSinceUsed = refreshToken.UsedAt.HasValue
                        ? (DateTime.UtcNow - refreshToken.UsedAt.Value).TotalSeconds
                        : double.MaxValue;

                    if (secondsSinceUsed <= ReuseGracePeriodSeconds)
                    {
                        _logger.LogWarning(
                            "Refresh token reuse within grace period ({Seconds:F1}s) for user {UserId} from {IP} - likely concurrent tab/request",
                            secondsSinceUsed, refreshToken.UserId, ipAddress);

                        await _auditLogService.LogEventAsync(
                            eventType: "RefreshToken_RaceCondition",
                            entityType: "RefreshToken",
                            entityId: refreshToken.Id.ToString(),
                            isSuccess: false,
                            errorMessage: $"استفاده مجدد در بازه Grace Period ({secondsSinceUsed:F1} ثانیه) - Race Condition تب‌های همزمان",
                            ipAddress: ipAddress,
                            userId: refreshToken.UserId,
                            description: "درخواست رد شد اما توکن‌های کاربر باطل نشدند",
                            ct: ct);

                        return RefreshTokenValidationResult.Failed(
                            RefreshTokenValidationStatus.RaceCondition,
                            "نشست در حال به‌روزرسانی توسط درخواست دیگری است");
                    }

                    _logger.LogError(
                        "SECURITY: Refresh token reuse detected ({Seconds:F1}s after use) for user {UserId} from {IP} - possible token theft!",
                        secondsSinceUsed, refreshToken.UserId, ipAddress);

                    await RevokeAllUserTokensAsync(
                        refreshToken.UserId,
                        "Refresh token reuse detected - possible attack",
                        ct);

                    await _auditLogService.LogEventAsync(
                        eventType: "SECURITY_ALERT",
                        entityType: "RefreshToken",
                        entityId: refreshToken.Id.ToString(),
                        isSuccess: false,
                        errorMessage: $"استفاده مجدد از Refresh Token ({secondsSinceUsed:F1} ثانیه پس از استفاده) - احتمال حمله!",
                        ipAddress: ipAddress,
                        userId: refreshToken.UserId,
                        description: "تمام توکن‌های کاربر باطل شد",
                        ct: ct);

                    return RefreshTokenValidationResult.Failed(
                        RefreshTokenValidationStatus.SecurityAlert,
                        "Refresh Token نامعتبر است. لطفاً دوباره لاگین کنید.");
                }

                if (!refreshToken.IsActive)
                {
                    await _auditLogService.LogEventAsync(
                        eventType: "RefreshToken",
                        entityType: "RefreshToken",
                        entityId: refreshToken.Id.ToString(),
                        isSuccess: false,
                        errorMessage: $"Refresh Token غیرفعال - IsRevoked: {refreshToken.IsRevoked}, Expired: {refreshToken.ExpiresAt < DateTime.UtcNow}",
                        ipAddress: ipAddress,
                        userId: refreshToken.UserId,
                        description: "تلاش برای استفاده از Refresh Token غیرفعال",
                        ct: ct);

                    return RefreshTokenValidationResult.Failed(
                        RefreshTokenValidationStatus.Inactive,
                        "Refresh Token منقضی یا باطل شده است");
                }

                refreshToken.IsUsed = true;
                refreshToken.UsedAt = DateTime.UtcNow;
                refreshToken.ZamanLastEdit = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                await _context.SaveChangesAsync(ct);

                await _auditLogService.LogEventAsync(
                    eventType: "RefreshToken",
                    entityType: "RefreshToken",
                    entityId: refreshToken.Id.ToString(),
                    isSuccess: true,
                    ipAddress: ipAddress,
                    userId: refreshToken.UserId,
                    description: "استفاده موفق از Refresh Token",
                    ct: ct);

                return RefreshTokenValidationResult.Success(refreshToken.UserId);
            }
            finally
            {
                // ============================================
                // پاکسازی توکن از حافظه (FDP_RIP.2)
                // ============================================
                var tokenCopy = token;
                _secureMemoryService.ClearString(ref tokenCopy);
            }
        }

        public async Task RevokeAllUserTokensAsync(
            long userId,
            string reason,
            CancellationToken ct = default)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync(ct);

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = reason;
                token.ZamanLastEdit = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            }

            await _context.SaveChangesAsync(ct);

            await _auditLogService.LogEventAsync(
                eventType: "RefreshToken",
                entityType: "RefreshToken",
                entityId: userId.ToString(),
                isSuccess: true,
                userId: userId,
                description: $"باطل کردن {activeTokens.Count} Refresh Token - دلیل: {reason}",
                ct: ct);
        }

        public async Task RevokeTokenAsync(
            string token,
            string reason,
            CancellationToken ct = default)
        {
            var refreshToken = await _context.RefreshTokens
                .Where(rt => rt.Token == token)
                .FirstOrDefaultAsync(ct);

            if (refreshToken == null || refreshToken.IsRevoked)
                return;

            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedReason = reason;
            refreshToken.ZamanLastEdit = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            await _context.SaveChangesAsync(ct);
        }

        public async Task CleanupExpiredTokensAsync(CancellationToken ct = default)
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiresAt < DateTime.UtcNow || (rt.IsUsed && rt.UsedAt < DateTime.UtcNow.AddDays(-1)))
                .ToListAsync(ct);

            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync(ct);

            if (expiredTokens.Count > 0)
            {
                await _auditLogService.LogEventAsync(
                    eventType: "RefreshToken",
                    entityType: "RefreshToken",
                    entityId: null,
                    isSuccess: true,
                    description: $"پاک کردن {expiredTokens.Count} Refresh Token منقضی شده",
                    ct: ct);
            }
        }

        public async Task<List<UserSessionDto>> GetUserActiveSessionsAsync(
            long userId,
            string? currentToken = null,
            CancellationToken ct = default)
        {
            var activeSessions = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && !rt.IsUsed && rt.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(rt => rt.ZamanInsert)
                .ToListAsync(ct);

            var sessionDtos = activeSessions.Select((session, index) => new UserSessionDto
            {
                PublicId = session.PublicId,
                IpAddress = session.IpAddress,
                UserAgent = session.UserAgent,
                OperatingSystem = session.OperatingSystem ?? "نامشخص",
                CreatedAt = session.ZamanInsert,
                ExpiresAt = session.ExpiresAt,
                IsCurrentSession = !string.IsNullOrEmpty(currentToken) && session.Token == currentToken,
                IsLastLogin = index == 0, // اولین نشست (جدیدترین) آخرین لاگین است
                BrowserName = ExtractBrowserName(session.UserAgent),
                DeviceType = ExtractDeviceType(session.UserAgent)
            }).ToList();

            return sessionDtos;
        }

        public async Task RevokeSessionAsync(
            long userId,
            Guid sessionPublicId,
            string reason,
            CancellationToken ct = default)
        {
            var session = await _context.RefreshTokens
                .Where(rt => rt.PublicId == sessionPublicId && rt.UserId == userId)
                .FirstOrDefaultAsync(ct);

            if (session == null)
            {
                await _auditLogService.LogEventAsync(
                    eventType: "RevokeSession",
                    entityType: "RefreshToken",
                    entityId: sessionPublicId.ToString(),
                    isSuccess: false,
                    errorMessage: "نشست یافت نشد یا متعلق به کاربر نیست",
                    userId: userId,
                    description: $"تلاش برای باطل کردن نشست {sessionPublicId}",
                    ct: ct);
                return;
            }

            if (session.IsRevoked)
            {
                await _auditLogService.LogEventAsync(
                    eventType: "RevokeSession",
                    entityType: "RefreshToken",
                    entityId: sessionPublicId.ToString(),
                    isSuccess: false,
                    errorMessage: "نشست قبلاً باطل شده است",
                    userId: userId,
                    description: $"تلاش برای باطل کردن نشست قبلاً باطل شده {sessionPublicId}",
                    ct: ct);
                return;
            }

            session.IsRevoked = true;
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedReason = reason;
            session.ZamanLastEdit = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            await _context.SaveChangesAsync(ct);

            await _auditLogService.LogEventAsync(
                eventType: "RevokeSession",
                entityType: "RefreshToken",
                entityId: sessionPublicId.ToString(),
                isSuccess: true,
                userId: userId,
                description: $"باطل کردن نشست {sessionPublicId} - دلیل: {reason}",
                ct: ct);
        }

        private string ExtractBrowserName(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "نامشخص";

            if (userAgent.Contains("Edg/"))
                return "Microsoft Edge";
            if (userAgent.Contains("Chrome/"))
                return "Google Chrome";
            if (userAgent.Contains("Firefox/"))
                return "Mozilla Firefox";
            if (userAgent.Contains("Safari/") && !userAgent.Contains("Chrome"))
                return "Safari";
            if (userAgent.Contains("OPR/") || userAgent.Contains("Opera/"))
                return "Opera";

            return "مرورگر دیگر";
        }

        private string ExtractDeviceType(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "نامشخص";

            if (userAgent.Contains("Mobile") || userAgent.Contains("Android"))
                return "موبایل";
            if (userAgent.Contains("Tablet") || userAgent.Contains("iPad"))
                return "تبلت";

            return "دسکتاپ";
        }

        /// <summary>
        /// دریافت تعداد نشست‌های فعال کاربر
        /// الزام FTA_MCS.1.1: محدودیت نشست همزمان
        /// الزام FDP_ACF.1.4: Context-based Access Control
        /// </summary>
        public async Task<int> GetUserActiveSessionsCountAsync(
            long userId,
            CancellationToken ct = default)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && !rt.IsUsed && rt.ExpiresAt > DateTime.UtcNow)
                .CountAsync(ct);
        }

        /// <summary>
        /// خاتمه قدیمی‌ترین نشست کاربر
        /// الزام FDP_ACF.1.4: رفتار ConcurrentSessionAction.TerminateOldest
        /// </summary>
        public async Task TerminateOldestSessionAsync(
            long userId,
            CancellationToken ct = default)
        {
            var oldestSession = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && !rt.IsUsed && rt.ExpiresAt > DateTime.UtcNow)
                .OrderBy(rt => rt.ZamanInsert)
                .FirstOrDefaultAsync(ct);

            if (oldestSession != null)
            {
                oldestSession.IsRevoked = true;
                oldestSession.RevokedAt = DateTime.UtcNow;
                oldestSession.RevokedReason = "خاتمه خودکار - محدودیت نشست همزمان (TerminateOldest)";
                oldestSession.ZamanLastEdit = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

                await _context.SaveChangesAsync(ct);

                await _auditLogService.LogEventAsync(
                    eventType: "SessionTerminated",
                    entityType: "RefreshToken",
                    entityId: oldestSession.PublicId.ToString(),
                    isSuccess: true,
                    userId: userId,
                    description: $"خاتمه خودکار قدیمی‌ترین نشست - IP: {oldestSession.IpAddress}",
                    ct: ct);
            }
        }

        /// <summary>
        /// باطل کردن تمام نشست‌های کاربر (بدون دلیل)
        /// </summary>
        public async Task RevokeAllUserTokensAsync(
            long userId,
            CancellationToken ct = default)
        {
            await RevokeAllUserTokensAsync(userId, "درخواست سیستم - خاتمه تمام نشست‌ها", ct);
        }
    }
}
