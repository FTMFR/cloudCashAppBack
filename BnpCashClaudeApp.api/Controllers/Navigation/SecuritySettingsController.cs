using BnpCashClaudeApp.api.Attributes;
using BnpCashClaudeApp.api.Extensions;
using BnpCashClaudeApp.api.Helpers;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.MediatR.Commands;
using BnpCashClaudeApp.Application.MediatR.Queries;
using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BnpCashClaudeApp.api.Controllers.Navigation
{
    /// <summary>
    /// کنترلر مدیریت تنظیمات امنیتی
    /// ============================================
    /// فقط کاربران با Permission مناسب می‌توانند تنظیمات را ویرایش کنند
    /// ============================================
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SecuritySettingsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ISecuritySettingsService _securitySettingsService;
        private readonly IAuditLogService _auditLogService;
        private readonly IDataExportService _dataExportService;

        public SecuritySettingsController(
            IMediator mediator,
            ISecuritySettingsService securitySettingsService,
            IAuditLogService auditLogService,
            IDataExportService dataExportService

            )
        {
            _mediator = mediator;
            _securitySettingsService = securitySettingsService;
            _auditLogService = auditLogService;
            _dataExportService = dataExportService;
        }

        // ============================================
        // تنظیمات قفل حساب کاربری
        // ============================================

        /// <summary>
        /// دریافت تنظیمات قفل حساب کاربری
        /// </summary>
        [HttpGet("account-lockout")]
        [RequirePermission("Security.LockoutPolicy")]
        public async Task<ActionResult<AccountLockoutSettings>> GetAccountLockoutSettings()
        {
            try
            {
                var query = new GetAccountLockoutSettingsQuery();
                var result = await _mediator.Send(query);
                var protectedResult = await ProtectReadPayloadAsync(result, "AccountLockoutSettings");
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// به‌روزرسانی تنظیمات قفل حساب کاربری
        /// </summary>
        [HttpPut("account-lockout")]
        [RequirePermission("Security.Manage")]
        public async Task<IActionResult> UpdateAccountLockoutSettings([FromBody] UpdateAccountLockoutSettingsDto dto)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Problem(
                    detail: "شناسه کاربر در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new UpdateAccountLockoutSettingsCommand
            {
                MaxFailedAttempts = dto.MaxFailedAttempts,
                LockoutDurationMinutes = dto.LockoutDurationMinutes,
                EnablePermanentLockout = dto.EnablePermanentLockout,
                PermanentLockoutThreshold = dto.PermanentLockoutThreshold,
                FailedAttemptResetMinutes = dto.FailedAttemptResetMinutes,
                UserId = userId.Value
            };

            var result = await _mediator.Send(command);

            if (!result)
                return BadRequest("خطا در به‌روزرسانی تنظیمات");

            return Ok(new { success = true, message = "تنظیمات قفل حساب کاربری با موفقیت به‌روزرسانی شد" });
        }

        // ============================================
        // تنظیمات سیاست رمز عبور
        // ============================================

        /// <summary>
        /// دریافت تنظیمات سیاست رمز عبور
        /// </summary>
        [HttpGet("password-policy")]
        [RequirePermission("Security.PasswordPolicy")]
        public async Task<ActionResult<PasswordPolicySettings>> GetPasswordPolicySettings()
        {
            try
            {
                var query = new GetPasswordPolicySettingsQuery();
                var result = await _mediator.Send(query);
                var protectedResult = await ProtectReadPayloadAsync(result, "PasswordPolicySettings");
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// به‌روزرسانی تنظیمات سیاست رمز عبور
        /// </summary>
        [HttpPut("password-policy")]
        [RequirePermission("Security.Manage")]
        public async Task<IActionResult> UpdatePasswordPolicySettings([FromBody] UpdatePasswordPolicySettingsDto dto)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Problem(
                    detail: "شناسه کاربر در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new UpdatePasswordPolicySettingsCommand
            {
                MinimumLength = dto.MinimumLength,
                MaximumLength = dto.MaximumLength,
                RequireUppercase = dto.RequireUppercase,
                RequireLowercase = dto.RequireLowercase,
                RequireDigit = dto.RequireDigit,
                RequireSpecialCharacter = dto.RequireSpecialCharacter,
                SpecialCharacters = dto.SpecialCharacters,
                DisallowUsername = dto.DisallowUsername,
                PasswordHistoryCount = dto.PasswordHistoryCount,
                PasswordExpirationDays = dto.PasswordExpirationDays,
                UserId = userId.Value
            };

            var result = await _mediator.Send(command);

            if (!result)
                return BadRequest("خطا در به‌روزرسانی تنظیمات");

            return Ok(new { success = true, message = "تنظیمات سیاست رمز عبور با موفقیت به‌روزرسانی شد" });
        }

        // ============================================
        // تنظیمات CAPTCHA
        // ============================================

        /// <summary>
        /// دریافت تنظیمات CAPTCHA
        /// </summary>
        [HttpGet("captcha")]
        [RequirePermission("Security.Read")]
        public async Task<ActionResult<CaptchaSettings>> GetCaptchaSettings()
        {
            try
            {
                var settings = await _securitySettingsService.GetCaptchaSettingsAsync();
                var protectedSettings = await ProtectReadPayloadAsync(settings, "CaptchaSettings");
                return Ok(protectedSettings);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// به‌روزرسانی تنظیمات CAPTCHA
        /// </summary>
        [HttpPut("captcha")]
        [RequirePermission("Security.Manage")]
        public async Task<IActionResult> UpdateCaptchaSettings([FromBody] UpdateCaptchaSettingsDto dto)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Problem(
                    detail: "شناسه کاربر در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var settings = new CaptchaSettings
            {
                IsEnabled = dto.IsEnabled,
                CodeLength = dto.CodeLength,
                ExpiryMinutes = dto.ExpiryMinutes,
                NoiseLineCount = dto.NoiseLineCount,
                NoiseDotCount = dto.NoiseDotCount,
                ImageWidth = dto.ImageWidth,
                ImageHeight = dto.ImageHeight,
                RequireOnMfa = dto.RequireOnMfa
            };

            await _securitySettingsService.SaveCaptchaSettingsAsync(settings, userId.Value);

            return Ok(new { success = true, message = "تنظیمات CAPTCHA با موفقیت به‌روزرسانی شد" });
        }

        // ============================================
        // تنظیمات MFA
        // ============================================

        /// <summary>
        /// دریافت تنظیمات احراز هویت دو مرحله‌ای (MFA)
        /// </summary>
        [HttpGet("mfa")]
        [RequirePermission("Security.Read")]
        public async Task<ActionResult<MfaSettings>> GetMfaSettings()
        {
            try
            {
                var settings = await _securitySettingsService.GetMfaSettingsAsync();
                var protectedSettings = await ProtectReadPayloadAsync(settings, "MfaSettings");
                return Ok(protectedSettings);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// به‌روزرسانی تنظیمات MFA
        /// </summary>
        [HttpPut("mfa")]
        [RequirePermission("Security.Manage")]
        public async Task<IActionResult> UpdateMfaSettings([FromBody] UpdateMfaSettingsDto dto)
        {
            var userId = User.GetUserId();
            if (userId == null)
                return Problem(
                    detail: "شناسه کاربر در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var settings = new MfaSettings
            {
                IsEnabled = dto.IsEnabled,
                IsRequired = dto.IsRequired,
                OtpLength = dto.OtpLength,
                OtpExpirySeconds = dto.OtpExpirySeconds,
                RecoveryCodesCount = dto.RecoveryCodesCount,
                MaxFailedOtpAttempts = dto.MaxFailedOtpAttempts,
                LockoutDurationMinutes = dto.LockoutDurationMinutes
            };

            await _securitySettingsService.SaveMfaSettingsAsync(settings, userId.Value);

            return Ok(new { success = true, message = "تنظیمات احراز هویت دو مرحله‌ای با موفقیت به‌روزرسانی شد" });
        }

        /// <summary>
        /// پاکسازی کش تنظیمات امنیتی
        /// ============================================
        /// برای بارگذاری مجدد تنظیمات از دیتابیس
        /// ============================================
        /// </summary>
        [HttpPost("invalidate-cache")]
        [RequirePermission("Security.Manage")]
        public IActionResult InvalidateCache()
        {
            _securitySettingsService.InvalidateCache();
            return Ok(new { success = true, message = "کش تنظیمات امنیتی پاکسازی شد" });
        }


        #region Audit Log Protection Settings (FAU_STG.3.1, FAU_STG.4.1)

        /// <summary>
        /// دریافت تنظیمات حفاظت از داده‌های ممیزی
        /// الزامات FAU_STG.3.1 و FAU_STG.4.1
        /// </summary>
        [HttpGet("AuditLogProtection")]
        [RequirePermission("AuditLog.Read")]
        public async Task<ActionResult<AuditLogProtectionSettings>> GetAuditLogProtectionSettings(
            [FromServices] ISecuritySettingsService securitySettingsService)
        {
            try
            {
                var settings = await securitySettingsService.GetAuditLogProtectionSettingsAsync();
                var protectedSettings = await ProtectReadPayloadAsync(
                    settings,
                    "AuditLogProtectionSettings",
                    "AuditLogProtection");
                return Ok(protectedSettings);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// به‌روزرسانی تنظیمات حفاظت از داده‌های ممیزی
        /// الزامات FAU_STG.3.1 و FAU_STG.4.1
        /// </summary>
        [HttpPut("AuditLogProtection")]
        [RequirePermission("AuditLog.Admin")]
        public async Task<IActionResult> UpdateAuditLogProtectionSettings(
            [FromBody] UpdateAuditLogProtectionSettingsDto dto,
            [FromServices] ISecuritySettingsService securitySettingsService)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out var userId))
            {
                return Problem(
                    detail: "شناسه کاربر در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");
            }

            var settings = new AuditLogProtectionSettings
            {
                IsEnabled = dto.IsEnabled,
                MaxRetryAttempts = dto.MaxRetryAttempts,
                EnableAlertOnFailure = dto.EnableAlertOnFailure,
                AlertEmailAddresses = dto.AlertEmailAddresses ?? string.Empty,
                AlertSmsNumbers = dto.AlertSmsNumbers ?? string.Empty,
                RetentionDays = dto.RetentionDays,
                ArchiveAfterDays = dto.ArchiveAfterDays,
                BackupIntervalHours = dto.BackupIntervalHours,
                RetentionCheckIntervalHours = dto.RetentionCheckIntervalHours,
                FallbackRecoveryIntervalMinutes = dto.FallbackRecoveryIntervalMinutes,
                HealthCheckIntervalMinutes = dto.HealthCheckIntervalMinutes,
                FallbackDirectory = dto.FallbackDirectory ?? string.Empty,
                BackupDirectory = dto.BackupDirectory ?? string.Empty,
                ArchiveDirectory = dto.ArchiveDirectory ?? string.Empty
            };

            await securitySettingsService.SaveAuditLogProtectionSettingsAsync(settings, userId);

            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var userName = User.Identity?.Name ?? "Unknown";

            await _auditLogService.LogEventAsync(
                eventType: "AuditLogProtectionSettingsUpdated",
                entityType: "SecuritySetting",
                entityId: "AuditLogProtection",
                isSuccess: true,
                ipAddress: ipAddress,
                userName: userName,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"تنظیمات حفاظت از داده‌های ممیزی توسط {userName} به‌روزرسانی شد",
                ct: default);

            return Ok(new
            {
                success = true,
                message = "تنظیمات حفاظت از داده‌های ممیزی با موفقیت به‌روزرسانی شد",
                updatedBy = userName,
                updatedAt = BaseEntity.ToPersianDateTime(DateTime.UtcNow)
            });
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
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        #endregion
    }
    // ============================================
    // DTOs برای Audit Log Protection
    // ============================================

    /// <summary>
    /// DTO برای به‌روزرسانی تنظیمات حفاظت از داده‌های ممیزی
    /// الزامات FAU_STG.3.1 و FAU_STG.4.1
    /// </summary>
    public class UpdateAuditLogProtectionSettingsDto
    {
        /// <summary>
        /// آیا حفاظت از داده‌های ممیزی فعال است؟
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// حداکثر تعداد تلاش مجدد برای ذخیره در دیتابیس
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// آیا ارسال هشدار در صورت شکست فعال است؟
        /// </summary>
        public bool EnableAlertOnFailure { get; set; } = true;

        /// <summary>
        /// ایمیل‌های مقصد برای ارسال هشدار (جدا شده با کاما)
        /// </summary>
        public string? AlertEmailAddresses { get; set; }

        /// <summary>
        /// شماره‌های موبایل برای ارسال پیامک هشدار (جدا شده با کاما)
        /// مثال: "09123456789,09187654321"
        /// </summary>
        public string? AlertSmsNumbers { get; set; }

        /// <summary>
        /// تعداد روزهای نگهداری داده‌ها (Retention)
        /// </summary>
        public int RetentionDays { get; set; } = 365;

        /// <summary>
        /// تعداد روزهایی که پس از آن لاگ‌ها آرشیو می‌شوند
        /// </summary>
        public int ArchiveAfterDays { get; set; } = 90;

        /// <summary>
        /// فاصله زمانی پشتیبان‌گیری خودکار (ساعت)
        /// </summary>
        public int BackupIntervalHours { get; set; } = 24;

        /// <summary>
        /// فاصله زمانی بررسی سیاست نگهداری (ساعت)
        /// </summary>
        public int RetentionCheckIntervalHours { get; set; } = 24;

        /// <summary>
        /// فاصله زمانی بازیابی لاگ‌های Fallback (دقیقه)
        /// </summary>
        public int FallbackRecoveryIntervalMinutes { get; set; } = 5;

        /// <summary>
        /// فاصله زمانی بررسی سلامت سیستم (دقیقه)
        /// </summary>
        public int HealthCheckIntervalMinutes { get; set; } = 10;

        /// <summary>
        /// مسیر دایرکتوری Fallback (خالی = پیش‌فرض)
        /// </summary>
        public string? FallbackDirectory { get; set; }

        /// <summary>
        /// مسیر دایرکتوری پشتیبان‌ها (خالی = پیش‌فرض)
        /// </summary>
        public string? BackupDirectory { get; set; }

        /// <summary>
        /// مسیر دایرکتوری آرشیو (خالی = پیش‌فرض)
        /// </summary>
        public string? ArchiveDirectory { get; set; }
    }


    // ============================================
    // DTOs برای API
    // ============================================

    /// <summary>
    /// DTO برای به‌روزرسانی تنظیمات قفل حساب کاربری
    /// </summary>
    public class UpdateAccountLockoutSettingsDto
    {
        /// <summary>
        /// حداکثر تعداد تلاش‌های ناموفق قبل از قفل شدن
        /// </summary>
        public int MaxFailedAttempts { get; set; } = 5;

        /// <summary>
        /// مدت زمان قفل حساب به دقیقه
        /// </summary>
        public int LockoutDurationMinutes { get; set; } = 15;

        /// <summary>
        /// آیا قفل دائمی فعال است
        /// </summary>
        public bool EnablePermanentLockout { get; set; } = false;

        /// <summary>
        /// تعداد تلاش‌های ناموفق برای قفل دائمی
        /// </summary>
        public int PermanentLockoutThreshold { get; set; } = 10;

        /// <summary>
        /// مدت زمان ریست شدن شمارنده تلاش‌های ناموفق (به دقیقه)
        /// </summary>
        public int FailedAttemptResetMinutes { get; set; } = 30;
    }

    /// <summary>
    /// DTO برای به‌روزرسانی تنظیمات سیاست رمز عبور
    /// </summary>
    public class UpdatePasswordPolicySettingsDto
    {
        /// <summary>
        /// حداقل طول رمز عبور
        /// </summary>
        public int MinimumLength { get; set; } = 8;

        /// <summary>
        /// حداکثر طول رمز عبور
        /// </summary>
        public int MaximumLength { get; set; } = 128;

        /// <summary>
        /// آیا حداقل یک حرف بزرگ الزامی است
        /// </summary>
        public bool RequireUppercase { get; set; } = true;

        /// <summary>
        /// آیا حداقل یک حرف کوچک الزامی است
        /// </summary>
        public bool RequireLowercase { get; set; } = true;

        /// <summary>
        /// آیا حداقل یک عدد الزامی است
        /// </summary>
        public bool RequireDigit { get; set; } = true;

        /// <summary>
        /// آیا حداقل یک کاراکتر خاص الزامی است
        /// </summary>
        public bool RequireSpecialCharacter { get; set; } = true;

        /// <summary>
        /// لیست کاراکترهای خاص مجاز
        /// </summary>
        public string SpecialCharacters { get; set; } = "!@#$%^&*()_+-=[]{}|;':\",./<>?";

        /// <summary>
        /// آیا رمز عبور نباید شامل نام کاربری باشد
        /// </summary>
        public bool DisallowUsername { get; set; } = true;

        /// <summary>
        /// تعداد رمزهای قبلی که نباید تکرار شوند
        /// </summary>
        public int PasswordHistoryCount { get; set; } = 5;

        /// <summary>
        /// مدت اعتبار رمز عبور به روز (0 = بدون انقضا)
        /// </summary>
        public int PasswordExpirationDays { get; set; } = 90;
    }

    /// <summary>
    /// DTO برای به‌روزرسانی تنظیمات CAPTCHA
    /// </summary>
    public class UpdateCaptchaSettingsDto
    {
        /// <summary>
        /// آیا CAPTCHA فعال است؟
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// تعداد کاراکترهای کد CAPTCHA
        /// </summary>
        public int CodeLength { get; set; } = 5;

        /// <summary>
        /// زمان انقضای CAPTCHA به دقیقه
        /// </summary>
        public int ExpiryMinutes { get; set; } = 2;

        /// <summary>
        /// تعداد خطوط نویز
        /// </summary>
        public int NoiseLineCount { get; set; } = 10;

        /// <summary>
        /// تعداد نقاط نویز
        /// </summary>
        public int NoiseDotCount { get; set; } = 50;

        /// <summary>
        /// عرض تصویر (پیکسل)
        /// </summary>
        public int ImageWidth { get; set; } = 130;

        /// <summary>
        /// ارتفاع تصویر (پیکسل)
        /// </summary>
        public int ImageHeight { get; set; } = 40;

        /// <summary>
        /// آیا در MFA نیاز به CAPTCHA دارد؟
        /// </summary>
        public bool RequireOnMfa { get; set; } = true;
    }

    /// <summary>
    /// DTO برای به‌روزرسانی تنظیمات MFA
    /// </summary>
    public class UpdateMfaSettingsDto
    {
        /// <summary>
        /// آیا MFA برای کل سیستم فعال است؟
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// آیا MFA برای همه کاربران اجباری است؟
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// طول کد OTP (پیامک)
        /// </summary>
        public int OtpLength { get; set; } = 6;

        /// <summary>
        /// زمان انقضای کد OTP به ثانیه
        /// </summary>
        public int OtpExpirySeconds { get; set; } = 120;

        /// <summary>
        /// تعداد کدهای بازیابی
        /// </summary>
        public int RecoveryCodesCount { get; set; } = 10;

        /// <summary>
        /// حداکثر تعداد تلاش ناموفق برای OTP
        /// </summary>
        public int MaxFailedOtpAttempts { get; set; } = 3;

        /// <summary>
        /// زمان قفل شدن پس از تلاش‌های ناموفق (دقیقه)
        /// </summary>
        public int LockoutDurationMinutes { get; set; } = 5;
    }
}
