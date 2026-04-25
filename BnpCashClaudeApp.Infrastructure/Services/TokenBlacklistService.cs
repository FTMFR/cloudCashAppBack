using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// پیاده‌سازی سرویس Blacklist توکن‌های JWT
    /// پیاده‌سازی الزامات پروفایل حفاظتی برنامه‌های کاربردی تحت شبکه (ISO 15408)
    /// </summary>
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly IMemoryCache _cache;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<TokenBlacklistService> _logger;

        // ============================================
        // نگهداری توکن‌های Blacklist شده هر کاربر
        // ============================================
        private readonly ConcurrentDictionary<string, DateTime> _userBlacklistTimestamps = new();

        // ============================================
        // Prefix های Cache
        // ============================================
        private const string TokenBlacklistPrefix = "BlacklistedToken_";
        private const string UserBlacklistPrefix = "UserBlacklist_";

        public TokenBlacklistService(
            IMemoryCache cache,
            IAuditLogService auditLogService,
            ILogger<TokenBlacklistService> logger)
        {
            _cache = cache;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// افزودن توکن به لیست سیاه (Logout)
        /// </summary>
        public async Task BlacklistTokenAsync(
            string token,
            DateTime expirationTime,
            string? username = null,
            string? reason = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            // ============================================
            // ایجاد Hash از توکن برای ذخیره‌سازی امن
            // ============================================
            var tokenHash = ComputeTokenHash(token);

            // ============================================
            // محاسبه مدت زمان باقی‌مانده تا انقضا
            // ============================================
            var timeToExpiry = expirationTime - DateTime.UtcNow;
            if (timeToExpiry <= TimeSpan.Zero)
            {
                _logger.LogDebug("Token already expired, skipping blacklist");
                return;
            }

            // ============================================
            // ذخیره در Cache با مدت زمان برابر با انقضای توکن
            // ============================================
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(timeToExpiry);

            _cache.Set(TokenBlacklistPrefix + tokenHash, true, cacheOptions);

            _logger.LogInformation(
                "Token blacklisted for user {Username}. Reason: {Reason}. Expires at: {ExpirationTime}",
                username ?? "Unknown",
                reason ?? "Logout",
                expirationTime);

            // ============================================
            // ثبت در Audit Log
            // ============================================
            await _auditLogService.LogEventAsync(
                eventType: "TokenBlacklisted",
                entityType: "Token",
                entityId: tokenHash.Substring(0, 16),
                isSuccess: true,
                userName: username,
                description: $"توکن کاربر {username ?? "نامشخص"} باطل شد. دلیل: {reason ?? "خروج کاربر"}",
                ct: ct);
        }

        /// <summary>
        /// بررسی اینکه آیا توکن در لیست سیاه است
        /// این متد دو نوع بررسی انجام می‌دهد:
        /// 1. بررسی اینکه آیا این توکن خاص Blacklist شده است (بر اساس Hash)
        /// 2. بررسی اینکه آیا تمام توکن‌های کاربر Blacklist شده‌اند (بر اساس زمان صدور)
        /// </summary>
        public Task<bool> IsTokenBlacklistedAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Task.FromResult(false);
            }

            // ============================================
            // بررسی 1: آیا این توکن خاص Blacklist شده است؟
            // ============================================
            var tokenHash = ComputeTokenHash(token);
            var isTokenBlacklisted = _cache.TryGetValue(TokenBlacklistPrefix + tokenHash, out _);

            if (isTokenBlacklisted)
            {
                _logger.LogDebug("Blacklisted token detected (token-level blacklist)");
                return Task.FromResult(true);
            }

            // ============================================
            // بررسی 2: آیا تمام توکن‌های کاربر Blacklist شده‌اند؟
            // این بررسی برای زمانی است که BlacklistAllUserTokensAsync فراخوانی شده باشد
            // ============================================
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                
                // بررسی اینکه آیا می‌توان توکن را خواند (بدون اعتبارسنجی signature)
                if (tokenHandler.CanReadToken(token))
                {
                    var jwtToken = tokenHandler.ReadJwtToken(token);
                    
                    // استخراج username از Claim
                    var username = jwtToken.Claims.FirstOrDefault(c => 
                        c.Type == ClaimTypes.Name || 
                        c.Type == "name" || 
                        c.Type == JwtRegisteredClaimNames.Sub)?.Value;
                    
                    // استخراج Iat (Issued At) از Claim
                    var iatClaim = jwtToken.Claims.FirstOrDefault(c => 
                        c.Type == JwtRegisteredClaimNames.Iat)?.Value;
                    
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(iatClaim))
                    {
                        // تبدیل Iat از Unix timestamp به DateTime
                        if (long.TryParse(iatClaim, out var iatUnix))
                        {
                            var tokenIssuedAt = DateTimeOffset.FromUnixTimeSeconds(iatUnix).UtcDateTime;
                            
                            // بررسی اعتبار توکن بر اساس زمان صدور
                            if (!IsUserTokenValid(username, tokenIssuedAt))
                            {
                                _logger.LogDebug(
                                    "Token invalidated by user-level blacklist. User: {Username}, IssuedAt: {IssuedAt}",
                                    username,
                                    tokenIssuedAt);
                                return Task.FromResult(true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // در صورت خطا در decode کردن توکن، فقط نتیجه بررسی hash را برمی‌گردانیم
                // این خطا ممکن است برای توکن‌های نامعتبر یا فرمت‌های غیر JWT رخ دهد
                _logger.LogWarning(
                    ex,
                    "Failed to decode token for user-level blacklist check. Falling back to token-level check only.");
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// باطل کردن تمام توکن‌های یک کاربر (Logout from all devices)
        /// </summary>
        public async Task BlacklistAllUserTokensAsync(
            string username,
            string? reason = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            // ============================================
            // ثبت زمان Blacklist برای این کاربر
            // ============================================
            var blacklistTime = DateTime.UtcNow;
            _userBlacklistTimestamps.AddOrUpdate(
                username.ToLowerInvariant(),
                blacklistTime,
                (key, oldValue) => blacklistTime);

            // ============================================
            // ذخیره در Cache
            // ============================================
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromDays(1));

            _cache.Set(UserBlacklistPrefix + username.ToLowerInvariant(), blacklistTime, cacheOptions);

            _logger.LogWarning(
                "All tokens for user {Username} have been blacklisted. Reason: {Reason}",
                username,
                reason ?? "Logout from all devices");

            // ============================================
            // ثبت در Audit Log
            // ============================================
            await _auditLogService.LogEventAsync(
                eventType: "AllTokensBlacklisted",
                entityType: "User",
                entityId: username,
                isSuccess: true,
                userName: username,
                description: $"تمام توکن‌های کاربر {username} باطل شدند. دلیل: {reason ?? "خروج از همه دستگاه‌ها"}",
                ct: ct);
        }

        /// <summary>
        /// بررسی اینکه آیا توکن یک کاربر معتبر است (بر اساس زمان صدور)
        /// </summary>
        public bool IsUserTokenValid(string username, DateTime tokenIssuedAt)
        {
            if (string.IsNullOrEmpty(username))
            {
                return true;
            }

            if (_cache.TryGetValue(UserBlacklistPrefix + username.ToLowerInvariant(), out DateTime blacklistTime))
            {
                if (tokenIssuedAt < blacklistTime)
                {
                    _logger.LogDebug(
                        "Token for user {Username} issued at {IssuedAt} is invalid (blacklisted at {BlacklistTime})",
                        username,
                        tokenIssuedAt,
                        blacklistTime);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// پاکسازی توکن‌های منقضی شده از Blacklist
        /// </summary>
        public Task CleanupExpiredTokensAsync(CancellationToken ct = default)
        {
            _logger.LogDebug("Cleanup triggered. MemoryCache handles expiration automatically.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// محاسبه Hash از توکن
        /// </summary>
        private string ComputeTokenHash(string token)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(token);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}

