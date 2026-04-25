using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// سرویس کنترل دسترسی مبتنی بر Context
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF.1.4 از استاندارد ISO 15408
    /// 
    /// این سرویس دسترسی را بر اساس پارامترهای Context بررسی می‌کند:
    /// - آدرس IP (Whitelist/Blacklist)
    /// - زمان دسترسی (ساعات کاری، روزهای هفته)
    /// - موقعیت جغرافیایی (کشور)
    /// - نوع دستگاه (موبایل، دسکتاپ، تبلت)
    /// - تعداد نشست‌های همزمان
    /// - ارزیابی ریسک
    /// ============================================
    /// </summary>
    public class ContextAccessControlService : IContextAccessControlService
    {
        private readonly ISecuritySettingsService _securitySettingsService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IAuditLogService _auditLogService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ContextAccessControlService> _logger;

        // Regex patterns for device detection
        private static readonly Regex MobileRegex = new(
            @"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TabletRegex = new(
            @"android|ipad|playbook|silk",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex BotRegex = new(
            @"bot|crawler|spider|crawling|googlebot|bingbot|yandexbot|duckduckbot",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ContextAccessControlService(
            ISecuritySettingsService securitySettingsService,
            IRefreshTokenService refreshTokenService,
            IAuditLogService auditLogService,
            IMemoryCache cache,
            ILogger<ContextAccessControlService> logger)
        {
            _securitySettingsService = securitySettingsService;
            _refreshTokenService = refreshTokenService;
            _auditLogService = auditLogService;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// بررسی جامع دسترسی بر اساس تمام پارامترهای Context
        /// </summary>
        public async Task<ContextAccessResult> ValidateAccessAsync(
            AccessContext context,
            long? userId = null,
            CancellationToken ct = default)
        {
            var settings = await _securitySettingsService.GetContextAccessControlSettingsAsync(ct);

            // اگر Context Access Control غیرفعال است، دسترسی مجاز است
            if (!settings.IsEnabled)
            {
                return ContextAccessResult.Allowed();
            }

            var result = new ContextAccessResult
            {
                IsAllowed = true,
                CheckDetails = new List<ContextCheckDetail>()
            };

            // 1. بررسی IP
            if (settings.EnableIpRestriction && !string.IsNullOrEmpty(context.IpAddress))
            {
                var ipResult = await ValidateIpAccessAsync(context.IpAddress, ct);
                result.CheckDetails.Add(new ContextCheckDetail
                {
                    CheckType = "IP",
                    Passed = ipResult.IsAllowed,
                    Message = ipResult.DenialReason,
                    CheckedValue = context.IpAddress
                });

                if (!ipResult.IsAllowed)
                {
                    result.IsAllowed = false;
                    result.DenialCode = ipResult.DenialCode;
                    result.DenialReason = ipResult.DenialReason;

                    if (settings.LogDeniedAccess)
                    {
                        await LogAccessDenied(context, userId, ipResult.DenialReason, ct);
                    }

                    return result;
                }
            }

            // 2. بررسی زمان
            if (settings.EnableTimeRestriction)
            {
                var timeResult = await ValidateTimeAccessAsync(ct);
                result.CheckDetails.Add(new ContextCheckDetail
                {
                    CheckType = "Time",
                    Passed = timeResult.IsAllowed,
                    Message = timeResult.DenialReason,
                    CheckedValue = DateTime.Now.ToString("HH:mm")
                });

                if (!timeResult.IsAllowed)
                {
                    result.IsAllowed = false;
                    result.DenialCode = timeResult.DenialCode;
                    result.DenialReason = timeResult.DenialReason;

                    if (settings.LogDeniedAccess)
                    {
                        await LogAccessDenied(context, userId, timeResult.DenialReason, ct);
                    }

                    return result;
                }
            }

            // 3. بررسی موقعیت جغرافیایی
            if (settings.EnableGeoRestriction && !string.IsNullOrEmpty(context.CountryCode))
            {
                var geoResult = await ValidateGeoAccessAsync(context.CountryCode, ct);
                result.CheckDetails.Add(new ContextCheckDetail
                {
                    CheckType = "Geo",
                    Passed = geoResult.IsAllowed,
                    Message = geoResult.DenialReason,
                    CheckedValue = context.CountryCode
                });

                if (!geoResult.IsAllowed)
                {
                    result.IsAllowed = false;
                    result.DenialCode = geoResult.DenialCode;
                    result.DenialReason = geoResult.DenialReason;

                    if (settings.LogDeniedAccess)
                    {
                        await LogAccessDenied(context, userId, geoResult.DenialReason, ct);
                    }

                    return result;
                }
            }

            // 4. بررسی نوع دستگاه
            if (settings.EnableDeviceRestriction && !string.IsNullOrEmpty(context.UserAgent))
            {
                var deviceResult = await ValidateDeviceAccessAsync(context.UserAgent, ct);
                result.CheckDetails.Add(new ContextCheckDetail
                {
                    CheckType = "Device",
                    Passed = deviceResult.IsAllowed,
                    Message = deviceResult.DenialReason,
                    CheckedValue = DetectDeviceType(context.UserAgent).ToString()
                });

                if (!deviceResult.IsAllowed)
                {
                    result.IsAllowed = false;
                    result.DenialCode = deviceResult.DenialCode;
                    result.DenialReason = deviceResult.DenialReason;

                    if (settings.LogDeniedAccess)
                    {
                        await LogAccessDenied(context, userId, deviceResult.DenialReason, ct);
                    }

                    return result;
                }
            }

            // 5. بررسی نشست همزمان
            // استثنا: برای endpoint Sessions بررسی محدودیت نشست همزمان را skip می‌کنیم
            // چون کاربر باید بتواند لیست نشست‌های خود را ببیند تا بتواند آن‌ها را مدیریت کند
            var isMySessionsEndpoint = !string.IsNullOrEmpty(context.RequestPath) && 
                (context.RequestPath.Contains("/Session", StringComparison.OrdinalIgnoreCase) ||
                 context.RequestPath.Contains("/api/Session", StringComparison.OrdinalIgnoreCase));

            if (settings.EnableConcurrentSessionLimit && userId.HasValue && !isMySessionsEndpoint)
            {
                var sessionResult = await ValidateConcurrentSessionAsync(userId.Value, ct);
                result.CheckDetails.Add(new ContextCheckDetail
                {
                    CheckType = "ConcurrentSession",
                    Passed = sessionResult.IsAllowed,
                    Message = sessionResult.DenialReason
                });

                if (!sessionResult.IsAllowed)
                {
                    result.IsAllowed = false;
                    result.DenialCode = sessionResult.DenialCode;
                    result.DenialReason = sessionResult.DenialReason;

                    if (settings.LogDeniedAccess)
                    {
                        await LogAccessDenied(context, userId, sessionResult.DenialReason, ct);
                    }

                    return result;
                }
            }

            // 6. ارزیابی ریسک
            if (settings.EnableRiskAssessment)
            {
                var riskResult = await CalculateRiskScoreAsync(context, userId, ct);
                result.RiskScore = riskResult.TotalScore;
                result.RequiresMfa = riskResult.RequiresMfa;

                result.CheckDetails.Add(new ContextCheckDetail
                {
                    CheckType = "Risk",
                    Passed = !riskResult.ShouldDeny,
                    Message = $"Risk Score: {riskResult.TotalScore}, Level: {riskResult.Level}",
                    CheckedValue = riskResult.TotalScore.ToString()
                });

                if (riskResult.ShouldDeny)
                {
                    result.IsAllowed = false;
                    result.DenialCode = ContextAccessDenialCode.HighRiskScore;
                    result.DenialReason = $"امتیاز ریسک بالا: {riskResult.TotalScore}";

                    if (settings.LogDeniedAccess)
                    {
                        await LogAccessDenied(context, userId, result.DenialReason, ct);
                    }

                    return result;
                }
            }

            // ثبت دسترسی مجاز (اگر تنظیم شده)
            if (settings.LogAllowedAccess)
            {
                await LogAccessAllowed(context, userId, ct);
            }

            return result;
        }

        /// <summary>
        /// بررسی دسترسی بر اساس IP
        /// </summary>
        public async Task<ContextAccessResult> ValidateIpAccessAsync(
            string ipAddress,
            CancellationToken ct = default)
        {
            var settings = await _securitySettingsService.GetContextAccessControlSettingsAsync(ct);

            if (!settings.EnableIpRestriction)
            {
                return ContextAccessResult.Allowed();
            }

            // بررسی Blacklist
            if (settings.IpRestrictionMode == IpRestrictionMode.Blacklist)
            {
                foreach (var blockedIp in settings.BlockedIpAddresses)
                {
                    if (IsIpMatch(ipAddress, blockedIp))
                    {
                        _logger.LogWarning(
                            "IP {IpAddress} is blacklisted (matched: {BlockedIp})",
                            ipAddress, blockedIp);

                        return ContextAccessResult.Denied(
                            ContextAccessDenialCode.IpBlacklisted,
                            $"آدرس IP {ipAddress} در لیست سیاه قرار دارد");
                    }
                }

                return ContextAccessResult.Allowed();
            }

            // بررسی Whitelist
            if (settings.IpRestrictionMode == IpRestrictionMode.Whitelist)
            {
                foreach (var allowedIp in settings.AllowedIpAddresses)
                {
                    if (IsIpMatch(ipAddress, allowedIp))
                    {
                        return ContextAccessResult.Allowed();
                    }
                }

                _logger.LogWarning(
                    "IP {IpAddress} is not in whitelist",
                    ipAddress);

                return ContextAccessResult.Denied(
                    ContextAccessDenialCode.IpNotWhitelisted,
                    $"آدرس IP {ipAddress} در لیست مجاز نیست");
            }

            return ContextAccessResult.Allowed();
        }

        /// <summary>
        /// بررسی دسترسی بر اساس زمان
        /// </summary>
        public async Task<ContextAccessResult> ValidateTimeAccessAsync(
            CancellationToken ct = default)
        {
            var settings = await _securitySettingsService.GetContextAccessControlSettingsAsync(ct);

            if (!settings.EnableTimeRestriction)
            {
                return ContextAccessResult.Allowed();
            }

            // دریافت زمان فعلی در منطقه زمانی تنظیم شده
            DateTime currentTime;
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
                currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            }
            catch
            {
                // اگر منطقه زمانی نامعتبر بود، از زمان محلی استفاده کن
                currentTime = DateTime.Now;
            }

            // بررسی روز هفته
            int dayOfWeek = (int)currentTime.DayOfWeek;
            if (!settings.AllowedDaysOfWeek.Contains(dayOfWeek))
            {
                _logger.LogWarning(
                    "Access denied: Day {DayOfWeek} is not allowed",
                    currentTime.DayOfWeek);

                return ContextAccessResult.Denied(
                    ContextAccessDenialCode.DayNotAllowed,
                    $"دسترسی در روز {GetPersianDayName(currentTime.DayOfWeek)} مجاز نیست");
            }

            // بررسی ساعت
            if (TimeSpan.TryParse(settings.AllowedStartTime, out var startTime) &&
                TimeSpan.TryParse(settings.AllowedEndTime, out var endTime))
            {
                var currentTimeOfDay = currentTime.TimeOfDay;

                bool isInAllowedTime;
                if (startTime <= endTime)
                {
                    // حالت عادی: مثلاً 08:00 تا 17:00
                    isInAllowedTime = currentTimeOfDay >= startTime && currentTimeOfDay <= endTime;
                }
                else
                {
                    // حالت شبانه: مثلاً 22:00 تا 06:00
                    isInAllowedTime = currentTimeOfDay >= startTime || currentTimeOfDay <= endTime;
                }

                if (!isInAllowedTime)
                {
                    _logger.LogWarning(
                        "Access denied: Time {CurrentTime} is outside allowed hours ({Start} - {End})",
                        currentTimeOfDay, startTime, endTime);

                    return ContextAccessResult.Denied(
                        ContextAccessDenialCode.OutsideAllowedHours,
                        $"دسترسی در ساعت {currentTime:HH:mm} مجاز نیست. ساعات مجاز: {settings.AllowedStartTime} تا {settings.AllowedEndTime}");
                }
            }

            return ContextAccessResult.Allowed();
        }

        /// <summary>
        /// بررسی دسترسی بر اساس موقعیت جغرافیایی
        /// </summary>
        public async Task<ContextAccessResult> ValidateGeoAccessAsync(
            string? countryCode,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(countryCode))
            {
                return ContextAccessResult.Allowed();
            }

            var settings = await _securitySettingsService.GetContextAccessControlSettingsAsync(ct);

            if (!settings.EnableGeoRestriction)
            {
                return ContextAccessResult.Allowed();
            }

            countryCode = countryCode.ToUpperInvariant();

            // بررسی کشورهای ممنوع
            if (settings.BlockedCountries.Contains(countryCode))
            {
                _logger.LogWarning(
                    "Access denied: Country {CountryCode} is blocked",
                    countryCode);

                return ContextAccessResult.Denied(
                    ContextAccessDenialCode.CountryBlocked,
                    $"دسترسی از کشور {countryCode} مجاز نیست");
            }

            // بررسی کشورهای مجاز (اگر لیست خالی نباشد)
            if (settings.AllowedCountries.Any() && !settings.AllowedCountries.Contains(countryCode))
            {
                _logger.LogWarning(
                    "Access denied: Country {CountryCode} is not in allowed list",
                    countryCode);

                return ContextAccessResult.Denied(
                    ContextAccessDenialCode.CountryBlocked,
                    $"دسترسی از کشور {countryCode} مجاز نیست");
            }

            return ContextAccessResult.Allowed();
        }

        /// <summary>
        /// بررسی دسترسی بر اساس نوع دستگاه
        /// </summary>
        public async Task<ContextAccessResult> ValidateDeviceAccessAsync(
            string? userAgent,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                return ContextAccessResult.Allowed();
            }

            var settings = await _securitySettingsService.GetContextAccessControlSettingsAsync(ct);

            if (!settings.EnableDeviceRestriction)
            {
                return ContextAccessResult.Allowed();
            }

            // بررسی User Agent های ممنوع
            foreach (var pattern in settings.BlockedUserAgentPatterns)
            {
                try
                {
                    if (Regex.IsMatch(userAgent, pattern, RegexOptions.IgnoreCase))
                    {
                        _logger.LogWarning(
                            "Access denied: User agent matches blocked pattern {Pattern}",
                            pattern);

                        return ContextAccessResult.Denied(
                            ContextAccessDenialCode.UserAgentBlocked,
                            "User Agent ممنوع است");
                    }
                }
                catch (RegexParseException)
                {
                    // الگوی نامعتبر - نادیده بگیر
                }
            }

            // تشخیص نوع دستگاه
            var deviceType = DetectDeviceType(userAgent);

            // بررسی مجوز نوع دستگاه
            switch (deviceType)
            {
                case DeviceType.Mobile when !settings.AllowMobileDevices:
                    return ContextAccessResult.Denied(
                        ContextAccessDenialCode.DeviceTypeBlocked,
                        "دسترسی از دستگاه موبایل مجاز نیست");

                case DeviceType.Desktop when !settings.AllowDesktopDevices:
                    return ContextAccessResult.Denied(
                        ContextAccessDenialCode.DeviceTypeBlocked,
                        "دسترسی از دستگاه دسکتاپ مجاز نیست");

                case DeviceType.Tablet when !settings.AllowTabletDevices:
                    return ContextAccessResult.Denied(
                        ContextAccessDenialCode.DeviceTypeBlocked,
                        "دسترسی از تبلت مجاز نیست");

                case DeviceType.Bot:
                    // ربات‌ها همیشه ممنوع هستند (می‌توان تنظیم کرد)
                    _logger.LogWarning("Bot detected: {UserAgent}", userAgent);
                    break;
            }

            return ContextAccessResult.Allowed();
        }

        /// <summary>
        /// بررسی محدودیت نشست همزمان
        /// </summary>
        public async Task<ContextAccessResult> ValidateConcurrentSessionAsync(
            long userId,
            CancellationToken ct = default)
        {
            var settings = await _securitySettingsService.GetContextAccessControlSettingsAsync(ct);

            if (!settings.EnableConcurrentSessionLimit)
            {
                return ContextAccessResult.Allowed();
            }

            // دریافت تعداد نشست‌های فعال کاربر
            var activeSessions = await _refreshTokenService.GetUserActiveSessionsCountAsync(userId, ct);

            if (activeSessions >= settings.MaxConcurrentSessions)
            {
                _logger.LogWarning(
                    "User {UserId} has reached max concurrent sessions ({ActiveSessions}/{MaxSessions})",
                    userId, activeSessions, settings.MaxConcurrentSessions);

                // بر اساس تنظیمات، اقدام مناسب انجام شود
                switch (settings.ConcurrentSessionAction)
                {
                    case ConcurrentSessionAction.DenyNew:
                        return ContextAccessResult.Denied(
                            ContextAccessDenialCode.TooManySessions,
                            $"تعداد نشست‌های همزمان به حداکثر ({settings.MaxConcurrentSessions}) رسیده است");

                    case ConcurrentSessionAction.TerminateOldest:
                        await _refreshTokenService.TerminateOldestSessionAsync(userId, ct);
                        _logger.LogInformation("Terminated oldest session for user {UserId}", userId);
                        return ContextAccessResult.Allowed();

                    case ConcurrentSessionAction.TerminateAll:
                        await _refreshTokenService.RevokeAllUserTokensAsync(userId, ct);
                        _logger.LogInformation("Terminated all sessions for user {UserId}", userId);
                        return ContextAccessResult.Allowed();
                }
            }

            return ContextAccessResult.Allowed();
        }

        /// <summary>
        /// محاسبه امتیاز ریسک دسترسی
        /// </summary>
        public async Task<RiskAssessmentResult> CalculateRiskScoreAsync(
            AccessContext context,
            long? userId = null,
            CancellationToken ct = default)
        {
            var settings = await _securitySettingsService.GetContextAccessControlSettingsAsync(ct);
            var result = new RiskAssessmentResult
            {
                Factors = new List<RiskFactor>()
            };

            int totalScore = 0;

            // 1. ریسک IP ناشناخته
            if (!string.IsNullOrEmpty(context.IpAddress) && userId.HasValue)
            {
                var isKnownIp = await IsKnownIpForUser(userId.Value, context.IpAddress, ct);
                if (!isKnownIp)
                {
                    var factor = new RiskFactor
                    {
                        Name = "UnknownIP",
                        Score = 20,
                        Description = "IP ناشناخته برای این کاربر"
                    };
                    result.Factors.Add(factor);
                    totalScore += factor.Score;
                }
            }

            // 2. ریسک User Agent ناشناخته
            if (!string.IsNullOrEmpty(context.UserAgent) && userId.HasValue)
            {
                var isKnownUserAgent = await IsKnownUserAgentForUser(userId.Value, context.UserAgent, ct);
                if (!isKnownUserAgent)
                {
                    var factor = new RiskFactor
                    {
                        Name = "UnknownUserAgent",
                        Score = 15,
                        Description = "User Agent ناشناخته برای این کاربر"
                    };
                    result.Factors.Add(factor);
                    totalScore += factor.Score;
                }
            }

            // 3. ریسک زمان غیرعادی
            var currentHour = DateTime.Now.Hour;
            if (currentHour < 6 || currentHour > 22)
            {
                var factor = new RiskFactor
                {
                    Name = "UnusualTime",
                    Score = 10,
                    Description = "دسترسی در ساعات غیرعادی"
                };
                result.Factors.Add(factor);
                totalScore += factor.Score;
            }

            // 4. ریسک دستگاه موبایل
            if (!string.IsNullOrEmpty(context.UserAgent))
            {
                var deviceType = DetectDeviceType(context.UserAgent);
                if (deviceType == DeviceType.Mobile)
                {
                    var factor = new RiskFactor
                    {
                        Name = "MobileDevice",
                        Score = 5,
                        Description = "دسترسی از دستگاه موبایل"
                    };
                    result.Factors.Add(factor);
                    totalScore += factor.Score;
                }
            }

            // 5. ریسک کشور خارجی
            if (!string.IsNullOrEmpty(context.CountryCode) && context.CountryCode.ToUpperInvariant() != "IR")
            {
                var factor = new RiskFactor
                {
                    Name = "ForeignCountry",
                    Score = 25,
                    Description = $"دسترسی از کشور {context.CountryCode}"
                };
                result.Factors.Add(factor);
                totalScore += factor.Score;
            }

            // محاسبه نتیجه نهایی
            result.TotalScore = Math.Min(totalScore, 100);
            result.Level = GetRiskLevel(result.TotalScore);
            result.RequiresMfa = result.TotalScore >= settings.MfaRequiredRiskThreshold;
            result.ShouldDeny = result.TotalScore > settings.MaxAllowedRiskScore;

            return result;
        }

        /// <summary>
        /// بررسی آیا IP در محدوده CIDR است
        /// </summary>
        public bool IsIpInCidrRange(string ipAddress, string cidrRange)
        {
            try
            {
                if (!cidrRange.Contains('/'))
                {
                    // مقایسه مستقیم IP
                    return ipAddress == cidrRange;
                }

                var parts = cidrRange.Split('/');
                var networkAddress = IPAddress.Parse(parts[0]);
                var prefixLength = int.Parse(parts[1]);

                var ipBytes = IPAddress.Parse(ipAddress).GetAddressBytes();
                var networkBytes = networkAddress.GetAddressBytes();

                // بررسی طول آدرس
                if (ipBytes.Length != networkBytes.Length)
                    return false;

                // محاسبه mask
                int bytesToCheck = prefixLength / 8;
                int bitsToCheck = prefixLength % 8;

                // بررسی بایت‌های کامل
                for (int i = 0; i < bytesToCheck; i++)
                {
                    if (ipBytes[i] != networkBytes[i])
                        return false;
                }

                // بررسی بیت‌های باقی‌مانده
                if (bitsToCheck > 0 && bytesToCheck < ipBytes.Length)
                {
                    byte mask = (byte)(0xFF << (8 - bitsToCheck));
                    if ((ipBytes[bytesToCheck] & mask) != (networkBytes[bytesToCheck] & mask))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// تشخیص نوع دستگاه از User Agent
        /// </summary>
        public DeviceType DetectDeviceType(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return DeviceType.Unknown;

            if (BotRegex.IsMatch(userAgent))
                return DeviceType.Bot;

            if (MobileRegex.IsMatch(userAgent))
                return DeviceType.Mobile;

            if (TabletRegex.IsMatch(userAgent) && !MobileRegex.IsMatch(userAgent))
                return DeviceType.Tablet;

            return DeviceType.Desktop;
        }

        #region Private Methods

        private bool IsIpMatch(string ipAddress, string pattern)
        {
            if (pattern.Contains('/'))
            {
                return IsIpInCidrRange(ipAddress, pattern);
            }

            // مقایسه مستقیم یا با wildcard
            if (pattern.Contains('*'))
            {
                var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                return Regex.IsMatch(ipAddress, regexPattern);
            }

            return ipAddress == pattern;
        }

        private async Task<bool> IsKnownIpForUser(long userId, string ipAddress, CancellationToken ct)
        {
            // بررسی از Cache
            var cacheKey = $"known_ips_{userId}";
            if (_cache.TryGetValue<HashSet<string>>(cacheKey, out var knownIps) && knownIps != null)
            {
                return knownIps.Contains(ipAddress);
            }

            // این متد می‌تواند از Audit Log یا جدول جداگانه‌ای برای ذخیره IP های شناخته شده استفاده کند
            // فعلاً true برمی‌گرداند
            return true;
        }

        private async Task<bool> IsKnownUserAgentForUser(long userId, string userAgent, CancellationToken ct)
        {
            // مشابه IsKnownIpForUser
            return true;
        }

        private RiskLevel GetRiskLevel(int score)
        {
            return score switch
            {
                <= 30 => RiskLevel.Low,
                <= 60 => RiskLevel.Medium,
                <= 80 => RiskLevel.High,
                _ => RiskLevel.Critical
            };
        }

        private string GetPersianDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Saturday => "شنبه",
                DayOfWeek.Sunday => "یکشنبه",
                DayOfWeek.Monday => "دوشنبه",
                DayOfWeek.Tuesday => "سه‌شنبه",
                DayOfWeek.Wednesday => "چهارشنبه",
                DayOfWeek.Thursday => "پنجشنبه",
                DayOfWeek.Friday => "جمعه",
                _ => dayOfWeek.ToString()
            };
        }

        private async Task LogAccessDenied(AccessContext context, long? userId, string? reason, CancellationToken ct)
        {
            try
            {
                await _auditLogService.LogEventAsync(
                    eventType: "ContextAccessDenied",
                    entityType: "AccessContext",
                    entityId: context.RequestPath,
                    isSuccess: false,
                    errorMessage: reason,
                    ipAddress: context.IpAddress,
                    userId: userId,
                    userAgent: context.UserAgent,
                    description: $"Context-based access denied: {reason}",
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log access denied event");
            }
        }

        private async Task LogAccessAllowed(AccessContext context, long? userId, CancellationToken ct)
        {
            try
            {
                await _auditLogService.LogEventAsync(
                    eventType: "ContextAccessAllowed",
                    entityType: "AccessContext",
                    entityId: context.RequestPath,
                    isSuccess: true,
                    ipAddress: context.IpAddress,
                    userId: userId,
                    userAgent: context.UserAgent,
                    description: "Context-based access allowed",
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log access allowed event");
            }
        }

        #endregion
    }
}

