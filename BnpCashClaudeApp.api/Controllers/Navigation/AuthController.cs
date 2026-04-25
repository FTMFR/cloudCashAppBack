using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.api.Extensions;
using BnpCashClaudeApp.api.Helpers;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Text;
using BnpCashClaudeApp.Domain.Common;

namespace BnpCashClaudeApp.api.Controllers.Navigation
{
    /// <summary>
    /// کنترلر احراز هویت
    /// پیاده‌سازی الزامات امنیتی پروفایل حفاظتی برنامه‌های کاربردی تحت شبکه (ISO 15408)
    /// 
    /// الزامات پیاده‌سازی شده:
    /// - Rate Limiting: AuthenticationPolicy برای جلوگیری از حملات Brute Force
    /// - Account Lockout: قفل حساب پس از تلاش‌های ناموفق
    /// - Password Policy: سیاست پیچیدگی رمز عبور
    /// - Token Blacklist: خروج امن با غیرفعال کردن توکن
    /// - Audit Logging: ثبت تمام رویدادهای امنیتی
    /// - MFA (FIA_UAU.5): احرازهویت چندگانه با پیامک (SMS OTP)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("AuthenticationPolicy")]
    public class AuthController : Controller
    {
        private readonly NavigationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuditLogService _auditLogService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAccountLockoutService _accountLockoutService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly IPasswordPolicyService _passwordPolicyService;
        private readonly IPasswordHistoryService _passwordHistoryService;
        private readonly IMfaService _mfaService;
        private readonly ICaptchaService _captchaService;
        private readonly ISecuritySettingsService _securitySettingsService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ISecureMemoryService _secureMemoryService;
        private readonly IPasswordResetService _passwordResetService;
        private readonly IDataExportService _dataExportService;

        public AuthController(
            NavigationDbContext context,
            IConfiguration configuration,
            IAuditLogService auditLogService,
            IPasswordHasher passwordHasher,
            IAccountLockoutService accountLockoutService,
            ITokenBlacklistService tokenBlacklistService,
            IPasswordPolicyService passwordPolicyService,
            IPasswordHistoryService passwordHistoryService,
            IMfaService mfaService,
            ICaptchaService captchaService,
            ISecuritySettingsService securitySettingsService,
            IRefreshTokenService refreshTokenService,
            ISecureMemoryService secureMemoryService,
            IPasswordResetService passwordResetService,
            IDataExportService dataExportService)
        {
            _context = context;
            _configuration = configuration;
            _auditLogService = auditLogService;
            _passwordHasher = passwordHasher;
            _accountLockoutService = accountLockoutService;
            _tokenBlacklistService = tokenBlacklistService;
            _passwordPolicyService = passwordPolicyService;
            _passwordHistoryService = passwordHistoryService;
            _mfaService = mfaService;
            _captchaService = captchaService;
            _securitySettingsService = securitySettingsService;
            _refreshTokenService = refreshTokenService;
            _secureMemoryService = secureMemoryService;
            _passwordResetService = passwordResetService;
            _dataExportService = dataExportService;
        }

        #region DTOs

        /// <summary>
        /// درخواست ورود
        /// </summary>
        public class LoginRequestDto
        {
            public string UserName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        /// <summary>
        /// پاسخ ورود
        /// </summary>
        public class LoginResponseDto
        {
            public string Token { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
            public string UserName { get; set; } = string.Empty;
            public List<string> Roles { get; set; } = new();
            /// <summary>
            /// آیا نیاز به MFA دارد
            /// </summary>
            public bool RequiresMfa { get; set; } = false;
            /// <summary>
            /// توکن موقت برای تایید MFA
            /// </summary>
            public string? MfaToken { get; set; }
            /// <summary>
            /// زمان انقضای کد OTP (ثانیه)
            /// </summary>
            public int? OtpExpirySeconds { get; set; }
            /// <summary>
            /// شماره موبایل ماسک شده
            /// </summary>
            public string? MaskedMobileNumber { get; set; }
            /// <summary>
            /// شناسه CAPTCHA
            /// </summary>
            public string? CaptchaId { get; set; }
            /// <summary>
            /// تصویر CAPTCHA به صورت Base64
            /// </summary>
            public string? CaptchaImage { get; set; }
            public long UserId { get; set; }
            public Guid PublicId { get; set; }
            public string FullName { get; set; }
        }

        // ============================================
        // DTOs برای MFA با پیامک (FIA_UAU.5)
        // ============================================

        /// <summary>
        /// پاسخ راه‌اندازی MFA
        /// </summary>
        public class MfaSetupResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            /// <summary>
            /// کدهای بازیابی
            /// </summary>
            public string[] RecoveryCodes { get; set; } = Array.Empty<string>();
        }

        /// <summary>
        /// درخواست تایید MFA در لاگین
        /// </summary>
        public class MfaVerifyRequestDto
        {
            /// <summary>
            /// توکن موقت MFA
            /// </summary>
            public string MfaToken { get; set; } = string.Empty;
            /// <summary>
            /// کد OTP دریافت شده از پیامک
            /// </summary>
            public string Code { get; set; } = string.Empty;
            /// <summary>
            /// شناسه CAPTCHA
            /// </summary>
            public string CaptchaId { get; set; } = string.Empty;
            /// <summary>
            /// کد CAPTCHA وارد شده توسط کاربر
            /// </summary>
            public string CaptchaCode { get; set; } = string.Empty;
        }

        /// <summary>
        /// درخواست فعال/غیرفعال‌سازی MFA
        /// </summary>
        public class MfaSetRequestDto
        {
            public string CurrentPassword { get; set; } = string.Empty;
            /// <summary>true = فعال، false = غیرفعال</summary>
            public bool Enable { get; set; }
        }

        /// <summary>
        /// پاسخ عمومی MFA
        /// </summary>
        public class MfaResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        /// <summary>
        /// پاسخ وضعیت MFA
        /// </summary>
        public class MfaStatusResponseDto
        {
            public bool IsEnabled { get; set; }
            public string? EnabledAt { get; set; }
            public string? LastUsedAt { get; set; }
            public int RecoveryCodesRemaining { get; set; }
            public string? MaskedMobileNumber { get; set; }
        }

        /// <summary>
        /// پاسخ ارسال مجدد OTP
        /// </summary>
        public class ResendOtpResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int? OtpExpirySeconds { get; set; }
            public string? MaskedMobileNumber { get; set; }
            /// <summary>
            /// شناسه CAPTCHA جدید
            /// </summary>
            public string? CaptchaId { get; set; }
            /// <summary>
            /// تصویر CAPTCHA جدید
            /// </summary>
            public string? CaptchaImage { get; set; }
        }

        /// <summary>
        /// پاسخ CAPTCHA
        /// </summary>
        public class CaptchaResponseDto
        {
            /// <summary>
            /// شناسه CAPTCHA
            /// </summary>
            public string CaptchaId { get; set; } = string.Empty;
            /// <summary>
            /// تصویر CAPTCHA به صورت Base64
            /// </summary>
            public string CaptchaImage { get; set; } = string.Empty;
        }

        /// <summary>
        /// درخواست تغییر رمز عبور
        /// </summary>
        public class ChangePasswordRequestDto
        {
            public string CurrentPassword { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
            public string ConfirmNewPassword { get; set; } = string.Empty;
        }

        /// <summary>
        /// پاسخ تغییر رمز عبور
        /// </summary>
        public class ChangePasswordResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        /// <summary>
        /// پاسخ خروج
        /// </summary>
        public class LogoutResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        /// <summary>
        /// پاسخ سیاست رمز عبور
        /// </summary>
        public class PasswordPolicyResponseDto
        {
            public int MinimumLength { get; set; }
            public int MaximumLength { get; set; }
            public bool RequireUppercase { get; set; }
            public bool RequireLowercase { get; set; }
            public bool RequireDigit { get; set; }
            public bool RequireSpecialCharacter { get; set; }
            public string SpecialCharacters { get; set; } = string.Empty;
        }

        // ============================================
        // DTOs برای بازیابی رمز عبور (Forgot Password)
        // ============================================

        /// <summary>
        /// درخواست ارسال کد بازیابی رمز عبور
        /// </summary>
        public class ForgotPasswordRequestDto
        {
            /// <summary>
            /// نام کاربری
            /// </summary>
            public string UserName { get; set; } = string.Empty;
        }

        /// <summary>
        /// پاسخ ارسال کد بازیابی رمز عبور
        /// </summary>
        public class ForgotPasswordResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            /// <summary>
            /// شماره موبایل ماسک شده
            /// </summary>
            public string? MaskedMobileNumber { get; set; }
            /// <summary>
            /// زمان انقضای کد OTP (ثانیه)
            /// </summary>
            public int? OtpExpirySeconds { get; set; }
        }

        /// <summary>
        /// درخواست تایید OTP بازیابی رمز عبور
        /// </summary>
        public class VerifyPasswordResetOtpRequestDto
        {
            /// <summary>
            /// نام کاربری
            /// </summary>
            public string UserName { get; set; } = string.Empty;
            /// <summary>
            /// کد OTP دریافت شده از پیامک
            /// </summary>
            public string OtpCode { get; set; } = string.Empty;
        }

        /// <summary>
        /// پاسخ تایید OTP بازیابی رمز عبور
        /// </summary>
        public class VerifyPasswordResetOtpResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            /// <summary>
            /// توکن ریست رمز عبور (موقت - 10 دقیقه)
            /// </summary>
            public string? PasswordResetToken { get; set; }
            /// <summary>
            /// زمان انقضای توکن (دقیقه)
            /// </summary>
            public int? TokenExpiryMinutes { get; set; }
        }

        /// <summary>
        /// درخواست تغییر رمز عبور با توکن ریست
        /// </summary>
        public class ResetPasswordWithTokenRequestDto
        {
            /// <summary>
            /// توکن ریست رمز عبور
            /// </summary>
            public string PasswordResetToken { get; set; } = string.Empty;
            /// <summary>
            /// رمز عبور جدید
            /// </summary>
            public string NewPassword { get; set; } = string.Empty;
            /// <summary>
            /// تکرار رمز عبور جدید
            /// </summary>
            public string ConfirmNewPassword { get; set; } = string.Empty;
        }

        /// <summary>
        /// پاسخ ریست رمز عبور
        /// </summary>
        public class ResetPasswordResponseDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        #endregion

        /// <summary>
        /// ورود کاربر
        /// پیاده‌سازی الزامات:
        /// - بررسی وضعیت قفل حساب قبل از احراز هویت
        /// - ثبت تلاش‌های ناموفق و قفل حساب پس از رسیدن به آستانه
        /// - ارسال کد OTP پیامکی برای MFA
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(423)] // Locked
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            // ============================================
            // استخراج اطلاعات از HttpContext برای Audit Log
            // ============================================
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);

            // ============================================
            // الزام 1: بررسی وضعیت قفل حساب کاربر
            // ============================================
            var lockoutStatus = await _accountLockoutService.GetLockoutStatusAsync(request.UserName);
            if (lockoutStatus.IsLockedOut)
            {
                await _auditLogService.LogEventAsync(
                    eventType: "Authentication",
                    entityType: "User",
                    entityId: null,
                    isSuccess: false,
                    errorMessage: "تلاش برای ورود به حساب قفل شده",
                    ipAddress: ipAddress,
                    userName: request.UserName,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"تلاش ورود به حساب قفل شده: {request.UserName}",
                    ct: default);

                var lockoutMessage = lockoutStatus.LockoutEndTime.HasValue
                    ? $"حساب کاربری قفل است. لطفاً {lockoutStatus.RemainingLockoutSeconds} ثانیه دیگر تلاش کنید."
                    : "حساب کاربری به صورت دائمی قفل شده است. لطفاً با مدیر سیستم تماس بگیرید.";

                return StatusCode(423, new { message = lockoutMessage, lockoutStatus });
            }

            // ============================================
            // الزام 2: پیدا کردن کاربر با نام کاربری
            // ============================================
            var user = await _context.tblUsers
                .FirstOrDefaultAsync(u => u.UserName == request.UserName);

            // ============================================
            // الزام 3: بررسی صحت رمز عبور
            // ============================================
            var passwordValid = false;
            try
            {
                if (user != null)
                {
                    passwordValid = _passwordHasher.VerifyPassword(user.Password, request.Password);
                }
            }
            finally
            {
                // ============================================
                // پاکسازی رمز عبور از حافظه (FDP_RIP.2)
                // ============================================
                var passwordCopy = request.Password;
                _secureMemoryService.ClearString(ref passwordCopy);
            }

            if (user == null || !passwordValid)
            {
                var newLockoutStatus = await _accountLockoutService.RecordFailedAttemptAsync(
                    request.UserName,
                    ipAddress,
                    default);

                await _auditLogService.LogEventAsync(
                    eventType: "Authentication",
                    entityType: "User",
                    entityId: user?.Id.ToString(),
                    isSuccess: false,
                    errorMessage: "نام کاربری یا رمز عبور اشتباه است",
                    ipAddress: ipAddress,
                    userName: request.UserName,
                    userId: user?.Id,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"تلاش ناموفق {newLockoutStatus.FailedAttempts} برای ورود با نام کاربری: {request.UserName}",
                    ct: default);

                var errorMessage = newLockoutStatus.IsLockedOut
                    ? "حساب کاربری قفل شد. لطفاً بعداً تلاش کنید."
                    : $"نام کاربری یا رمز عبور اشتباه است. {newLockoutStatus.RemainingAttempts} تلاش باقی‌مانده.";

                return Unauthorized(new { message = errorMessage, remainingAttempts = newLockoutStatus.RemainingAttempts });
            }

            // ============================================
            // الزام 4: بررسی وضعیت فعال بودن کاربر
            // ============================================
            if (user.IsActive == false)
            {
                await _auditLogService.LogEventAsync(
                    eventType: "Authentication",
                    entityType: "User",
                    entityId: user.Id.ToString(),
                    isSuccess: false,
                    errorMessage: "حساب کاربری غیرفعال است",
                    ipAddress: ipAddress,
                    userName: user.UserName,
                    userId: user.Id,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"تلاش ورود به حساب غیرفعال: {user.UserName}",
                    ct: default);

                return StatusCode(403, new { message = "حساب کاربری غیرفعال است. لطفاً با مدیر سیستم تماس بگیرید." });
            }

            // ============================================
            // الزام 5: بررسی MFA (FIA_UAU.5)
            // اگر MFA فعال است، ارسال کد OTP پیامکی
            // ============================================
            // بررسی تنظیمات MFA
            var mfaSettings = await _securitySettingsService.GetMfaSettingsAsync();

            // اگر MFA در سیستم فعال و کاربر MFA را فعال کرده است
            if (mfaSettings.IsEnabled && user.IsMfaEnabled)
            {
                // بررسی وجود شماره موبایل
                if (string.IsNullOrEmpty(user.MobileNumber))
                {
                    await _auditLogService.LogEventAsync(
                        eventType: "MFA_Error",
                        entityType: "User",
                        entityId: user.Id.ToString(),
                        isSuccess: false,
                        errorMessage: "شماره موبایل ثبت نشده",
                        ipAddress: ipAddress,
                        userName: user.UserName,
                        userId: user.Id,
                        operatingSystem: operatingSystem,
                        userAgent: userAgent,
                        description: $"MFA فعال است اما شماره موبایل برای کاربر {user.UserName} ثبت نشده",
                        ct: default);

                    return StatusCode(500, new { message = "خطا در ارسال کد تایید. لطفاً با مدیر سیستم تماس بگیرید." });
                }

                // ارسال کد OTP
                var smsResult = await _mfaService.GenerateAndSendOtpAsync(user.MobileNumber, user.Id, default);

                if (!smsResult.IsSuccess)
                {
                    return StatusCode(500, new { message = smsResult.ErrorMessage ?? "خطا در ارسال پیامک" });
                }

                // تولید توکن موقت MFA
                var mfaToken = GenerateMfaToken(user.Id, user.UserName);

                await _auditLogService.LogEventAsync(
                    eventType: "MFA_OTP_Sent",
                    entityType: "User",
                    entityId: user.Id.ToString(),
                    isSuccess: true,
                    ipAddress: ipAddress,
                    userName: user.UserName,
                    userId: user.Id,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"کد OTP به شماره {smsResult.MaskedMobileNumber} ارسال شد",
                    ct: default);

                // بررسی نیاز به CAPTCHA و تولید آن
                string? captchaId = null;
                string? captchaImage = null;
                if (await _captchaService.IsRequiredOnMfaAsync())
                {
                    await _captchaService.GenerateCaptchaAsync();
                    var captchaResult = _captchaService.GetLastCaptcha();
                    captchaId = captchaResult?.CaptchaId;
                    captchaImage = captchaResult?.ImageBase64;
                }

                return Ok(new LoginResponseDto
                {
                    RequiresMfa = true,
                    MfaToken = mfaToken,
                    UserName = user.UserName,
                    OtpExpirySeconds = smsResult.ExpirySeconds,
                    MaskedMobileNumber = smsResult.MaskedMobileNumber,
                    CaptchaId = captchaId,
                    CaptchaImage = captchaImage,
                    PublicId = user.PublicId,
                    FullName = user.FirstName + " " + user.LastName
                });
            }

            // ============================================
            // بررسی اجباری بودن MFA
            // ============================================
            if (mfaSettings.IsEnabled && mfaSettings.IsRequired && !user.IsMfaEnabled)
            {
                await _auditLogService.LogEventAsync(
                    eventType: "Authentication",
                    entityType: "User",
                    entityId: user.Id.ToString(),
                    isSuccess: false,
                    errorMessage: "MFA اجباری است اما فعال نشده",
                    ipAddress: ipAddress,
                    userName: user.UserName,
                    userId: user.Id,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"کاربر {user.UserName} سعی در ورود بدون فعال‌سازی MFA اجباری",
                    ct: default);

                return StatusCode(403, new { message = "احراز هویت دو مرحله‌ای برای ورود الزامی است. لطفاً ابتدا MFA را فعال کنید." });
            }

            // ============================================
            // الزام 6: بررسی گروه‌های کاربر (نقش/سمت)
            // ============================================
            var userGroups = await _context.tblUserGrps
                .Where(ug => ug.tblUserId == user.Id)
                .Include(ug => ug.tblGrp)
                .ToListAsync();

            if (!userGroups.Any())
            {
                await _auditLogService.LogEventAsync(
                    eventType: "Authentication",
                    entityType: "User",
                    entityId: user.Id.ToString(),
                    isSuccess: false,
                    errorMessage: "برای این کاربر هیچ گروه/سمتی ثبت نشده است",
                    ipAddress: ipAddress,
                    userName: user.UserName,
                    userId: user.Id,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"کاربر {user.UserName} بدون گروه/سمت",
                    ct: default);

                return Problem(
                    detail: "برای این کاربر هیچ گروه/سمتی ثبت نشده است.",
                    statusCode: 403,
                    title: "Forbidden");
            }

            var currentUserGrp = userGroups.First();
            var currentUserGrpId = currentUserGrp.Id;

            var roles = userGroups
                .Select(ug => ug.tblGrp.Title)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            // ============================================
            // رفع Ghost Session: اگر در همین مرورگر قبلاً لاگین شده، توکن قبلی را Revoke کن
            // بدون این کار، توکن قبلی در دیتابیس فعال می‌ماند و به عنوان نشست فعال شمرده می‌شود
            // ============================================
            if (Request.Cookies.TryGetValue("refreshToken", out var existingRefreshToken))
            {
                await _refreshTokenService.RevokeTokenAsync(
                    existingRefreshToken,
                    "لاگین جدید از همان مرورگر");
            }

            // ============================================
            // الزام 7: ساخت توکن JWT
            // ============================================
            var token = GenerateJwtToken(user.UserName, user.Id, roles, currentUserGrpId);

            // ============================================
            // تنظیم توکن به عنوان HttpOnly Cookie
            // امنیت: JavaScript نمی‌تواند به توکن دسترسی داشته باشد
            // ============================================
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,                        // JavaScript نمی‌تواند بخواند (حفاظت XSS)
                Secure = true,                          // فقط روی HTTPS
                SameSite = SameSiteMode.Strict,         // محافظت در برابر CSRF
                Expires = token.ExpiresAt,              // هم‌زمان با انقضای توکن
                Path = "/",                             // برای کل دامنه
                Domain = null                           // فقط همان دامنه
            };

            Response.Cookies.Append("accessToken", token.TokenString, cookieOptions);

            // ============================================
            // ایجاد Refresh Token (7 روز)
            // ============================================
            var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(
                user.Id,
                ipAddress,
                userAgent,
                operatingSystem);

            // تنظیم Refresh Token به عنوان HttpOnly Cookie
            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                Path = "/"
            };

            Response.Cookies.Append("refreshToken", refreshToken, refreshCookieOptions);

            // ============================================
            // الزام 8: ریست شمارنده تلاش‌های ناموفق
            // ============================================
            await _accountLockoutService.ResetFailedAttemptsAsync(request.UserName, default);

            user.LastLoginAt = BaseEntity.ToPersianDateTime(DateTime.UtcNow);
            user.IpAddress = ipAddress;
            _context.tblUsers.Update(user);
            await _context.SaveChangesAsync();
            // ============================================
            // الزام 9: ثبت Audit Log
            // ============================================
            await _auditLogService.LogEventAsync(
                eventType: "Authentication",
                entityType: "User",
                entityId: user.Id.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userName: user.UserName,
                userId: user.Id,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"ورود موفق کاربر {user.UserName} با نقش‌های: {string.Join(", ", roles)}",
                ct: default);

            // ============================================
            // پاسخ به کلاینت (بدون توکن در Body)
            // توکن فقط در HttpOnly Cookie ذخیره شده است
            // JavaScript به آن دسترسی ندارد (حفاظت در برابر XSS)
            // ============================================
            return Ok(new LoginResponseDto
            {
                ExpiresAt = token.ExpiresAt,
                UserName = user.UserName,
                Roles = roles,
                PublicId = user.PublicId,
                FullName = user.FirstName + " " + user.LastName
            });
        }

        /// <summary>
        /// خروج کاربر (Logout)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(LogoutResponseDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<LogoutResponseDto>> Logout()
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var userName = User.Identity?.Name ?? "Unknown";

            // ============================================
            // دریافت توکن از Cookie یا Header
            // ============================================
            string? token = null;

            // اولویت 1: خواندن از Cookie
            if (HttpContext.Request.Cookies.TryGetValue("accessToken", out var cookieToken))
            {
                token = cookieToken;
            }
            // اولویت 2: خواندن از Header
            else
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    token = authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { message = "توکن معتبر نیست" });
            }

            // ============================================
            // باطل کردن Refresh Token
            // ============================================
            if (HttpContext.Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
            {
                await _refreshTokenService.RevokeTokenAsync(
                    refreshToken,
                    "User logout");
            }

            // ============================================
            // حذف HttpOnly Cookies
            // ============================================
            Response.Cookies.Delete("accessToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });

            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });

            // ============================================
            // افزودن توکن به لیست سیاه
            // ============================================
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var expirationTime = jwtToken.ValidTo;

            await _tokenBlacklistService.BlacklistTokenAsync(
                token,
                expirationTime,
                userName,
                "User logout",
                default);

            await _auditLogService.LogEventAsync(
                eventType: "Logout",
                entityType: "User",
                entityId: userName,
                isSuccess: true,
                ipAddress: ipAddress,
                userName: userName,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"خروج موفق کاربر {userName}",
                ct: default);

            return Ok(new LogoutResponseDto
            {
                Success = true,
                Message = "خروج با موفقیت انجام شد"
            });
        }

        /// <summary>
        /// تمدید Access Token با استفاده از Refresh Token
        /// این endpoint خودکار توسط Frontend فراخوانی می‌شود
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<LoginResponseDto>> RefreshToken()
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);

            // خواندن Refresh Token از Cookie
            if (!HttpContext.Request.Cookies.TryGetValue("refreshToken", out var refreshTokenValue))
            {
                await _auditLogService.LogEventAsync(
                    eventType: "RefreshToken",
                    entityType: "RefreshToken",
                    entityId: null,
                    isSuccess: false,
                    errorMessage: "Refresh Token در Cookie یافت نشد",
                    ipAddress: ipAddress,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: "تلاش برای تمدید بدون Refresh Token",
                    ct: default);

                return Unauthorized(new { message = "Refresh Token یافت نشد" });
            }

            // اعتبارسنجی Refresh Token
            var validationResult = await _refreshTokenService.ValidateAndUseRefreshTokenAsync(
                refreshTokenValue,
                ipAddress);

            if (validationResult.Status == Application.DTOs.RefreshTokenValidationStatus.RaceCondition)
            {
                // Race Condition (تب‌های همزمان): Cookie را حذف نمی‌کنیم!
                // تب دیگر قبلاً Cookie جدید را set کرده - کلاینت با retry از Cookie جدید استفاده می‌کند
                return Unauthorized(new { message = validationResult.Message ?? "نشست در حال به‌روزرسانی است. لطفاً دوباره تلاش کنید.", raceCondition = true });
            }

            if (!validationResult.IsSuccess)
            {
                // Refresh Token واقعاً نامعتبر - حذف Cookies و خروج
                Response.Cookies.Delete("accessToken");
                Response.Cookies.Delete("refreshToken");

                return Unauthorized(new { message = validationResult.Message ?? "Refresh Token نامعتبر است. لطفاً دوباره لاگین کنید." });
            }

            // دریافت اطلاعات کاربر
            var user = await _context.tblUsers.FindAsync(validationResult.UserId!.Value);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { message = "کاربر یافت نشد یا غیرفعال است" });
            }

            // دریافت نقش‌ها
            var userGroups = await _context.tblUserGrps
                .Where(ug => ug.tblUserId == user.Id)
                .Include(ug => ug.tblGrp)
                .ToListAsync();

            var roles = userGroups
                .Select(ug => ug.tblGrp.Title)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            var currentUserGrpId = userGroups.First().Id;

            // ساخت Access Token جدید (15 دقیقه)
            var newAccessToken = GenerateJwtToken(user.UserName, user.Id, roles, currentUserGrpId);

            // ساخت Refresh Token جدید (Rotation برای امنیت)
            var newRefreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(
                user.Id,
                ipAddress,
                userAgent,
                operatingSystem);

            // تنظیم Cookies جدید
            Response.Cookies.Append("accessToken", newAccessToken.TokenString, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = newAccessToken.ExpiresAt,
                Path = "/"
            });

            Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                Path = "/"
            });

            await _auditLogService.LogEventAsync(
                eventType: "RefreshToken",
                entityType: "RefreshToken",
                entityId: validationResult.UserId!.Value.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userName: user.UserName,
                userId: user.Id,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: "تمدید موفق Access Token با Refresh Token",
                ct: default);

            // توکن فقط در HttpOnly Cookie ارسال شده - در Body نیست
            return Ok(new LoginResponseDto
            {
                ExpiresAt = newAccessToken.ExpiresAt,
                UserName = user.UserName,
                Roles = roles,
                PublicId = user.PublicId
            });
        }

        /// <summary>
        /// خروج از تمام دستگاه‌ها
        /// این متد تمام نشست‌های فعال کاربر را باطل می‌کند
        /// الزام FTA_MCS.1: مدیریت نشست‌های همزمان
        /// </summary>
        [HttpPost("logout-all")]
        [Authorize]
        [ProducesResponseType(typeof(LogoutResponseDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<LogoutResponseDto>> LogoutAll()
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var userName = User.Identity?.Name ?? "Unknown";
            var userId = User.GetUserId();

            if (userId == null)
            {
                return Unauthorized(new { message = "شناسه کاربر یافت نشد" });
            }

            // ============================================
            // 1. باطل کردن تمام توکن‌ها در حافظه (Blacklist)
            // این کار باعث می‌شود توکن‌های JWT فعلی نامعتبر شوند
            // ============================================
            await _tokenBlacklistService.BlacklistAllUserTokensAsync(
                userName,
                "Logout from all devices",
                default);

            // ============================================
            // 2. باطل کردن تمام Refresh Token‌ها در دیتابیس
            // این کار باعث می‌شود که GetUserActiveSessionsCountAsync 
            // دیگر این نشست‌ها را به عنوان فعال شمارش نکند
            // و محدودیت نشست همزمان (FTA_MCS.1) به درستی کار کند
            // ============================================
            await _refreshTokenService.RevokeAllUserTokensAsync(
                userId.Value,
                "Logout from all devices",
                default);

            // ============================================
            // 3. حذف HttpOnly Cookies
            // ============================================
            Response.Cookies.Delete("accessToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });

            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });

            await _auditLogService.LogEventAsync(
                eventType: "LogoutAll",
                entityType: "User",
                entityId: userName,
                isSuccess: true,
                ipAddress: ipAddress,
                userName: userName,
                userId: userId.Value,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"خروج از تمام دستگاه‌ها برای کاربر {userName}",
                ct: default);

            return Ok(new LogoutResponseDto
            {
                Success = true,
                Message = "خروج از تمام دستگاه‌ها با موفقیت انجام شد"
            });
        }

        /// <summary>
        /// تغییر رمز عبور
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(ChangePasswordResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ChangePasswordResponseDto>> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var userName = User.Identity?.Name ?? "Unknown";

            var user = await _context.tblUsers
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null)
            {
                return NotFound(new { message = "کاربر یافت نشد" });
            }

            // ============================================
            // بررسی صحت رمز عبور فعلی با پاکسازی امن (FDP_RIP.2)
            // ============================================
            var currentPasswordValid = false;
            try
            {
                currentPasswordValid = _passwordHasher.VerifyPassword(user.Password, request.CurrentPassword);
            }
            finally
            {
                // پاکسازی رمز عبور فعلی از حافظه
                var currentPasswordCopy = request.CurrentPassword;
                _secureMemoryService.ClearString(ref currentPasswordCopy);
            }

            if (!currentPasswordValid)
            {
                await _auditLogService.LogEventAsync(
                    eventType: "ChangePassword",
                    entityType: "User",
                    entityId: user.Id.ToString(),
                    isSuccess: false,
                    errorMessage: "رمز عبور فعلی اشتباه است",
                    ipAddress: ipAddress,
                    userName: userName,
                    userId: user.Id,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: "تلاش ناموفق برای تغییر رمز عبور - رمز فعلی اشتباه",
                    ct: default);

                return BadRequest(new { message = "رمز عبور فعلی اشتباه است" });
            }

            if (request.NewPassword != request.ConfirmNewPassword)
            {
                return BadRequest(new { message = "رمز عبور جدید و تکرار آن یکسان نیستند" });
            }

            var passwordValidation = _passwordPolicyService.ValidatePassword(request.NewPassword, userName);
            if (!passwordValidation.IsValid)
            {
                return BadRequest(new
                {
                    message = "رمز عبور جدید سیاست‌های امنیتی را رعایت نمی‌کند",
                    errors = passwordValidation.Errors
                });
            }

            if (request.CurrentPassword == request.NewPassword)
            {
                return BadRequest(new { message = "رمز عبور جدید نمی‌تواند با رمز عبور فعلی یکسان باشد" });
            }

            var policySettings = _passwordPolicyService.GetPolicySettings();
            var isInHistory = await _passwordHistoryService.IsPasswordInHistoryAsync(
                user.Id,
                request.NewPassword,
                policySettings.PasswordHistoryCount);

            if (isInHistory)
            {
                return BadRequest(new 
                { 
                    message = $"رمز عبور جدید نباید با {policySettings.PasswordHistoryCount} رمز عبور قبلی یکسان باشد" 
                });
            }

            // ============================================
            // Hash کردن رمز عبور جدید با پاکسازی امن (FDP_RIP.2)
            // ============================================
            string newPasswordHash;
            try
            {
                newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            }
            finally
            {
                // پاکسازی رمز عبور جدید از حافظه
                var newPasswordCopy = request.NewPassword;
                _secureMemoryService.ClearString(ref newPasswordCopy);
                var confirmPasswordCopy = request.ConfirmNewPassword;
                _secureMemoryService.ClearString(ref confirmPasswordCopy);
            }

            user.Password = newPasswordHash;
            user.SetPasswordLastChangedAt(DateTime.UtcNow);
            user.MustChangePassword = false;
            await _context.SaveChangesAsync();

            await _passwordHistoryService.AddToHistoryAsync(user.Id, newPasswordHash, ipAddress);
            await _passwordHistoryService.CleanupOldHistoryAsync(user.Id, policySettings.PasswordHistoryCount);

            await _tokenBlacklistService.BlacklistAllUserTokensAsync(userName, "Password changed", default);

            await _auditLogService.LogEventAsync(
                eventType: "ChangePassword",
                entityType: "User",
                entityId: user.Id.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userName: userName,
                userId: user.Id,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"رمز عبور کاربر {userName} با موفقیت تغییر یافت",
                ct: default);

            return Ok(new ChangePasswordResponseDto
            {
                Success = true,
                Message = "رمز عبور با موفقیت تغییر یافت. لطفاً مجدداً وارد شوید."
            });
        }

        /// <summary>
        /// دریافت سیاست رمز عبور
        /// </summary>
        [HttpGet("password-policy")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PasswordPolicyResponseDto), 200)]
        public async Task<ActionResult<PasswordPolicyResponseDto>> GetPasswordPolicy()
        {
            try
            {
                var settings = _passwordPolicyService.GetPolicySettings();
                var response = new PasswordPolicyResponseDto
                {
                    MinimumLength = settings.MinimumLength,
                    MaximumLength = settings.MaximumLength,
                    RequireUppercase = settings.RequireUppercase,
                    RequireLowercase = settings.RequireLowercase,
                    RequireDigit = settings.RequireDigit,
                    RequireSpecialCharacter = settings.RequireSpecialCharacter,
                    SpecialCharacters = settings.SpecialCharacters
                };

                var protectedResponse = await ProtectReadPayloadAsync(response, "AuthPasswordPolicy");
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// اعتبارسنجی قدرت رمز عبور
        /// </summary>
        [HttpPost("validate-password")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        public ActionResult ValidatePassword([FromBody] string password)
        {
            var result = _passwordPolicyService.ValidatePassword(password);
            return Ok(new
            {
                isValid = result.IsValid,
                errors = result.Errors,
                strengthScore = result.StrengthScore,
                strengthDescription = result.StrengthDescription
            });
        }

        #region MFA Endpoints - SMS Based (FIA_UAU.5)

        /// <summary>
        /// فعال یا غیرفعال‌سازی MFA
        /// نیاز به رمز عبور فعلی. در بدنه enable=true برای فعال و enable=false برای غیرفعال.
        /// </summary>
        [HttpPost("mfa")]
        [Authorize]
        [ProducesResponseType(typeof(MfaSetupResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<MfaSetupResponseDto>> SetMfa([FromBody] MfaSetRequestDto request)
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var userName = User.Identity?.Name ?? "Unknown";

            var user = await _context.tblUsers
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null)
            {
                return NotFound(new { message = "کاربر یافت نشد" });
            }

            // تایید رمز عبور
            if (!_passwordHasher.VerifyPassword(user.Password, request.CurrentPassword))
            {
                return BadRequest(new { message = "رمز عبور اشتباه است" });
            }

            if (request.Enable)
            {
                // ========== فعال‌سازی ==========
                var mfaSettings = await _securitySettingsService.GetMfaSettingsAsync();
                if (!mfaSettings.IsEnabled)
                {
                    return BadRequest(new { message = "احراز هویت دو مرحله‌ای در سیستم غیرفعال است" });
                }

                if (user.IsMfaEnabled)
                {
                    return BadRequest(new { message = "MFA قبلاً برای این حساب فعال شده است" });
                }

                if (string.IsNullOrEmpty(user.MobileNumber))
                {
                    return BadRequest(new { message = "برای فعال‌سازی MFA، ابتدا شماره موبایل خود را ثبت کنید" });
                }

                var recoveryCodes = _mfaService.GenerateRecoveryCodes(mfaSettings.RecoveryCodesCount);
                var hashedCodes = recoveryCodes.Select(c => _mfaService.HashRecoveryCode(c)).ToArray();
                user.MfaRecoveryCodes = string.Join(";", hashedCodes);

                user.IsMfaEnabled = true;
                user.SetMfaEnabledAt(DateTime.UtcNow);
                await _context.SaveChangesAsync();

                await _tokenBlacklistService.BlacklistAllUserTokensAsync(userName, "MFA enabled", default);

                await _auditLogService.LogEventAsync(
                    eventType: "MFA_Enabled",
                    entityType: "User",
                    entityId: user.Id.ToString(),
                    isSuccess: true,
                    ipAddress: ipAddress,
                    userName: userName,
                    userId: user.Id,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"MFA با موفقیت برای کاربر {userName} فعال شد",
                    ct: default);

                return Ok(new MfaSetupResponseDto
                {
                    Success = true,
                    Message = "MFA با موفقیت فعال شد. کدهای بازیابی را در جای امن ذخیره کنید.",
                    RecoveryCodes = recoveryCodes
                });
            }
            else
            {
                // ========== غیرفعال‌سازی ==========
                if (!user.IsMfaEnabled)
                {
                    return BadRequest(new { message = "MFA فعال نیست" });
                }

                user.IsMfaEnabled = false;
                user.MfaSecretKey = null;
                user.MfaRecoveryCodes = null;
                user.MfaEnabledAt = null;
                user.MfaLastUsedAt = null;
                await _context.SaveChangesAsync();

                await _auditLogService.LogEventAsync(
                    eventType: "MFA_Disabled",
                    entityType: "User",
                    entityId: user.Id.ToString(),
                    isSuccess: true,
                    ipAddress: ipAddress,
                    userName: userName,
                    userId: user.Id,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"MFA برای کاربر {userName} غیرفعال شد",
                    ct: default);

                return Ok(new MfaSetupResponseDto
                {
                    Success = true,
                    Message = "MFA با موفقیت غیرفعال شد",
                    RecoveryCodes = Array.Empty<string>()
                });
            }
        }

        /// <summary>
        /// دریافت وضعیت MFA
        /// </summary>
        [HttpGet("mfa/status")]
        [Authorize]
        [ProducesResponseType(typeof(MfaStatusResponseDto), 200)]
        public async Task<ActionResult<MfaStatusResponseDto>> GetMfaStatus()
        {
            try
            {
            var userName = User.Identity?.Name ?? "Unknown";

            var user = await _context.tblUsers
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null)
            {
                return NotFound(new { message = "کاربر یافت نشد" });
            }

            var recoveryCodesCount = 0;
            if (!string.IsNullOrEmpty(user.MfaRecoveryCodes))
            {
                recoveryCodesCount = user.MfaRecoveryCodes.Split(';', StringSplitOptions.RemoveEmptyEntries).Length;
            }

                var response = new MfaStatusResponseDto
                {
                    IsEnabled = user.IsMfaEnabled,
                    EnabledAt = user.MfaEnabledAt,
                    LastUsedAt = user.MfaLastUsedAt,
                    RecoveryCodesRemaining = recoveryCodesCount,
                    MaskedMobileNumber = MaskMobileNumber(user.MobileNumber)
                };

                var protectedResponse = await ProtectReadPayloadAsync(
                    response,
                    "MfaStatus",
                    user.Id.ToString());
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// تایید MFA با کد OTP پیامکی
        /// مرحله دوم لاگین
        /// </summary>
        [HttpPost("mfa/verify")]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<LoginResponseDto>> VerifyMfa([FromBody] MfaVerifyRequestDto request)
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);

            // ============================================
            // اعتبارسنجی CAPTCHA (اگر فعال باشد)
            // ============================================
            if (await _captchaService.IsRequiredOnMfaAsync())
            {
                if (!_captchaService.ValidateCaptcha(request.CaptchaId, request.CaptchaCode, out string captchaMessage))
                {
                    await _auditLogService.LogEventAsync(
                        eventType: "MFA_Captcha_Failed",
                        entityType: "User",
                        entityId: null,
                        isSuccess: false,
                        errorMessage: captchaMessage,
                        ipAddress: ipAddress,
                        operatingSystem: operatingSystem,
                        userAgent: userAgent,
                        description: "تلاش ناموفق MFA - کپچا نادرست",
                        ct: default);

                    // تولید CAPTCHA جدید
                    await _captchaService.GenerateCaptchaAsync();
                    var newCaptcha = _captchaService.GetLastCaptcha();

                    return BadRequest(new 
                    { 
                        message = captchaMessage,
                        captchaId = newCaptcha?.CaptchaId,
                        captchaImage = newCaptcha?.ImageBase64
                    });
                }
            }

            // اعتبارسنجی توکن MFA
            var mfaTokenData = ValidateMfaToken(request.MfaToken);
            if (mfaTokenData == null)
            {
                return Unauthorized(new { message = "توکن MFA نامعتبر یا منقضی شده است" });
            }

            var user = await _context.tblUsers
                .FirstOrDefaultAsync(u => u.Id == mfaTokenData.Value.UserId);

            if (user == null)
            {
                return NotFound(new { message = "کاربر یافت نشد" });
            }

            // بررسی قفل حساب
            var lockoutStatus = await _accountLockoutService.GetLockoutStatusAsync(user.UserName);
            if (lockoutStatus.IsLockedOut)
            {
                return StatusCode(423, new { message = "حساب کاربری قفل است", lockoutStatus });
            }

            bool isValid = false;
            bool isRecoveryCode = false;

            // بررسی کد OTP
            isValid = _mfaService.VerifyOtp(user.Id, request.Code);

            // اگر OTP نامعتبر بود، بررسی کد بازیابی
            if (!isValid && !string.IsNullOrEmpty(user.MfaRecoveryCodes))
            {
                var hashedCodes = user.MfaRecoveryCodes.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var hashedCode in hashedCodes)
                {
                    if (_mfaService.VerifyRecoveryCode(hashedCode, request.Code))
                    {
                        isValid = true;
                        isRecoveryCode = true;
                        // حذف کد استفاده شده
                        hashedCodes.Remove(hashedCode);
                        user.MfaRecoveryCodes = string.Join(";", hashedCodes);
                        break;
                    }
                }
            }

            if (!isValid)
            {
                var newLockoutStatus = await _accountLockoutService.RecordFailedAttemptAsync(
                    user.UserName,
                    ipAddress,
                    default);

                await _auditLogService.LogEventAsync(
                    eventType: "MFA_Verification_Failed",
                    entityType: "User",
                    entityId: user.Id.ToString(),
                    isSuccess: false,
                    errorMessage: "کد تایید نادرست",
                    ipAddress: ipAddress,
                    userName: user.UserName,
                    userId: user.Id,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"تلاش ناموفق تایید MFA برای کاربر {user.UserName}",
                    ct: default);

                return Unauthorized(new { message = "کد تایید نادرست است", remainingAttempts = newLockoutStatus.RemainingAttempts });
            }

            // به‌روزرسانی آخرین استفاده MFA
            user.SetMfaLastUsedAt(DateTime.UtcNow);
            await _context.SaveChangesAsync();

            // دریافت نقش‌های کاربر
            var userGroups = await _context.tblUserGrps
                .Where(ug => ug.tblUserId == user.Id)
                .Include(ug => ug.tblGrp)
                .ToListAsync();

            var currentUserGrp = userGroups.First();
            var roles = userGroups
                .Select(ug => ug.tblGrp.Title)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            // ============================================
            // رفع Ghost Session: اگر در همین مرورگر قبلاً لاگین شده، توکن قبلی را Revoke کن
            // ============================================
            if (Request.Cookies.TryGetValue("refreshToken", out var existingRefreshToken))
            {
                await _refreshTokenService.RevokeTokenAsync(
                    existingRefreshToken,
                    "لاگین جدید از همان مرورگر (MFA)");
            }

            // تولید توکن JWT
            var token = GenerateJwtToken(user.UserName, user.Id, roles, currentUserGrp.Id);

            // ============================================
            // ذخیره Access Token در HttpOnly Cookie
            // ============================================
            Response.Cookies.Append("accessToken", token.TokenString, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = token.ExpiresAt,
                Path = "/"
            });

            // ساخت و ذخیره Refresh Token در HttpOnly Cookie
            var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(
                user.Id,
                ipAddress,
                userAgent,
                operatingSystem);

            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                Path = "/"
            });

            // ریست شمارنده تلاش‌های ناموفق
            await _accountLockoutService.ResetFailedAttemptsAsync(user.UserName, default);

            await _auditLogService.LogEventAsync(
                eventType: "MFA_Verification_Success",
                entityType: "User",
                entityId: user.Id.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userName: user.UserName,
                userId: user.Id,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"تایید MFA موفق برای کاربر {user.UserName}" + 
                            (isRecoveryCode ? " (با کد بازیابی)" : " (با پیامک)"),
                ct: default);

            // توکن فقط در HttpOnly Cookie ارسال شده - در Body نیست
            return Ok(new LoginResponseDto
            {
                ExpiresAt = token.ExpiresAt,
                Roles = roles,
                PublicId = user.PublicId,
                FullName = user.FirstName + " " + user.LastName,
                UserName = user.UserName,
            });
        }

        /// <summary>
        /// ارسال مجدد کد OTP
        /// </summary>
        [HttpPost("mfa/resend-otp")]
        [ProducesResponseType(typeof(ResendOtpResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ResendOtpResponseDto>> ResendOtp([FromBody] MfaVerifyRequestDto request)
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);

            // اعتبارسنجی توکن MFA
            var mfaTokenData = ValidateMfaToken(request.MfaToken);
            if (mfaTokenData == null)
            {
                return Unauthorized(new { message = "توکن MFA نامعتبر یا منقضی شده است" });
            }

            var user = await _context.tblUsers
                .FirstOrDefaultAsync(u => u.Id == mfaTokenData.Value.UserId);

            if (user == null || string.IsNullOrEmpty(user.MobileNumber))
            {
                return NotFound(new { message = "کاربر یافت نشد" });
            }

            // ارسال مجدد کد OTP
            var smsResult = await _mfaService.GenerateAndSendOtpAsync(user.MobileNumber, user.Id, default);

            if (!smsResult.IsSuccess)
            {
                return StatusCode(500, new { message = smsResult.ErrorMessage ?? "خطا در ارسال پیامک" });
            }

            await _auditLogService.LogEventAsync(
                eventType: "MFA_OTP_Resent",
                entityType: "User",
                entityId: user.Id.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userName: user.UserName,
                userId: user.Id,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"کد OTP مجدداً به شماره {smsResult.MaskedMobileNumber} ارسال شد",
                ct: default);

            // تولید CAPTCHA جدید (اگر فعال باشد)
            string? captchaId = null;
            string? captchaImage = null;
            if (await _captchaService.IsRequiredOnMfaAsync())
            {
                await _captchaService.GenerateCaptchaAsync();
                var captchaResult = _captchaService.GetLastCaptcha();
                captchaId = captchaResult?.CaptchaId;
                captchaImage = captchaResult?.ImageBase64;
            }

            return Ok(new ResendOtpResponseDto
            {
                Success = true,
                Message = "کد تایید مجدداً ارسال شد",
                OtpExpirySeconds = smsResult.ExpirySeconds,
                MaskedMobileNumber = smsResult.MaskedMobileNumber,
                CaptchaId = captchaId,
                CaptchaImage = captchaImage
            });
        }

        /// <summary>
        /// تولید مجدد کدهای بازیابی
        /// </summary>
        [HttpPost("mfa/regenerate-recovery-codes")]
        [Authorize]
        [ProducesResponseType(typeof(MfaSetupResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<MfaSetupResponseDto>> RegenerateRecoveryCodes([FromBody] ChangePasswordRequestDto request)
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var userName = User.Identity?.Name ?? "Unknown";

            var user = await _context.tblUsers
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null)
            {
                return NotFound(new { message = "کاربر یافت نشد" });
            }

            if (!user.IsMfaEnabled)
            {
                return BadRequest(new { message = "MFA فعال نیست" });
            }

            // تایید رمز عبور
            if (!_passwordHasher.VerifyPassword(user.Password, request.CurrentPassword))
            {
                return BadRequest(new { message = "رمز عبور اشتباه است" });
            }

            // تولید کدهای بازیابی جدید
            var recoveryCodes = _mfaService.GenerateRecoveryCodes(10);
            var hashedCodes = recoveryCodes.Select(c => _mfaService.HashRecoveryCode(c)).ToArray();
            user.MfaRecoveryCodes = string.Join(";", hashedCodes);
            await _context.SaveChangesAsync();

            await _auditLogService.LogEventAsync(
                eventType: "MFA_RecoveryCodes_Regenerated",
                entityType: "User",
                entityId: user.Id.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userName: userName,
                userId: user.Id,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"کدهای بازیابی MFA برای کاربر {userName} مجدداً تولید شد",
                ct: default);

            return Ok(new MfaSetupResponseDto
            {
                Success = true,
                Message = "کدهای بازیابی جدید تولید شد",
                RecoveryCodes = recoveryCodes
            });
        }

        /// <summary>
        /// دریافت CAPTCHA جدید
        /// برای تازه‌سازی تصویر CAPTCHA
        /// </summary>
        [HttpGet("captcha")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CaptchaResponseDto), 200)]
        public async Task<ActionResult<CaptchaResponseDto>> GetCaptcha(CancellationToken ct = default)
        {
            // بررسی فعال بودن CAPTCHA
            if (!await _captchaService.IsEnabledAsync())
            {
                return BadRequest(new { message = "سرویس CAPTCHA غیرفعال است" });
            }

            await _captchaService.GenerateCaptchaAsync();
            var captchaResult = _captchaService.GetLastCaptcha();

            if (captchaResult == null)
            {
                return StatusCode(500, new { message = "خطا در تولید CAPTCHA" });
            }

            var response = new CaptchaResponseDto
            {
                CaptchaId = captchaResult.CaptchaId,
                CaptchaImage = captchaResult.ImageBase64
            };

            var protectedResponse = await ProtectReadPayloadAsync(response, "CaptchaChallenge", ct: ct);
            return Ok(protectedResponse);
        }

        #endregion

        #region Forgot Password Endpoints - بازیابی رمز عبور

        /// <summary>
        /// مرحله 1: درخواست ارسال کد بازیابی رمز عبور
        /// ============================================
        /// - جستجوی کاربر بر اساس نام کاربری
        /// - بررسی فعال بودن کاربر و وجود شماره موبایل
        /// - ارسال کد OTP به شماره موبایل
        /// - جلوگیری از User Enumeration (پاسخ یکسان برای کاربر موجود و ناموجود)
        /// ============================================
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ForgotPasswordResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(429)]
        public async Task<ActionResult<ForgotPasswordResponseDto>> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);

            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                return BadRequest(new { message = "نام کاربری الزامی است" });
            }

            // ============================================
            // بررسی وضعیت قفل حساب
            // ============================================
            var lockoutStatus = await _accountLockoutService.GetLockoutStatusAsync(request.UserName);
            if (lockoutStatus.IsLockedOut)
            {
                await _auditLogService.LogEventAsync(
                    eventType: "PasswordReset_Request",
                    entityType: "User",
                    entityId: null,
                    isSuccess: false,
                    errorMessage: "تلاش بازیابی رمز عبور حساب قفل شده",
                    ipAddress: ipAddress,
                    userName: request.UserName,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"تلاش بازیابی رمز عبور برای حساب قفل شده: {request.UserName}",
                    ct: default);

                // پیام عمومی برای جلوگیری از User Enumeration
                return Ok(new ForgotPasswordResponseDto
                {
                    Success = true,
                    Message = "در صورت وجود حساب کاربری، کد بازیابی به شماره موبایل ثبت شده ارسال خواهد شد."
                });
            }

            // ============================================
            // جستجوی کاربر
            // ============================================
            var user = await _context.tblUsers
                .FirstOrDefaultAsync(u => u.UserName == request.UserName);

            // ============================================
            // جلوگیری از User Enumeration
            // حتی اگر کاربر وجود نداشته باشد، پیام یکسان برگردانده می‌شود
            // ============================================
            if (user == null || !user.IsActive || string.IsNullOrEmpty(user.MobileNumber))
            {
                await _auditLogService.LogEventAsync(
                    eventType: "PasswordReset_Request",
                    entityType: "User",
                    entityId: user?.Id.ToString(),
                    isSuccess: false,
                    errorMessage: user == null ? "کاربر یافت نشد" : 
                                 !user.IsActive ? "حساب غیرفعال" : "شماره موبایل ثبت نشده",
                    ipAddress: ipAddress,
                    userName: request.UserName,
                    userId: user?.Id,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"درخواست بازیابی رمز عبور ناموفق برای: {request.UserName}",
                    ct: default);

                // پیام عمومی یکسان
                return Ok(new ForgotPasswordResponseDto
                {
                    Success = true,
                    Message = "در صورت وجود حساب کاربری، کد بازیابی به شماره موبایل ثبت شده ارسال خواهد شد."
                });
            }

            // ============================================
            // ارسال کد OTP
            // ============================================
            var smsResult = await _passwordResetService.GenerateAndSendPasswordResetOtpAsync(
                user.MobileNumber, user.Id, default);

            if (!smsResult.IsSuccess)
            {
                await _auditLogService.LogEventAsync(
                    eventType: "PasswordReset_OTP_Failed",
                    entityType: "User",
                    entityId: user.Id.ToString(),
                    isSuccess: false,
                    errorMessage: smsResult.ErrorMessage,
                    ipAddress: ipAddress,
                    userName: user.UserName,
                    userId: user.Id,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"خطا در ارسال OTP بازیابی برای کاربر {user.UserName}",
                    ct: default);

                return StatusCode(500, new { message = "خطا در ارسال کد بازیابی. لطفاً مجدداً تلاش کنید." });
            }

            await _auditLogService.LogEventAsync(
                eventType: "PasswordReset_OTP_Sent",
                entityType: "User",
                entityId: user.Id.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userName: user.UserName,
                userId: user.Id,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"کد بازیابی رمز عبور به شماره {smsResult.MaskedMobileNumber} ارسال شد",
                ct: default);

            return Ok(new ForgotPasswordResponseDto
            {
                Success = true,
                Message = "کد بازیابی با موفقیت ارسال شد",
                MaskedMobileNumber = smsResult.MaskedMobileNumber,
                OtpExpirySeconds = smsResult.ExpirySeconds
            });
        }

        /// <summary>
        /// مرحله 2: تایید کد OTP بازیابی رمز عبور
        /// ============================================
        /// - تایید کد OTP
        /// - حداکثر 3 بار تلاش ناموفق
        /// - در صورت موفقیت: تولید توکن ریست (10 دقیقه)
        /// ============================================
        /// </summary>
        [HttpPost("forgot-password/verify-otp")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(VerifyPasswordResetOtpResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<VerifyPasswordResetOtpResponseDto>> VerifyForgotPasswordOtp(
            [FromBody] VerifyPasswordResetOtpRequestDto request)
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);

            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.OtpCode))
            {
                return BadRequest(new { message = "نام کاربری و کد تایید الزامی است" });
            }

            // ============================================
            // جستجوی کاربر
            // ============================================
            var user = await _context.tblUsers
                .FirstOrDefaultAsync(u => u.UserName == request.UserName);

            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { message = "نام کاربری یا کد تایید نادرست است" });
            }

            // ============================================
            // بررسی وضعیت قفل حساب
            // ============================================
            var lockoutStatus = await _accountLockoutService.GetLockoutStatusAsync(user.UserName);
            if (lockoutStatus.IsLockedOut)
            {
                return StatusCode(423, new { message = "حساب کاربری قفل است. لطفاً بعداً تلاش کنید.", lockoutStatus });
            }

            // ============================================
            // تایید کد OTP
            // ============================================
            var isValid = _passwordResetService.VerifyPasswordResetOtp(user.Id, request.OtpCode);

            if (!isValid)
            {
                // ثبت تلاش ناموفق و بررسی محدودیت
                var remainingAttempts = _passwordResetService.RecordFailedOtpAttempt(user.Id);

                await _auditLogService.LogEventAsync(
                    eventType: "PasswordReset_OTP_Failed",
                    entityType: "User",
                    entityId: user.Id.ToString(),
                    isSuccess: false,
                    errorMessage: "کد بازیابی نادرست",
                    ipAddress: ipAddress,
                    userName: user.UserName,
                    userId: user.Id,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: remainingAttempts > 0
                        ? $"تلاش ناموفق تایید OTP بازیابی برای کاربر {user.UserName} - {remainingAttempts} تلاش باقی‌مانده"
                        : $"OTP بازیابی برای کاربر {user.UserName} باطل شد (حداکثر تلاش)",
                    ct: default);

                var errorMessage = remainingAttempts > 0
                    ? $"کد تایید نادرست است. {remainingAttempts} تلاش باقی‌مانده."
                    : "کد تایید باطل شد. لطفاً مجدداً درخواست ارسال کد دهید.";

                return Unauthorized(new { message = errorMessage, remainingAttempts });
            }

            // ============================================
            // تولید توکن ریست رمز عبور
            // ============================================
            var resetToken = GeneratePasswordResetToken(user.Id, user.UserName);

            // ذخیره توکن در کش برای اعتبارسنجی بعدی
            _passwordResetService.StorePasswordResetToken(user.Id, resetToken);

            await _auditLogService.LogEventAsync(
                eventType: "PasswordReset_OTP_Verified",
                entityType: "User",
                entityId: user.Id.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userName: user.UserName,
                userId: user.Id,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"کد بازیابی رمز عبور برای کاربر {user.UserName} تایید شد",
                ct: default);

            return Ok(new VerifyPasswordResetOtpResponseDto
            {
                Success = true,
                Message = "کد تایید معتبر است. لطفاً رمز عبور جدید را وارد کنید.",
                PasswordResetToken = resetToken,
                TokenExpiryMinutes = 10
            });
        }

        /// <summary>
        /// مرحله 3: تغییر رمز عبور با توکن ریست
        /// ============================================
        /// - اعتبارسنجی توکن ریست (JWT + Cache)
        /// - بررسی سیاست رمز عبور
        /// - بررسی تاریخچه رمز عبور
        /// - تغییر رمز و باطل کردن تمام توکن‌های فعال
        /// ============================================
        /// </summary>
        [HttpPost("forgot-password/reset")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ResetPasswordResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ResetPasswordResponseDto>> ResetPasswordWithToken(
            [FromBody] ResetPasswordWithTokenRequestDto request)
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);

            if (string.IsNullOrWhiteSpace(request.PasswordResetToken))
            {
                return BadRequest(new { message = "توکن ریست الزامی است" });
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword) || string.IsNullOrWhiteSpace(request.ConfirmNewPassword))
            {
                return BadRequest(new { message = "رمز عبور جدید و تکرار آن الزامی است" });
            }

            if (request.NewPassword != request.ConfirmNewPassword)
            {
                return BadRequest(new { message = "رمز عبور جدید و تکرار آن یکسان نیستند" });
            }

            // ============================================
            // اعتبارسنجی توکن ریست (JWT)
            // ============================================
            var resetTokenData = ValidatePasswordResetToken(request.PasswordResetToken);
            if (resetTokenData == null)
            {
                return Unauthorized(new { message = "توکن ریست نامعتبر یا منقضی شده است. لطفاً مجدداً درخواست بازیابی دهید." });
            }

            // ============================================
            // اعتبارسنجی توکن از کش (یکبار مصرف)
            // ============================================
            if (!_passwordResetService.ValidatePasswordResetToken(resetTokenData.Value.UserId, request.PasswordResetToken))
            {
                return Unauthorized(new { message = "توکن ریست قبلاً استفاده شده یا منقضی شده است." });
            }

            // ============================================
            // جستجوی کاربر
            // ============================================
            var user = await _context.tblUsers
                .FirstOrDefaultAsync(u => u.Id == resetTokenData.Value.UserId);

            if (user == null || !user.IsActive)
            {
                return NotFound(new { message = "کاربر یافت نشد یا غیرفعال است" });
            }

            // ============================================
            // بررسی سیاست رمز عبور
            // ============================================
            var passwordValidation = _passwordPolicyService.ValidatePassword(request.NewPassword, user.UserName);
            if (!passwordValidation.IsValid)
            {
                return BadRequest(new
                {
                    message = "رمز عبور جدید سیاست‌های امنیتی را رعایت نمی‌کند",
                    errors = passwordValidation.Errors
                });
            }

            // ============================================
            // بررسی تاریخچه رمز عبور
            // ============================================
            var policySettings = _passwordPolicyService.GetPolicySettings();
            var isInHistory = await _passwordHistoryService.IsPasswordInHistoryAsync(
                user.Id,
                request.NewPassword,
                policySettings.PasswordHistoryCount);

            if (isInHistory)
            {
                return BadRequest(new
                {
                    message = $"رمز عبور جدید نباید با {policySettings.PasswordHistoryCount} رمز عبور قبلی یکسان باشد"
                });
            }

            // ============================================
            // Hash کردن رمز عبور جدید با پاکسازی امن (FDP_RIP.2)
            // ============================================
            string newPasswordHash;
            try
            {
                newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            }
            finally
            {
                // پاکسازی رمز عبور از حافظه
                var newPasswordCopy = request.NewPassword;
                _secureMemoryService.ClearString(ref newPasswordCopy);
                var confirmPasswordCopy = request.ConfirmNewPassword;
                _secureMemoryService.ClearString(ref confirmPasswordCopy);
            }

            // ============================================
            // ذخیره رمز عبور جدید
            // ============================================
            user.Password = newPasswordHash;
            user.SetPasswordLastChangedAt(DateTime.UtcNow);
            user.MustChangePassword = false;
            await _context.SaveChangesAsync();

            // ============================================
            // ثبت در تاریخچه رمز عبور
            // ============================================
            await _passwordHistoryService.AddToHistoryAsync(user.Id, newPasswordHash, ipAddress);
            await _passwordHistoryService.CleanupOldHistoryAsync(user.Id, policySettings.PasswordHistoryCount);

            // ============================================
            // باطل کردن تمام توکن‌های فعال کاربر
            // ============================================
            await _tokenBlacklistService.BlacklistAllUserTokensAsync(user.UserName, "Password reset via forgot password", default);

            // ============================================
            // باطل کردن توکن ریست (یکبار مصرف)
            // ============================================
            _passwordResetService.InvalidatePasswordResetToken(user.Id);

            // ============================================
            // ریست شمارنده تلاش‌های ناموفق
            // ============================================
            await _accountLockoutService.ResetFailedAttemptsAsync(user.UserName, default);

            await _auditLogService.LogEventAsync(
                eventType: "PasswordReset_Success",
                entityType: "User",
                entityId: user.Id.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userName: user.UserName,
                userId: user.Id,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"رمز عبور کاربر {user.UserName} از طریق بازیابی با موفقیت تغییر یافت",
                ct: default);

            return Ok(new ResetPasswordResponseDto
            {
                Success = true,
                Message = "رمز عبور با موفقیت تغییر یافت. لطفاً با رمز عبور جدید وارد شوید."
            });
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// تولید توکن موقت MFA
        /// </summary>
        private string GenerateMfaToken(long userId, string userName)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection.GetValue<string>("Key") ?? throw new InvalidOperationException("JWT Key not configured");
            var issuer = jwtSection.GetValue<string>("Issuer");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(10); // توکن MFA فقط 10 دقیقه معتبر است

            var claims = new[]
            {
                new Claim("mfa_user_id", userId.ToString()),
                new Claim("mfa_user_name", userName),
                new Claim("mfa_purpose", "mfa_verification"),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var tokenDescriptor = new JwtSecurityToken(
                issuer: issuer,
                audience: "mfa",
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: credentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(tokenDescriptor);
        }

        /// <summary>
        /// اعتبارسنجی توکن MFA
        /// </summary>
        private (long UserId, string UserName)? ValidateMfaToken(string mfaToken)
        {
            try
            {
                var jwtSection = _configuration.GetSection("Jwt");
                var key = jwtSection.GetValue<string>("Key") ?? throw new InvalidOperationException("JWT Key not configured");
                var issuer = jwtSection.GetValue<string>("Issuer");

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = "mfa",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(mfaToken, validationParameters, out _);

                var userIdClaim = principal.FindFirst("mfa_user_id")?.Value;
                var userNameClaim = principal.FindFirst("mfa_user_name")?.Value;
                var purposeClaim = principal.FindFirst("mfa_purpose")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || 
                    string.IsNullOrEmpty(userNameClaim) || 
                    purposeClaim != "mfa_verification")
                {
                    return null;
                }

                return (long.Parse(userIdClaim), userNameClaim);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// تولید توکن JWT
        /// </summary>
        private (string TokenString, DateTime ExpiresAt) GenerateJwtToken(
            string userName,
            long userId,
            List<string> roles,
            long tblUserGrpId)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection.GetValue<string>("Key") ?? throw new InvalidOperationException("JWT Key not configured");
            var issuer = jwtSection.GetValue<string>("Issuer");
            var audience = jwtSection.GetValue<string>("Audience");
            var expiresMinutes = jwtSection.GetValue<int>("ExpiresMinutes", 60);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("TblUserGrpIdInsert", tblUserGrpId.ToString()),
                new Claim("TblUserGrpIdLastEdit", tblUserGrpId.ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var expires = now.AddMinutes(expiresMinutes);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: credentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenString = tokenHandler.WriteToken(tokenDescriptor);

            return (tokenString, expires);
        }

        /// <summary>
        /// ماسک کردن شماره موبایل
        /// </summary>
        private string? MaskMobileNumber(string? mobileNumber)
        {
            if (string.IsNullOrEmpty(mobileNumber) || mobileNumber.Length < 7)
                return mobileNumber;

            return mobileNumber.Substring(0, 4) + "***" + mobileNumber.Substring(mobileNumber.Length - 4);
        }

        /// <summary>
        /// تولید توکن موقت ریست رمز عبور (JWT)
        /// انقضا: 10 دقیقه
        /// </summary>
        private string GeneratePasswordResetToken(long userId, string userName)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection.GetValue<string>("Key") ?? throw new InvalidOperationException("JWT Key not configured");
            var issuer = jwtSection.GetValue<string>("Issuer");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(10); // توکن ریست فقط 10 دقیقه معتبر است

            var claims = new[]
            {
                new Claim("reset_user_id", userId.ToString()),
                new Claim("reset_user_name", userName),
                new Claim("reset_purpose", "password_reset"),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var tokenDescriptor = new JwtSecurityToken(
                issuer: issuer,
                audience: "password_reset",
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: credentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(tokenDescriptor);
        }

        /// <summary>
        /// اعتبارسنجی توکن ریست رمز عبور
        /// </summary>
        private (long UserId, string UserName)? ValidatePasswordResetToken(string resetToken)
        {
            try
            {
                var jwtSection = _configuration.GetSection("Jwt");
                var key = jwtSection.GetValue<string>("Key") ?? throw new InvalidOperationException("JWT Key not configured");
                var issuer = jwtSection.GetValue<string>("Issuer");

                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = "password_reset",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(resetToken, validationParameters, out _);

                var userIdClaim = principal.FindFirst("reset_user_id")?.Value;
                var userNameClaim = principal.FindFirst("reset_user_name")?.Value;
                var purposeClaim = principal.FindFirst("reset_purpose")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) ||
                    string.IsNullOrEmpty(userNameClaim) ||
                    purposeClaim != "password_reset")
                {
                    return null;
                }

                return (long.Parse(userIdClaim), userNameClaim);
            }
            catch
            {
                return null;
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
                UserName = User.Identity?.Name ?? "Anonymous",
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

        #endregion
    }
}
