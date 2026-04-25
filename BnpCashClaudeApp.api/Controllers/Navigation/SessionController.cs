using BnpCashClaudeApp.api.Attributes;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.api.Extensions;
using BnpCashClaudeApp.api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;

namespace BnpCashClaudeApp.api.Controllers.Navigation
{
    /// <summary>
    /// کنترلر مدیریت نشست‌های کاربر
    /// ============================================
    /// FTA_MCS.1 - محدودیت نشست همزمان
    /// ============================================
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SessionController : Controller
    {
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IAuditLogService _auditLogService;
        private readonly ISecuritySettingsService _securitySettingsService;
        private readonly IDataExportService _dataExportService;

        public SessionController(
            IRefreshTokenService refreshTokenService,
            IAuditLogService auditLogService,
            ISecuritySettingsService securitySettingsService,
            IDataExportService dataExportService)
        {
            _refreshTokenService = refreshTokenService;
            _auditLogService = auditLogService;
            _securitySettingsService = securitySettingsService;
            _dataExportService = dataExportService;
        }

        /// <summary>
        /// دریافت لیست نشست‌های فعال کاربر جاری
        /// در صورت رسیدن به حداکثر نشست، پیام هشدار همراه با لیست نشست‌ها برگردانده می‌شود
        /// GET: api/Session/MySessions
        /// </summary>
        [HttpGet("MySessions")]
        [RequirePermission("Sessions.Read")]
        public async Task<IActionResult> GetMySessions(CancellationToken ct)
        {
            try
            {
                var userId = User.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "کاربر احراز هویت نشده است" });
            }

            string? currentToken = null;
            if (Request.Cookies.TryGetValue("refreshToken", out var refreshTokenValue))
            {
                currentToken = refreshTokenValue;
            }

            var sessions = await _refreshTokenService.GetUserActiveSessionsAsync(
                userId.Value,
                currentToken,
                ct);

            // بررسی محدودیت نشست همزمان
            var settings = await _securitySettingsService.GetContextAccessControlSettingsAsync(ct);
            bool isMaxSessionsReached = false;
            string? warningMessage = null;

            if (settings.EnableConcurrentSessionLimit && sessions.Count >= settings.MaxConcurrentSessions)
            {
                isMaxSessionsReached = true;
                warningMessage = $"تعداد نشست‌های همزمان به حداکثر ({settings.MaxConcurrentSessions}) رسیده است";
            }

            await _auditLogService.LogEventAsync(
                eventType: "ViewSessions",
                entityType: "Session",
                entityId: userId.Value.ToString(),
                isSuccess: true,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userId: userId.Value,
                description: $"مشاهده {sessions.Count} نشست فعال" + 
                    (isMaxSessionsReached ? " - محدودیت نشست همزمان فعال است" : ""),
                ct: ct);

            // ساخت پاسخ با اطلاعات کامل
            var response = new
            {
                success = true,
                sessions,
                totalCount = sessions.Count,
                isMaxSessionsReached = isMaxSessionsReached,
                maxConcurrentSessions = settings.EnableConcurrentSessionLimit ? settings.MaxConcurrentSessions : (int?)null,
                warning = warningMessage
            };

                var protectedResponse = await ProtectReadPayloadAsync(
                    response,
                    "Session",
                    userId.Value.ToString(),
                    ct);

                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// باطل کردن یک نشست خاص (خروج از یک دستگاه)
        /// DELETE: api/Session/RevokeSession/{sessionPublicId}
        /// </summary>
        [HttpDelete("RevokeSession/{sessionPublicId:guid}")]
        [RequirePermission("Sessions.Revoke")]
        public async Task<IActionResult> RevokeSession(Guid sessionPublicId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "کاربر احراز هویت نشده است" });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _refreshTokenService.RevokeSessionAsync(
                userId.Value,
                sessionPublicId,
                "کاربر نشست را باطل کرد",
                ct);

            await _auditLogService.LogEventAsync(
                eventType: "RevokeSession",
                entityType: "Session",
                entityId: sessionPublicId.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userId: userId.Value,
                description: $"کاربر نشست {sessionPublicId} را باطل کرد",
                ct: ct);

            return Ok(new
            {
                success = true,
                message = "نشست با موفقیت باطل شد"
            });
        }

        /// <summary>
        /// باطل کردن تمام نشست‌های کاربر به جز نشست فعلی
        /// DELETE: api/Session/RevokeAllOtherSessions
        /// </summary>
        [HttpDelete("RevokeAllOtherSessions")]
        [RequirePermission("Sessions.Revoke")]
        public async Task<IActionResult> RevokeAllOtherSessions(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "کاربر احراز هویت نشده است" });
            }

            string? currentToken = null;
            if (!Request.Cookies.TryGetValue("refreshToken", out var refreshTokenValue))
            {
                return BadRequest(new { message = "توکن فعلی یافت نشد" });
            }
            currentToken = refreshTokenValue;

            // دریافت همه نشست‌های فعال
            var sessions = await _refreshTokenService.GetUserActiveSessionsAsync(
                userId.Value,
                currentToken,
                ct);

            // باطل کردن نشست‌های غیر فعلی
            var otherSessions = sessions.Where(s => !s.IsCurrentSession).ToList();

            foreach (var session in otherSessions)
            {
                await _refreshTokenService.RevokeSessionAsync(
                    userId.Value,
                    session.PublicId,
                    "کاربر همه نشست‌های دیگر را باطل کرد",
                    ct);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _auditLogService.LogEventAsync(
                eventType: "RevokeAllOtherSessions",
                entityType: "Session",
                entityId: userId.Value.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userId: userId.Value,
                description: $"کاربر {otherSessions.Count} نشست دیگر را باطل کرد",
                ct: ct);

            return Ok(new
            {
                success = true,
                message = $"{otherSessions.Count} نشست با موفقیت باطل شد",
                revokedCount = otherSessions.Count
            });
        }

        /// <summary>
        /// دریافت تعداد نشست‌های فعال کاربر (برای نمایش Badge)
        /// GET: api/Session/ActiveCount
        /// </summary>
        //[HttpGet("ActiveCount")]
        //[RequirePermission("Sessions.Read")]
        //public async Task<IActionResult> GetActiveSessionsCount(CancellationToken ct)
        //{
        //    try
        //    {
        //        var userId = User.GetUserId();
        //    if (userId == null)
        //    {
        //        return Unauthorized(new { message = "کاربر احراز هویت نشده است" });
        //    }

        //    var sessions = await _refreshTokenService.GetUserActiveSessionsAsync(
        //        userId.Value,
        //        null,
        //        ct);

        //        var response = new
        //        {
        //            success = true,
        //            activeCount = sessions.Count
        //        };

        //        var protectedResponse = await ProtectReadPayloadAsync(
        //            response,
        //            "SessionActiveCount",
        //            userId.Value.ToString(),
        //            ct);

        //        return Ok(protectedResponse);
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        return StatusCode(403, new { success = false, error = ex.Message });
        //    }
        //}

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
    }
}
