using BnpCashClaudeApp.api.Attributes;
using BnpCashClaudeApp.api.Helpers;
using BnpCashClaudeApp.Application.DTOs.Common;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.MediatR.Queries;
using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Infrastructure.Services;
using BnpCashClaudeApp.Persistence.Migrations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading;

namespace BnpCashClaudeApp.api.Controllers.Navigation
{
    /// <summary>
    /// کنترلر مدیریت تنظیمات امنیتی
    /// پیاده‌سازی الزام FMT (Security Management) از استاندارد ISO 15408
    /// 
    /// الزامات پیاده‌سازی شده:
    /// - FMT_MSA: مدیریت ویژگی‌های امنیتی
    /// - FMT_SMF: مشخص کردن توابع مدیریت
    /// - FMT_SMR: نقش‌های مدیریت امنیتی
    /// - FDP_SDI: صحت داده‌های ذخیره شده
    /// 
    /// دسترسی: فقط کاربران با Permission مناسب
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("ApiPolicy")]
    public class SecurityController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly NavigationDbContext _navigationDbContext;
        private readonly IAuditLogService _auditLogService;
        private readonly IPasswordPolicyService _passwordPolicyService;
        private readonly IAccountLockoutService _accountLockoutService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly IDataIntegrityService _dataIntegrityService;
        private readonly IFailSecureService _failSecureService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IDataExportService _dataExportService;
        private readonly IMediator _mediator;

        public SecurityController(
            IConfiguration configuration,
            NavigationDbContext navigationDbContext,
            IAuditLogService auditLogService,
            IPasswordPolicyService passwordPolicyService,
            IAccountLockoutService accountLockoutService,
            ITokenBlacklistService tokenBlacklistService,
            IDataIntegrityService dataIntegrityService,
            IFailSecureService failSecureService,
            IRefreshTokenService refreshTokenService,
            IDataExportService dataExportService,
            IMediator mediator)
        {
            _configuration = configuration;
            _navigationDbContext = navigationDbContext;
            _auditLogService = auditLogService;
            _passwordPolicyService = passwordPolicyService;
            _accountLockoutService = accountLockoutService;
            _tokenBlacklistService = tokenBlacklistService;
            _dataIntegrityService = dataIntegrityService;
            _failSecureService = failSecureService;
            _refreshTokenService = refreshTokenService;
            _dataExportService = dataExportService;
            _mediator = mediator;
        }

        #region DTOs

        /// <summary>
        /// DTO برای تنظیمات امنیتی کلی
        /// </summary>
        public class SecuritySettingsDto
        {
            /// <summary>
            /// تنظیمات سیاست رمز عبور
            /// </summary>
            public PasswordPolicyDto PasswordPolicy { get; set; } = new();

            /// <summary>
            /// تنظیمات قفل حساب
            /// </summary>
            public AccountLockoutDto AccountLockout { get; set; } = new();

            /// <summary>
            /// تنظیمات JWT
            /// </summary>
            public JwtSettingsDto JwtSettings { get; set; } = new();

            /// <summary>
            /// تنظیمات TLS
            /// </summary>
            public TlsSettingsDto TlsSettings { get; set; } = new();

            /// <summary>
            /// تنظیمات Rate Limiting
            /// </summary>
            public RateLimitingSettingsDto RateLimiting { get; set; } = new();
        }

        /// <summary>
        /// DTO سیاست رمز عبور
        /// </summary>
        public class PasswordPolicyDto
        {
            public int MinimumLength { get; set; }
            public int MaximumLength { get; set; }
            public bool RequireUppercase { get; set; }
            public bool RequireLowercase { get; set; }
            public bool RequireDigit { get; set; }
            public bool RequireSpecialCharacter { get; set; }
            public string SpecialCharacters { get; set; } = string.Empty;
            public bool DisallowUsername { get; set; }
            public int PasswordHistoryCount { get; set; }
            public int PasswordExpirationDays { get; set; }
        }

        /// <summary>
        /// DTO تنظیمات قفل حساب
        /// </summary>
        public class AccountLockoutDto
        {
            public int MaxFailedAttempts { get; set; }
            public int LockoutDurationMinutes { get; set; }
            public bool EnablePermanentLockout { get; set; }
            public int PermanentLockoutThreshold { get; set; }
            public int FailedAttemptResetMinutes { get; set; }
        }

        /// <summary>
        /// DTO تنظیمات JWT
        /// </summary>
        public class JwtSettingsDto
        {
            public string Issuer { get; set; } = string.Empty;
            public string Audience { get; set; } = string.Empty;
            public int ExpiresMinutes { get; set; }
        }

        /// <summary>
        /// DTO تنظیمات TLS
        /// </summary>
        public class TlsSettingsDto
        {
            public string MinimumTlsVersion { get; set; } = string.Empty;
            public bool RequireClientCertificate { get; set; }
        }

        /// <summary>
        /// DTO تنظیمات Rate Limiting
        /// </summary>
        public class RateLimitingSettingsDto
        {
            public RateLimitPolicyDto Authentication { get; set; } = new();
            public RateLimitPolicyDto Api { get; set; } = new();
        }

        /// <summary>
        /// DTO یک سیاست Rate Limit
        /// </summary>
        public class RateLimitPolicyDto
        {
            public int PermitLimit { get; set; }
            public int WindowMinutes { get; set; }
            public int QueueLimit { get; set; }
        }

        /// <summary>
        /// DTO وضعیت کاربر
        /// ============================================
        /// تاریخ‌ها به صورت شمسی (string) هستند
        /// ============================================
        /// </summary>
        public class UserSecurityStatusDto
        {
            public long UserId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public bool IsLockedOut { get; set; }
            public int FailedAttempts { get; set; }
            public int RemainingAttempts { get; set; }
            public string? LockoutEndTime { get; set; } // شمسی
            public string? LastLoginAt { get; set; } // شمسی
            public string? PasswordLastChangedAt { get; set; } // شمسی
            public bool MustChangePassword { get; set; }
            public string? LastIpAddressLogin { get; set; }
        }

        /// <summary>
        /// DTO بررسی سلامت امنیتی
        /// ============================================
        /// تاریخ‌ها به صورت شمسی نمایش داده می‌شوند
        /// ============================================
        /// </summary>
        public class SecurityHealthCheckDto
        {
            /// <summary>
            /// وضعیت کلی سلامت
            /// </summary>
            public string OverallStatus { get; set; } = "Healthy";

            /// <summary>
            /// لیست بررسی‌ها
            /// </summary>
            public List<HealthCheckItemDto> Checks { get; set; } = new();

            /// <summary>
            /// زمان بررسی (شمسی)
            /// </summary>
            public string CheckedAt { get; set; } = BaseEntity.ToPersianDateTime(DateTime.UtcNow);
        }

        /// <summary>
        /// DTO آیتم بررسی سلامت
        /// </summary>
        public class HealthCheckItemDto
        {
            public string Name { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty; // Healthy, Warning, Unhealthy
            public string? Message { get; set; }
        }

        #endregion

        /// <summary>
        /// دریافت تمام تنظیمات امنیتی
        /// الزام FMT_SMF.1: مشخص کردن توابع مدیریت
        /// </summary>
        /// <returns>تنظیمات امنیتی کلی</returns>
        [HttpGet("settings")]
        [RequirePermission("Security.Read")]
        [ProducesResponseType(typeof(SecuritySettingsDto), 200)]
        public async Task<ActionResult<SecuritySettingsDto>> GetSecuritySettings()
        {
            try
            {
            // ============================================
            // جمع‌آوری تمام تنظیمات امنیتی
            // ============================================
                var query = new GetPasswordPolicySettingsQuery();
                var result = await _mediator.Send(query);
                var passwordPolicy = await ProtectReadPayloadAsync(result, "PasswordPolicySettings");

                var query2 = new GetAccountLockoutSettingsQuery();
                var result2 = await _mediator.Send(query2);
                var lockoutSettings = await ProtectReadPayloadAsync(result2, "AccountLockoutSettings");

                var settings = new SecuritySettingsDto
            {
                // ============================================
                // تنظیمات سیاست رمز عبور
                // ============================================
                PasswordPolicy = new PasswordPolicyDto
                {
                    MinimumLength = passwordPolicy.MinimumLength,
                    MaximumLength = passwordPolicy.MaximumLength,
                    RequireUppercase = passwordPolicy.RequireUppercase,
                    RequireLowercase = passwordPolicy.RequireLowercase,
                    RequireDigit = passwordPolicy.RequireDigit,
                    RequireSpecialCharacter = passwordPolicy.RequireSpecialCharacter,
                    SpecialCharacters = passwordPolicy.SpecialCharacters,
                    DisallowUsername = passwordPolicy.DisallowUsername,
                    PasswordHistoryCount = passwordPolicy.PasswordHistoryCount,
                    PasswordExpirationDays = passwordPolicy.PasswordExpirationDays
                },

                // ============================================
                // تنظیمات قفل حساب
                // ============================================
                AccountLockout = new AccountLockoutDto
                {
                    MaxFailedAttempts = lockoutSettings.MaxFailedAttempts,
                    LockoutDurationMinutes = lockoutSettings.LockoutDurationMinutes,
                    EnablePermanentLockout = lockoutSettings.EnablePermanentLockout,
                    PermanentLockoutThreshold = lockoutSettings.PermanentLockoutThreshold,
                    FailedAttemptResetMinutes = lockoutSettings.FailedAttemptResetMinutes
                },

                // ============================================
                // تنظیمات JWT
                // ============================================
                JwtSettings = new JwtSettingsDto
                {
                    Issuer = _configuration["Jwt:Issuer"] ?? string.Empty,
                    Audience = _configuration["Jwt:Audience"] ?? string.Empty,
                    ExpiresMinutes = _configuration.GetValue<int>("Jwt:ExpiresMinutes", 60)
                },

                // ============================================
                // تنظیمات TLS
                // ============================================
                TlsSettings = new TlsSettingsDto
                {
                    MinimumTlsVersion = _configuration["Tls:MinimumTlsVersion"] ?? "1.2",
                    RequireClientCertificate = _configuration.GetValue<bool>("Tls:RequireClientCertificate", false)
                },

                // ============================================
                // تنظیمات Rate Limiting
                // ============================================
                RateLimiting = new RateLimitingSettingsDto
                {
                    Authentication = new RateLimitPolicyDto
                    {
                        PermitLimit = _configuration.GetValue<int>("RateLimiting:Authentication:PermitLimit", 5),
                        WindowMinutes = _configuration.GetValue<int>("RateLimiting:Authentication:WindowMinutes", 1),
                        QueueLimit = _configuration.GetValue<int>("RateLimiting:Authentication:QueueLimit", 2)
                    },
                    Api = new RateLimitPolicyDto
                    {
                        PermitLimit = _configuration.GetValue<int>("RateLimiting:Api:PermitLimit", 100),
                        WindowMinutes = _configuration.GetValue<int>("RateLimiting:Api:WindowMinutes", 1),
                        QueueLimit = _configuration.GetValue<int>("RateLimiting:Api:QueueLimit", 10)
                    }
                }
            };

                var protectedSettings = await ProtectReadPayloadAsync(settings, "SecuritySettings");
                return Ok(protectedSettings);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت سیاست رمز عبور
        /// الزام FMT_MSA.1: مدیریت ویژگی‌های امنیتی
        /// </summary>
        /// <returns>تنظیمات سیاست رمز عبور</returns>
        //[HttpGet("password-policy")]
        //[RequirePermission("Security.PasswordPolicy")]
        //[ProducesResponseType(typeof(PasswordPolicyDto), 200)]
        //public async Task<ActionResult<PasswordPolicyDto>> GetPasswordPolicy()
        //{
        //    try
        //    {
        //        var policy = _passwordPolicyService.GetPolicySettings();

        //        var response = new PasswordPolicyDto
        //        {
        //            MinimumLength = policy.MinimumLength,
        //            MaximumLength = policy.MaximumLength,
        //            RequireUppercase = policy.RequireUppercase,
        //            RequireLowercase = policy.RequireLowercase,
        //            RequireDigit = policy.RequireDigit,
        //            RequireSpecialCharacter = policy.RequireSpecialCharacter,
        //            SpecialCharacters = policy.SpecialCharacters,
        //            DisallowUsername = policy.DisallowUsername,
        //            PasswordHistoryCount = policy.PasswordHistoryCount,
        //            PasswordExpirationDays = policy.PasswordExpirationDays
        //        };

        //        var protectedResponse = await ProtectReadPayloadAsync(response, "PasswordPolicy");
        //        return Ok(protectedResponse);
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        return StatusCode(403, new { success = false, error = ex.Message });
        //    }
        //}

        /// <summary>
        /// دریافت تنظیمات قفل حساب
        /// الزام FMT_MSA.1: مدیریت ویژگی‌های امنیتی
        /// </summary>
        /// <returns>تنظیمات قفل حساب</returns>
        //[HttpGet("lockout-policy")]
        //[RequirePermission("Security.LockoutPolicy")]
        //[ProducesResponseType(typeof(AccountLockoutDto), 200)]
        //public async Task<ActionResult<AccountLockoutDto>> GetLockoutPolicy()
        //{
        //    try
        //    {
        //        var settings = _accountLockoutService.GetSettings();

        //        var response = new AccountLockoutDto
        //        {
        //            MaxFailedAttempts = settings.MaxFailedAttempts,
        //            LockoutDurationMinutes = settings.LockoutDurationMinutes,
        //            EnablePermanentLockout = settings.EnablePermanentLockout,
        //            PermanentLockoutThreshold = settings.PermanentLockoutThreshold,
        //            FailedAttemptResetMinutes = settings.FailedAttemptResetMinutes
        //        };

        //        var protectedResponse = await ProtectReadPayloadAsync(response, "AccountLockoutPolicy");
        //        return Ok(protectedResponse);
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        return StatusCode(403, new { success = false, error = ex.Message });
        //    }
        //}

        /// <summary>
        /// دریافت وضعیت امنیتی کاربران (با فیلتر اختیاری)
        /// الزام FMT_MSA.1: مدیریت ویژگی‌های امنیتی
        /// </summary>
        /// <param name="userName">اختیاری - نام کاربری؛ در صورت عدم ارسال، وضعیت همه کاربران برمی‌گردد</param>
        /// <returns>لیست وضعیت امنیتی کاربران (یک آیتم در صورت فیلتر)</returns>
        [HttpGet("users-security-status")]
        [RequirePermission("Security.Read")]
        [ProducesResponseType(typeof(List<UserSecurityStatusDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<List<UserSecurityStatusDto>>> GetUsersSecurityStatus(
            [FromQuery] string? userName = null)
        {
            try
            {
                List<tblUser> users;
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    var user = await _navigationDbContext.tblUsers
                        .FirstOrDefaultAsync(u => u.UserName == userName.Trim());
                    if (user == null)
                        return Ok (new ResultDto(false , "کاربر یافت نشد" ));
                    users = new List<tblUser> { user };
                }
                else
                {
                    users = await _navigationDbContext.tblUsers.ToListAsync();
                }

                var result = new List<UserSecurityStatusDto>();
                foreach (var user in users)
                {
                    var lockoutStatus = await _accountLockoutService.GetLockoutStatusAsync(user.UserName);
                    result.Add(new UserSecurityStatusDto
                    {
                        UserId = user.Id,
                        UserName = user.UserName,
                        FullName = $"{user.FirstName} {user.LastName}",
                        IsActive = user.IsActive,
                        IsLockedOut = lockoutStatus.IsLockedOut,
                        FailedAttempts = lockoutStatus.FailedAttempts,
                        RemainingAttempts = lockoutStatus.RemainingAttempts,
                        LockoutEndTime = lockoutStatus.LockoutEndTime.HasValue
                            ? BaseEntity.ToPersianDateTime(lockoutStatus.LockoutEndTime.Value)
                            : null,
                        LastLoginAt = user.LastLoginAt,
                        PasswordLastChangedAt = user.PasswordLastChangedAt,
                        MustChangePassword = user.MustChangePassword,
                        LastIpAddressLogin = user.IpAddress
                    });
                }

                var payloadKey = result.Count == 1
                    ? $"UserSecurityStatus_{result[0].UserId}"
                    : "UserSecurityStatusList";
                var protectedResult = await ProtectReadPayloadAsync(result, payloadKey,
                    result.Count == 1 ? result[0].UserId.ToString() : null);
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// باطل کردن تمام نشست‌های یک کاربر
        /// الزام FMT_SMF.1: مشخص کردن توابع مدیریت
        /// الزام FTA_MCS.1: مدیریت نشست‌های همزمان
        /// </summary>
        /// <param name="publicId">شناسه عمومی (PublicId) کاربر</param>
        /// <returns>نتیجه عملیات</returns>
        [HttpPost("terminate-sessions/{publicId}")]
        [RequirePermission("Security.TerminateSessions")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> TerminateUserSessions(Guid publicId)
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var adminUserName = User.Identity?.Name ?? "Unknown";

            // ============================================
            // پیدا کردن کاربر بر اساس PublicId
            // ============================================
            var user = await _navigationDbContext.tblUsers
                .FirstOrDefaultAsync(u => u.PublicId == publicId);
            if (user == null)
            {
                return NotFound(new { message = "کاربر یافت نشد" });
            }

            // ============================================
            // 1. باطل کردن تمام توکن‌ها در حافظه (Blacklist)
            // این کار باعث می‌شود توکن‌های JWT فعلی نامعتبر شوند
            // ============================================
            await _tokenBlacklistService.BlacklistAllUserTokensAsync(
                user.UserName,
                $"Sessions terminated by admin: {adminUserName}",
                default);

            // ============================================
            // 2. باطل کردن تمام Refresh Token‌ها در دیتابیس
            // این کار باعث می‌شود که GetUserActiveSessionsCountAsync 
            // دیگر این نشست‌ها را به عنوان فعال شمارش نکند
            // و محدودیت نشست همزمان (FTA_MCS.1) به درستی کار کند
            // ============================================
            await _refreshTokenService.RevokeAllUserTokensAsync(
                user.Id,
                $"Sessions terminated by admin: {adminUserName}",
                default);

            // ============================================
            // 3. ثبت Audit Log
            // ============================================
            await _auditLogService.LogEventAsync(
                eventType: "SessionsTerminated",
                entityType: "User",
                entityId: publicId.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userName: adminUserName,
                userId: user.Id,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"تمام نشست‌های کاربر {user.UserName} توسط مدیر {adminUserName} بسته شد",
                ct: default);

            return Ok(new { message = $"تمام نشست‌های کاربر {user.UserName} با موفقیت بسته شد" });
        }

        /// <summary>
        /// بررسی سلامت امنیتی سیستم
        /// الزام FMT_SMF.1: مشخص کردن توابع مدیریت
        /// </summary>
        /// <returns>نتیجه بررسی سلامت</returns>
        [HttpGet("health-check")]
        [RequirePermission("Security.Read")]
        [ProducesResponseType(typeof(SecurityHealthCheckDto), 200)]
        public async Task<ActionResult<SecurityHealthCheckDto>> HealthCheck()
        {
            try
            {
            var result = new SecurityHealthCheckDto();
            var hasIssues = false;

            // ============================================
            // بررسی 1: کاربران قفل شده
            // ============================================
            var users = await _navigationDbContext.tblUsers.ToListAsync();
            var lockedUsers = 0;
            foreach (var user in users)
            {
                var status = await _accountLockoutService.GetLockoutStatusAsync(user.UserName);
                if (status.IsLockedOut) lockedUsers++;
            }

            if (lockedUsers > 0)
            {
                result.Checks.Add(new HealthCheckItemDto
                {
                    Name = "حساب‌های قفل شده",
                    Status = "Warning",
                    Message = $"{lockedUsers} حساب کاربری قفل شده وجود دارد"
                });
                hasIssues = true;
            }
            else
            {
                result.Checks.Add(new HealthCheckItemDto
                {
                    Name = "حساب‌های قفل شده",
                    Status = "Healthy",
                    Message = "هیچ حساب قفل شده‌ای وجود ندارد"
                });
            }

            // ============================================
            // بررسی 2: کاربران غیرفعال
            // ============================================
            var inactiveUsers = users.Count(u => !u.IsActive);
            result.Checks.Add(new HealthCheckItemDto
            {
                Name = "کاربران غیرفعال",
                Status = inactiveUsers > 0 ? "Warning" : "Healthy",
                Message = inactiveUsers > 0 
                    ? $"{inactiveUsers} کاربر غیرفعال وجود دارد" 
                    : "تمام کاربران فعال هستند"
            });

            // ============================================
            // بررسی 3: تنظیمات TLS
            // ============================================
            var minTls = _configuration["Tls:MinimumTlsVersion"] ?? "1.2";
            if (minTls == "1.2" || minTls == "1.3")
            {
                result.Checks.Add(new HealthCheckItemDto
                {
                    Name = "تنظیمات TLS",
                    Status = "Healthy",
                    Message = $"حداقل نسخه TLS: {minTls}"
                });
            }
            else
            {
                result.Checks.Add(new HealthCheckItemDto
                {
                    Name = "تنظیمات TLS",
                    Status = "Unhealthy",
                    Message = "نسخه TLS پایین‌تر از 1.2 امن نیست"
                });
                hasIssues = true;
            }

            // ============================================
            // بررسی 4: طول کلید JWT
            // ============================================
            var jwtKey = _configuration["Jwt:Key"] ?? string.Empty;
            if (jwtKey.Length >= 32)
            {
                result.Checks.Add(new HealthCheckItemDto
                {
                    Name = "طول کلید JWT",
                    Status = "Healthy",
                    Message = "کلید JWT به اندازه کافی طولانی است"
                });
            }
            else
            {
                result.Checks.Add(new HealthCheckItemDto
                {
                    Name = "طول کلید JWT",
                    Status = "Unhealthy",
                    Message = "کلید JWT باید حداقل 32 کاراکتر باشد"
                });
                hasIssues = true;
            }

            // ============================================
            // بررسی 5: سیاست رمز عبور
            // ============================================
            var passwordPolicy = _passwordPolicyService.GetPolicySettings();
            if (passwordPolicy.MinimumLength >= 8 &&
                passwordPolicy.RequireUppercase &&
                passwordPolicy.RequireLowercase &&
                passwordPolicy.RequireDigit &&
                passwordPolicy.RequireSpecialCharacter)
            {
                result.Checks.Add(new HealthCheckItemDto
                {
                    Name = "سیاست رمز عبور",
                    Status = "Healthy",
                    Message = "سیاست رمز عبور مناسب است"
                });
            }
            else
            {
                result.Checks.Add(new HealthCheckItemDto
                {
                    Name = "سیاست رمز عبور",
                    Status = "Warning",
                    Message = "سیاست رمز عبور می‌تواند قوی‌تر باشد"
                });
            }

            // ============================================
            // بررسی 6: Rate Limiting
            // ============================================
            var authPermitLimit = _configuration.GetValue<int>("RateLimiting:Authentication:PermitLimit", 5);
            if (authPermitLimit <= 10)
            {
                result.Checks.Add(new HealthCheckItemDto
                {
                    Name = "Rate Limiting احراز هویت",
                    Status = "Healthy",
                    Message = $"محدودیت مناسب: {authPermitLimit} درخواست در دقیقه"
                });
            }
            else
            {
                result.Checks.Add(new HealthCheckItemDto
                {
                    Name = "Rate Limiting احراز هویت",
                    Status = "Warning",
                    Message = "محدودیت بالا ممکن است حملات Brute Force را آسان کند"
                });
            }

            // ============================================
            // تعیین وضعیت کلی
            // ============================================
            if (result.Checks.Any(c => c.Status == "Unhealthy"))
            {
                result.OverallStatus = "Unhealthy";
            }
            else if (hasIssues || result.Checks.Any(c => c.Status == "Warning"))
            {
                result.OverallStatus = "Warning";
            }
            else
            {
                result.OverallStatus = "Healthy";
            }

                var protectedResult = await ProtectReadPayloadAsync(result, "SecurityHealthCheck");
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت اطلاعات محیط سیستم
        /// الزام FMT_SMF.1: مشخص کردن توابع مدیریت
        /// </summary>
        /// <returns>اطلاعات محیط</returns>
                [HttpGet("environment-info")]
        [RequirePermission("Security.Read")]
        [ProducesResponseType(200)]
        public async Task<ActionResult> GetEnvironmentInfo()
        {
            try
            {
                var response = new
                {
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    DotNetVersion = Environment.Version.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                    Is64BitProcess = Environment.Is64BitProcess,
                    ServerTime = BaseEntity.ToPersianDateTime(DateTime.UtcNow), // زمان سرور (شمسی)
                    TimeZone = TimeZoneInfo.Local.DisplayName
                };

                var protectedResponse = await ProtectReadPayloadAsync(response, "EnvironmentInfo");
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        #region Data Integrity (FDP_SDI)

        /// <summary>
        /// بررسی صحت داده‌های حساس
        /// الزام FDP_SDI.2.2: بررسی دوره‌ای صحت داده‌ها
        /// </summary>
        /// <returns>نتیجه بررسی صحت</returns>
        [HttpGet("verify-data-integrity")]
        [RequirePermission("Security.DataIntegrity")]
        [ProducesResponseType(200)]
        public async Task<ActionResult> VerifyDataIntegrity()
        {
            try
            {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var userName = User.Identity?.Name ?? "Unknown";

            var result = await _dataIntegrityService.VerifyAllEntitiesIntegrityAsync();

            await _auditLogService.LogEventAsync(
                eventType: "DataIntegrityVerification",
                entityType: "System",
                isSuccess: result.TotalViolations == 0,
                ipAddress: ipAddress,
                userName: userName,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"بررسی صحت داده‌ها توسط {userName}. تعداد کل نقض: {result.TotalViolations}. " +
                           $"تفکیک: {(result.ViolationsByEntityType.Count > 0 ? string.Join(", ", result.ViolationsByEntityType.Select(kv => $"{kv.Key}: {kv.Value}")) : "هیچ نقضی شناسایی نشد")}",
                ct: default);

            var response = new
            {
                Success = result.TotalViolations == 0,
                TotalViolations = result.TotalViolations,
                ViolationsByEntityType = result.ViolationsByEntityType,
                VerificationTime = result.VerificationTime,
                Message = result.TotalViolations == 0 
                    ? "تمام داده‌های حساس معتبر هستند"
                    : $"{result.TotalViolations} مورد نقض صحت داده شناسایی شد. " +
                      $"تفکیک: {(result.ViolationsByEntityType.Count > 0 ? string.Join(", ", result.ViolationsByEntityType.Select(kv => $"{kv.Key}: {kv.Value}")) : "هیچ نقضی شناسایی نشد")}",
                VerifiedAt = BaseEntity.ToPersianDateTime(result.VerificationTime)
            };

                var protectedResponse = await ProtectReadPayloadAsync(response, "DataIntegrityVerification");
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// محاسبه Integrity Hash برای داده‌های موجود
        /// این متد برای داده‌های قدیمی که Hash ندارند استفاده می‌شود
        /// الزام FDP_SDI.2.1: محاسبه Integrity Hash
        /// </summary>
        /// <returns>نتیجه محاسبه</returns>
        [HttpPost("compute-integrity-hash")]
        [RequirePermission("Security.DataIntegrity")]
        [ProducesResponseType(200)]
        public async Task<ActionResult> ComputeIntegrityHashForExistingData()
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var userName = User.Identity?.Name ?? "Unknown";

            var updatedCount = await _dataIntegrityService.ComputeHashForExistingEntitiesAsync();

            await _auditLogService.LogEventAsync(
                eventType: "IntegrityHashComputed",
                entityType: "System",
                isSuccess: true,
                ipAddress: ipAddress,
                userName: userName,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"محاسبه Integrity Hash برای {updatedCount} Entity توسط {userName}",
                ct: default);

            return Ok(new
            {
                Success = true,
                UpdatedCount = updatedCount,
                Message = $"Integrity Hash برای {updatedCount} Entity محاسبه شد",
                ComputedAt = BaseEntity.ToPersianDateTime(DateTime.UtcNow)
            });
        }

        /// <summary>
        /// تولید کلید Integrity امن جدید
        /// این کلید باید در appsettings.json یا Key Vault ذخیره شود
        /// </summary>
        /// <returns>کلید Base64 encoded</returns>
        [HttpGet("generate-integrity-key")]
        [RequirePermission("Security.DataIntegrity")]
        [ProducesResponseType(200)]
        public async Task<ActionResult> GenerateIntegrityKey()
        {
            try
            {
            var key = _dataIntegrityService.GenerateSecureIntegrityKey();

            var response = new
            {
                Key = key,
                Message = "این کلید را در Security:IntegrityKey در appsettings.json یا Key Vault ذخیره کنید",
                Warning = "توجه: تغییر کلید باعث نامعتبر شدن تمام Hash های موجود می‌شود"
            };

                var protectedResponse = await ProtectReadPayloadAsync(response, "IntegrityKey");
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        #endregion

        #region Fail-Secure (FPT_FLS.1.1)

        /// <summary>
        /// دریافت وضعیت Fail-Secure سیستم
        /// الزام FPT_FLS.1.1: حفظ وضعیت امن در زمان شکست
        /// </summary>
        /// <returns>وضعیت سلامت Fail-Secure</returns>
        [HttpGet("fail-secure/status")]
        [RequirePermission("Security.Read")]
        [ProducesResponseType(typeof(FailSecureHealthStatus), 200)]
        public async Task<ActionResult<FailSecureHealthStatus>> GetFailSecureStatus()
        {
            try
            {
                var status = _failSecureService.GetHealthStatus();
                var protectedStatus = await ProtectReadPayloadAsync(status, "FailSecureStatus");
                return Ok(protectedStatus);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// تست ثبت شکست در فایل (برای تست عملکرد Fail-Secure)
        /// این endpoint برای تست و دیباگ است
        /// </summary>
        /// <returns>نتیجه تست</returns>
        //[HttpPost("fail-secure/test-file-logging")]
        //[RequirePermission("Security.DataIntegrity")]
        //[ProducesResponseType(200)]
        //public async Task<ActionResult> TestFailSecureFileLogging()
        //{
        //    var userName = User.Identity?.Name ?? "Unknown";
            
        //    // تست ذخیره در فایل
        //    await _failSecureService.LogFailureToFileAsync(
        //        failureType: "TestFailure",
        //        operationName: "TestOperation",
        //        details: $"این یک تست است که توسط {userName} اجرا شد. زمان: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        //    return Ok(new
        //    {
        //        Success = true,
        //        Message = "شکست تستی در فایل ذخیره شد",
        //        FilePath = "logs/fail-secure/failures_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log",
        //        TestedBy = userName,
        //        TestedAt = BaseEntity.ToPersianDateTime(DateTime.UtcNow)
        //    });
        //}

        /// <summary>
        /// غیرفعال کردن حالت امن سیستم
        /// فقط Admin می‌تواند این کار را انجام دهد
        /// </summary>
        /// <returns>نتیجه عملیات</returns>
        //[HttpPost("fail-secure/deactivate")]
        //[RequirePermission("Security.Admin")]
        //[ProducesResponseType(200)]
        //public async Task<ActionResult> DeactivateSecureMode()
        //{
        //    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        //    if (!long.TryParse(userIdClaim, out var userId))
        //    {
        //        return BadRequest(new { message = "شناسه کاربر نامعتبر است" });
        //    }

        //    await _failSecureService.DeactivateSecureModeAsync(userId);

        //    return Ok(new
        //    {
        //        Success = true,
        //        Message = "حالت امن سیستم غیرفعال شد",
        //        DeactivatedBy = User.Identity?.Name,
        //        DeactivatedAt = BaseEntity.ToPersianDateTime(DateTime.UtcNow)
        //    });
        //}

        #endregion

        #region Context Access Control (FDP_ACF.1.4)

        /// <summary>
        /// دریافت تنظیمات کنترل دسترسی مبتنی بر Context
        /// الزام FDP_ACF.1.4: عملیات کنترل دسترسی 4
        /// </summary>
        /// <returns>تنظیمات Context Access Control</returns>
        [HttpGet("context-access/settings")]
        [RequirePermission("Security.Read")]
        [ProducesResponseType(typeof(ContextAccessControlSettings), 200)]
        public async Task<ActionResult<ContextAccessControlSettings>> GetContextAccessControlSettings(
            [FromServices] ISecuritySettingsService securitySettingsService)
        {
            try
            {
                var settings = await securitySettingsService.GetContextAccessControlSettingsAsync();
                var response = new
                {
                    Success = true,
                    Settings = settings
                };

                var protectedResponse = await ProtectReadPayloadAsync(response, "ContextAccessControlSettings");
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// ذخیره تنظیمات کنترل دسترسی مبتنی بر Context
        /// الزام FDP_ACF.1.4: عملیات کنترل دسترسی 4
        /// </summary>
        /// <param name="settings">تنظیمات جدید</param>
        /// <returns>نتیجه عملیات</returns>
        [HttpPost("context-access/settings")]
        [RequirePermission("Security.Write")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> SaveContextAccessControlSettings(
            [FromBody] ContextAccessControlSettings settings,
            [FromServices] ISecuritySettingsService securitySettingsService)
        {
            if (settings == null)
            {
                return BadRequest(new { message = "تنظیمات نامعتبر است" });
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out var userId))
            {
                return BadRequest(new { message = "شناسه کاربر نامعتبر است" });
            }

            await securitySettingsService.SaveContextAccessControlSettingsAsync(settings, userId);

            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var userName = User.Identity?.Name ?? "Unknown";

            await _auditLogService.LogEventAsync(
                eventType: "ContextAccessControlSettingsUpdated",
                entityType: "SecuritySetting",
                entityId: "ContextAccessControl",
                isSuccess: true,
                ipAddress: ipAddress,
                userName: userName,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"تنظیمات کنترل دسترسی مبتنی بر Context توسط {userName} به‌روزرسانی شد",
                ct: default);

            return Ok(new
            {
                Success = true,
                Message = "تنظیمات کنترل دسترسی مبتنی بر Context ذخیره شد",
                UpdatedBy = userName,
                UpdatedAt = BaseEntity.ToPersianDateTime(DateTime.UtcNow)
            });
        }

        /// <summary>
        /// تست کنترل دسترسی Context برای IP خاص
        /// </summary>
        /// <param name="ipAddress">آدرس IP برای تست</param>
        /// <returns>نتیجه بررسی</returns>
        //[HttpGet("context-access/test-ip")]
        //[RequirePermission("Security.Read")]
        //[ProducesResponseType(typeof(ContextAccessResult), 200)]
        //public async Task<ActionResult<ContextAccessResult>> TestIpAccess(
        //    [FromQuery] string ipAddress,
        //    [FromServices] IContextAccessControlService contextAccessControlService)
        //{
        //    if (string.IsNullOrWhiteSpace(ipAddress))
        //    {
        //        return BadRequest(new { message = "آدرس IP الزامی است" });
        //    }

        //    var result = await contextAccessControlService.ValidateIpAccessAsync(ipAddress);
        //    return Ok(new
        //    {
        //        IpAddress = ipAddress,
        //        Result = result
        //    });
        //}

        /// <summary>
        /// تست کنترل دسترسی Context برای زمان فعلی
        /// </summary>
        /// <returns>نتیجه بررسی</returns>
        //[HttpGet("context-access/test-time")]
        //[RequirePermission("Security.Read")]
        //[ProducesResponseType(typeof(ContextAccessResult), 200)]
        //public async Task<ActionResult<ContextAccessResult>> TestTimeAccess(
        //    [FromServices] IContextAccessControlService contextAccessControlService)
        //{
        //    var result = await contextAccessControlService.ValidateTimeAccessAsync();
        //    return Ok(new
        //    {
        //        CurrentTime = DateTime.Now.ToString("HH:mm"),
        //        DayOfWeek = DateTime.Now.DayOfWeek.ToString(),
        //        Result = result
        //    });
        //}

        /// <summary>
        /// تست کامل کنترل دسترسی Context
        /// </summary>
        /// <returns>نتیجه بررسی جامع</returns>
        //[HttpGet("context-access/test-full")]
        //[RequirePermission("Security.Read")]
        //[ProducesResponseType(typeof(ContextAccessResult), 200)]
        //public async Task<ActionResult<ContextAccessResult>> TestFullContextAccess(
        //    [FromServices] IContextAccessControlService contextAccessControlService)
        //{
        //    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        //    long? userId = long.TryParse(userIdClaim, out var id) ? id : null;

        //    var context = new AccessContext
        //    {
        //        IpAddress = HttpContextHelper.GetIpAddress(HttpContext),
        //        UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
        //        RequestPath = HttpContext.Request.Path,
        //        HttpMethod = HttpContext.Request.Method,
        //        RequestTime = DateTime.UtcNow
        //    };

        //    var result = await contextAccessControlService.ValidateAccessAsync(context, userId);

        //    return Ok(new
        //    {
        //        Context = context,
        //        Result = result
        //    });
        //}

        /// <summary>
        /// محاسبه امتیاز ریسک برای Context فعلی
        /// </summary>
        /// <returns>نتیجه ارزیابی ریسک</returns>
        [HttpGet("context-access/risk-score")]
        [RequirePermission("Security.Read")]
        [ProducesResponseType(typeof(RiskAssessmentResult), 200)]
        public async Task<ActionResult<RiskAssessmentResult>> CalculateRiskScore(
            [FromServices] IContextAccessControlService contextAccessControlService)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                long? userId = long.TryParse(userIdClaim, out var id) ? id : null;

                var context = new AccessContext
                {
                    IpAddress = HttpContextHelper.GetIpAddress(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    RequestPath = HttpContext.Request.Path,
                    HttpMethod = HttpContext.Request.Method,
                    RequestTime = DateTime.UtcNow
                };

                var result = await contextAccessControlService.CalculateRiskScoreAsync(context, userId);

                var response = new
                {
                    Context = context,
                    RiskAssessment = result
                };

                var protectedResponse = await ProtectReadPayloadAsync(response, "ContextRiskScore");
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        #endregion

       
    }

    
}


