using BnpCashClaudeApp.api.Attributes;
using BnpCashClaudeApp.api.Controllers.Base;
using BnpCashClaudeApp.api.Extensions;
using BnpCashClaudeApp.api.Helpers;
using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Application.DTOs.Common;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.MediatR.Commands;
using BnpCashClaudeApp.Application.MediatR.Queries;
using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.AuditLogSubsystem;
using BnpCashClaudeApp.Domain.Enums;
using BnpCashClaudeApp.Persistence.Migrations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BnpCashClaudeApp.api.Controllers.Navigation
{
    /// <summary>
    /// کنترلر مدیریت کاربران
    /// ============================================
    /// پیاده‌سازی الزامات امنیتی پروفایل حفاظتی برنامه‌های کاربردی تحت شبکه (ISO 15408)
    /// پیاده‌سازی الزام FDP_ACF و FAU_GEN - کنترل دسترسی دقیق
    /// ============================================
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("ApiPolicy")]
    public class UsersController : AuditControllerBase
    {
        private readonly IMediator _mediator;
        private readonly NavigationDbContext _context;
        private readonly IAccountLockoutService _accountLockoutService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IPasswordPolicyService _passwordPolicyService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly IDataExportService _dataExportService;
        private readonly IAttachmentService _attachmentService;
        private readonly IDbContextFactory<LogDbContext> _logDbContextFactory;

        public UsersController(
            IMediator mediator,
            NavigationDbContext context,
            IAuditLogService auditLogService,
            IAccountLockoutService accountLockoutService,
            IPasswordHasher passwordHasher,
            IPasswordPolicyService passwordPolicyService,
            ITokenBlacklistService tokenBlacklistService,
            IDataExportService dataExportService,
            IAttachmentService attachmentService,
            IDbContextFactory<LogDbContext> logDbContextFactory)
            : base(auditLogService)
        {
            _mediator = mediator;
            _context = context;
            _accountLockoutService = accountLockoutService;
            _passwordHasher = passwordHasher;
            _passwordPolicyService = passwordPolicyService;
            _tokenBlacklistService = tokenBlacklistService;
            _dataExportService = dataExportService;
            _attachmentService = attachmentService;
            _logDbContextFactory = logDbContextFactory;
        }

        /// <summary>
        /// دریافت لیست کاربران
        /// </summary>
        [HttpGet]
        [RequirePermission("Users.Read")]
        public async Task<ActionResult<List<UserDto>>> GetAll()
        {
            try
            {
                var query = new GetAllUsersQuery();
                var result = await _mediator.Send(query);
                //var protectedResult = await ProtectReadPayloadAsync(result, "User");
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت اطلاعات یک کاربر
        /// </summary>
        [HttpGet("{publicId}")]
        [RequirePermission("Users.ReadById")]
        public async Task<ActionResult<UserDto>> GetById(Guid publicId)
        {
            try
            {
                var query = new GetUserByIdQuery { PublicId = publicId };
                var result = await _mediator.Send(query);

                if (result == null)
                    return NotFound();

                //var protectedResult = await ProtectReadPayloadAsync(result, "User", publicId.ToString());
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// سوابق ورود و خروج یک کاربر
        /// فقط رویدادهای Authentication (ورود)، Logout و LogoutAll از AuditLog برگردانده می‌شوند.
        /// دسترسی: Users.ReadById
        /// </summary>
        /// <param name="publicId">شناسه عمومی کاربر</param>
        /// <param name="pageNumber">شماره صفحه (پیش‌فرض: 1)</param>
        /// <param name="pageSize">تعداد در هر صفحه (پیش‌فرض: 20، حداکثر: 100)</param>
        [HttpGet("{publicId}/login-logout-history")]
        [RequirePermission("Users.ReadById")]
        [ProducesResponseType(typeof(LoginLogoutHistoryPagedResult), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<LoginLogoutHistoryPagedResult>> GetLoginLogoutHistory(
            Guid publicId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var user = await _context.tblUsers.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.PublicId == publicId);
                if (user == null)
                    return Ok(new ResultDto(false ,  "کاربر یافت نشد" ));

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                await using var logContext = await _logDbContextFactory.CreateDbContextAsync();

                var query = logContext.AuditLogMasters.AsNoTracking()
                    .Where(x => x.UserId == user.Id || (x.UserName != null && x.UserName == user.UserName))
                    .Where(x =>
                        x.EventType == "Logout" ||
                        x.EventType == "LogoutAll" ||
                        (x.EventType == "Authentication" && x.Description != null && x.Description.Contains("ورود")));

                var totalCount = await query.CountAsync();

                var rawItems = await query
                    .OrderByDescending(x => x.EventDateTime)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var items = rawItems.Select(x => new LoginLogoutRecordDto
                {
                    Id = x.Id,
                    EventDateTime = BaseEntity.ToPersianDateTime(x.EventDateTime),
                    EventType = x.EventType,
                    IsSuccess = x.IsSuccess,
                    ErrorMessage = x.ErrorMessage,
                    IpAddress = x.IpAddress,
                    UserName = x.UserName,
                    OperatingSystem = x.OperatingSystem,
                    UserAgent = x.UserAgent,
                    Description = x.Description
                }).ToList();

                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var response = new LoginLogoutHistoryPagedResult
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = pageNumber > 1,
                    HasNextPage = pageNumber < totalPages
                };

                var protectedResponse = await ProtectReadPayloadAsync(response, "AuditLog", publicId.ToString());
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// ایجاد کاربر جدید
        /// </summary>
        [HttpPost]
        [RequirePermission("Users.Create")]
        public async Task<ActionResult<Guid>> Create([FromBody] CreateUserDto dto)
        {
            var tblUserGrpIdInsert = User.GetTblUserGrpIdInsert();
            if (tblUserGrpIdInsert == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");
            var command = new CreateUserCommand
            {
                UserName = dto.UserName,
                Password = dto.Password,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                MobileNumber = dto.MobileNumber,
                // UserCode به صورت خودکار در Handler تولید می‌شود
                TblUserGrpIdInsert = tblUserGrpIdInsert.Value,
                IpAddress = dto.IpAddress,
                GrpPublicId = dto.GrpPublicId,
                // Multi-tenancy: مشتری و شعبه
                tblCustomerId = dto.tblCustomerId,
                tblShobeId = dto.tblShobeId
            };

            var result = await _mediator.Send(command);
            await LogAuditEventAsync("Create", "User", result.ToString(), true);
            return CreatedAtAction(nameof(GetById), new { publicId = result }, result);
        }

        /// <summary>
        /// ویرایش کاربر
        /// </summary>
        [HttpPut("{publicId}")]
        [RequirePermission("Users.Update")]
        public async Task<IActionResult> Update(Guid publicId, [FromBody] UpdateUserDto dto)
        {
            var tblUserGrpIdLastEdit = User.GetTblUserGrpIdLastEdit();
            if (tblUserGrpIdLastEdit == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");
            var command = new UpdateUserCommand
            {
                PublicId = publicId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                MobileNumber = dto.MobileNumber,
                TblUserGrpIdLastEdit = tblUserGrpIdLastEdit.Value,
                tblCustomerId = dto.tblCustomerId,
                tblShobeId = dto.tblShobeId,
                GrpPublicId = dto.GrpPublicId,
                AuditUserId = User.GetUserId()
            };

            var result = await _mediator.Send(command);

            if (!result)
            {
                await LogAuditEventAsync("Update", "User", publicId.ToString(), false, "خطا در انجام عملیات");
                return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
            }

            return Ok(new ResultDto(true, "عملیات با موفقیت انجام شد"));
        }

        /// <summary>
        /// حذف کاربر
        /// </summary>
        [HttpDelete("{publicId}")]
        [RequirePermission("Users.Delete")]
        public async Task<IActionResult> Delete(Guid publicId)
        {
            var command = new DeleteUserCommand { PublicId = publicId };
            var result = await _mediator.Send(command);

            if (!result)
            {
                await LogAuditEventAsync("Delete", "User", publicId.ToString(), false, "خطا در انجام عملیات");
                return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
            }

            await LogAuditEventAsync("Delete", "User", publicId.ToString(), true);
            return Ok(new ResultDto(true, "عملیات با موفقیت انجام شد"));
        }

        #region Security Endpoints

        /// <summary>
        /// تغییر وضعیت فعال/غیرفعال حساب کاربری
        /// الزام: امکان فعال‌سازی و غیرفعال‌سازی حساب کاربری
        /// </summary>
        [HttpPost("{publicId}/set-status")]
        [RequirePermission("Users.Activate")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SetUserStatus(Guid publicId, [FromBody] SetUserStatusDto dto)
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var adminUserName = User.Identity?.Name ?? "Unknown";

            var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.PublicId == publicId);
            if (user == null)
            {
                return NotFound(new { message = "کاربر یافت نشد" });
            }

            user.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();

            // در صورت غیرفعال‌سازی، تمام توکن‌های کاربر باطل شوند
            if (!dto.IsActive)
            {
                await _tokenBlacklistService.BlacklistAllUserTokensAsync(
                    user.UserName,
                    "Account deactivated",
                    default);
            }

            var eventType = dto.IsActive ? "UserActivated" : "UserDeactivated";
            var statusText = dto.IsActive ? "فعال" : "غیرفعال";

            await AuditLogService.LogEventAsync(
                eventType: eventType,
                entityType: "User",
                entityId: publicId.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userName: adminUserName,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"حساب کاربری {user.UserName} توسط مدیر {adminUserName} {statusText} شد",
                ct: default);

            return Ok(new { message = $"حساب کاربری {user.UserName} با موفقیت {statusText} شد" });
        }

        /// <summary>
        /// باز کردن قفل حساب کاربری
        /// الزام: امکان باز کردن قفل حساب توسط مدیر
        /// </summary>
        [HttpPost("{publicId}/unlock")]
        [RequirePermission("Users.Unlock")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UnlockUser(Guid publicId)
        {
            var adminUserName = User.Identity?.Name ?? "Unknown";

            var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.PublicId == publicId);
            if (user == null)
            {
                return NotFound(new { message = "کاربر یافت نشد" });
            }

            await _accountLockoutService.UnlockAccountAsync(user.UserName, adminUserName, default);

            return Ok(new { message = $"قفل حساب کاربری {user.UserName} با موفقیت باز شد" });
        }

        /// <summary>
        /// دریافت وضعیت قفل حساب کاربری
        /// </summary>
        [HttpGet("{publicId}/lockout-status")]
        [RequirePermission("Users.Read")]
        [ProducesResponseType(typeof(LockoutStatus), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetLockoutStatus(Guid publicId)
        {
            try
            {
                var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.PublicId == publicId);
                if (user == null)
                {
                    return NotFound(new { message = "کاربر یافت نشد" });
                }

                var status = await _accountLockoutService.GetLockoutStatusAsync(user.UserName);
                var protectedStatus = await ProtectReadPayloadAsync(status, "UserLockoutStatus", publicId.ToString());

                return Ok(protectedStatus);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// ریست رمز عبور توسط مدیر
        /// الزام: امکان تغییر رمز عبور کاربر توسط مدیر
        /// </summary>
        [HttpPost("{publicId}/reset-password")]
        [RequirePermission("Users.ResetPassword")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResetPassword(Guid publicId, [FromBody] ResetPasswordDto dto)
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var adminUserName = User.Identity?.Name ?? "Unknown";

            var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.PublicId == publicId);
            if (user == null)
            {
                return NotFound(new { message = "کاربر یافت نشد" });
            }

            //var passwordValidation = _passwordPolicyService.ValidatePassword(dto.NewPassword, user.UserName);
            //if (!passwordValidation.IsValid)
            //{
            //    return BadRequest(new
            //    {
            //        message = "رمز عبور جدید سیاست‌های امنیتی را رعایت نمی‌کند",
            //        errors = passwordValidation.Errors
            //    });
            //}

            user.Password = _passwordHasher.HashPassword(dto.NewPassword);
            user.SetPasswordLastChangedAt(DateTime.UtcNow); // تاریخ به شمسی تبدیل می‌شود
            user.MustChangePassword = dto.MustChangePassword;
            await _context.SaveChangesAsync();

            await _tokenBlacklistService.BlacklistAllUserTokensAsync(
                user.UserName,
                "Password reset by admin",
                default);

            await AuditLogService.LogEventAsync(
                eventType: "PasswordReset",
                entityType: "User",
                entityId: publicId.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userName: adminUserName,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"رمز عبور کاربر {user.UserName} توسط مدیر {adminUserName} ریست شد",
                ct: default);

            return Ok(new { message = $"رمز عبور کاربری {user.UserName} با موفقیت تغییر یافت" });
        }

        /// <summary>
        /// اجبار کاربر به تغییر رمز عبور در ورود بعدی
        /// </summary>
        [HttpPost("{publicId}/force-password-change")]
        [RequirePermission("Users.Update")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ForcePasswordChange(Guid publicId)
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var adminUserName = User.Identity?.Name ?? "Unknown";

            var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.PublicId == publicId);
            if (user == null)
            {
                return NotFound(new { message = "کاربر یافت نشد" });
            }

            user.MustChangePassword = true;
            await _context.SaveChangesAsync();

            await AuditLogService.LogEventAsync(
                eventType: "ForcePasswordChange",
                entityType: "User",
                entityId: publicId.ToString(),
                isSuccess: true,
                ipAddress: ipAddress,
                userName: adminUserName,
                operatingSystem: operatingSystem,
                userAgent: userAgent,
                description: $"کاربر {user.UserName} مجبور به تغییر رمز عبور در ورود بعدی شد",
                ct: default);

            return Ok(new { message = $"کاربر {user.UserName} در ورود بعدی باید رمز عبور خود را تغییر دهد" });
        }

        #endregion

        #region Data Export (FDP_ETC.2)

        /// <summary>
        /// خروجی لیست کاربران با ویژگی‌های امنیتی
        /// ============================================
        /// پیاده‌سازی الزامات FDP_ETC.2.1, FDP_ETC.2.2, FDP_ETC.2.4
        /// FDP_ETC.2.1: خروجی داده با ویژگی‌های امنیتی مرتبط
        /// FDP_ETC.2.2: ارتباط بدون ابهام ویژگی‌های امنیتی
        /// FDP_ETC.2.4: قوانین کنترل خروجی اضافی (ماسک داده‌ها)
        /// ============================================
        /// </summary>
        /// <param name="filter">فیلترهای جستجو (اختیاری)</param>
        /// <returns>لیست کاربران با ویژگی‌های امنیتی</returns>
        [HttpGet("export")]
        [RequirePermission("Users.Export")]
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> ExportUsers([FromQuery] ExportUsersFilter? filter = null)
        {
            try
            {
                // دریافت اطلاعات کاربر فعلی
                var userId = GetUserId();
                var userName = User.Identity?.Name ?? "Unknown";
                var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
                var userAgent = HttpContextHelper.GetUserAgent(HttpContext);

                // دریافت لیست کاربران
                var query = new GetAllUsersQuery();
                var users = await _mediator.Send(query);

                // ایجاد Context برای خروجی
                var exportContext = new ExportContext
                {
                    EntityType = "User",
                    EntityId = null, // چند رکورد
                    UserId = userId,
                    UserName = userName,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    RequestPath = HttpContext.Request.Path,
                    RequestedFormat = "JSON"
                };

                // پوشش داده‌ها با ویژگی‌های امنیتی (FDP_ETC.2.1, FDP_ETC.2.2)
                // ماسک داده‌های حساس (FDP_ETC.2.4)
                var secureResponse = await _dataExportService.WrapWithSecurityAttributesAsync(
                    users.ToList(),
                    exportContext);

                return Ok(secureResponse);
            }
            catch (InvalidOperationException ex)
            {
                // خروجی مجاز نیست (FDP_ETC.2.4)
                return StatusCode(403, new
                {
                    success = false,
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                await AuditLogService.LogEventAsync(
                    eventType: "UsersExportFailed",
                    entityType: "User",
                    entityId: "Export",
                    isSuccess: false,
                    ipAddress: HttpContextHelper.GetIpAddress(HttpContext),
                    userName: User.Identity?.Name ?? "Unknown",
                    userId: GetUserId(),
                    userAgent: HttpContextHelper.GetUserAgent(HttpContext),
                    description: $"خطا در خروجی کاربران: {ex.Message}",
                    ct: default);

                return StatusCode(500, new
                {
                    success = false,
                    error = "خطا در تهیه خروجی کاربران"
                });
            }
        }

        /// <summary>
        /// خروجی یک کاربر خاص با ویژگی‌های امنیتی
        /// </summary>
        //[HttpGet("{publicId}/export")]
        //[RequirePermission("Users.Export")]
        //[ProducesResponseType(200)]
        //[ProducesResponseType(404)]
        //public async Task<IActionResult> ExportUser(Guid publicId)
        //{
        //    try
        //    {
        //        var userId = GetUserId();
        //        var userName = User.Identity?.Name ?? "Unknown";
        //        var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
        //        var userAgent = HttpContextHelper.GetUserAgent(HttpContext);

        //        // دریافت کاربر
        //        var query = new GetUserByIdQuery { PublicId = publicId };
        //        var user = await _mediator.Send(query);

        //        if (user == null)
        //        {
        //            return NotFound(new { success = false, error = "کاربر یافت نشد" });
        //        }

        //        var exportContext = new ExportContext
        //        {
        //            EntityType = "User",
        //            EntityId = publicId.ToString(),
        //            UserId = userId,
        //            UserName = userName,
        //            IpAddress = ipAddress,
        //            UserAgent = userAgent,
        //            RequestPath = HttpContext.Request.Path,
        //            RequestedFormat = "JSON"
        //        };

        //        var secureResponse = await _dataExportService.WrapWithSecurityAttributesAsync(
        //            user,
        //            exportContext);

        //        return Ok(secureResponse);
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        return StatusCode(403, new
        //        {
        //            success = false,
        //            error = ex.Message
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            success = false,
        //            error = "خطا در تهیه خروجی کاربر"
        //        });
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
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        #endregion

        #region Profile Picture (Attachment Subsystem)

        /// <summary>
        /// آپلود تصویر پروفایل کاربر
        /// ============================================
        /// پیاده‌سازی الزامات FDP_ACC.2, FDP_ACF.1 (کنترل دسترسی)
        /// پیاده‌سازی الزام FAU_GEN.1 (ثبت رویداد)
        /// ============================================
        /// </summary>
        /// <param name="publicId">شناسه عمومی کاربر</param>
        /// <param name="file">فایل تصویر</param>
        /// <returns>نتیجه آپلود</returns>
        [HttpPost("{publicId}/profile-picture")]
        [RequirePermission("Users.UploadProfilePicture")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UploadProfilePicture(Guid publicId, IFormFile file)
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var currentUserName = User.Identity?.Name ?? "Unknown";

            // 1. پیدا کردن کاربر
            var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.PublicId == publicId);
            if (user == null)
            {
                return Ok(new ResultDto { IsSuccess = false, Message = "کاربر یافت نشد" });
            }

            // 2. اعتبارسنجی اولیه فایل (اعتبارسنجی کامل در AttachmentService انجام می‌شود)
            if (file == null || file.Length == 0)
            {
                return Ok(new ResultDto { IsSuccess = false, Message = "فایلی انتخاب نشده است" });
            }

            try
            {
                // 3. حذف تصویر قبلی (اگر وجود داشته باشد)
                if (user.ProfilePictureId.HasValue)
                {
                    await _attachmentService.HardDeleteAsync(
                        user.ProfilePictureId.Value,
                        userId: GetUserId(),
                        ipAddress: ipAddress);
                }

                // 4. آپلود تصویر جدید
                using var stream = file.OpenReadStream();
                var uploadRequest = new AttachmentUploadRequest
                {
                    EntityType = "User",
                    EntityId = user.Id,
                    EntityPublicId = user.PublicId,
                    AttachmentType = AttachmentType.ProfileImage,
                    SensitivityLevel = FileSensitivityLevel.Internal,
                    Description = $"تصویر پروفایل کاربر {user.UserName}",
                    UploadedByUserId = GetUserId(),
                    IpAddress = ipAddress,
                    tblCustomerId = user.tblCustomerId,
                    tblShobeId = user.tblShobeId
                };

                var result = await _attachmentService.UploadAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    uploadRequest);

                if (!result.IsSuccess)
                {
                    return Ok(new ResultDto { IsSuccess = false, Message = result.ErrorMessage });
                }

                // 5. ذخیره شناسه در کاربر
                user.ProfilePictureId = result.PublicId;
                await _context.SaveChangesAsync();

                // 6. ثبت رویداد در Audit Log
                await AuditLogService.LogEventAsync(
                    eventType: "ProfilePictureUploaded",
                    entityType: "User",
                    entityId: publicId.ToString(),
                    isSuccess: true,
                    ipAddress: ipAddress,
                    userName: currentUserName,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"تصویر پروفایل کاربر {user.UserName} آپلود شد",
                    ct: default);

                return Ok(new
                {
                    success = true,
                    message = "تصویر پروفایل با موفقیت آپلود شد",
                    profilePictureId = result.PublicId,
                    fileName = result.FileName
                });
            }
            catch (Exception ex)
            {
                await AuditLogService.LogEventAsync(
                    eventType: "ProfilePictureUploadFailed",
                    entityType: "User",
                    entityId: publicId.ToString(),
                    isSuccess: false,
                    ipAddress: ipAddress,
                    userName: currentUserName,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"خطا در آپلود تصویر پروفایل: {ex.Message}",
                    ct: default);

                return Ok(new ResultDto { IsSuccess = false, Message = "خطا در آپلود تصویر پروفایل" });
            }
        }

        /// <summary>
        /// دریافت تصویر پروفایل کاربر
        /// ============================================
        /// پیاده‌سازی الزامات FDP_ACC.2, FDP_ACF.1 (کنترل دسترسی)
        /// پیاده‌سازی الزام FAU_GEN.1 (ثبت رویداد دسترسی)
        /// ============================================
        /// </summary>
        /// <param name="publicId">شناسه عمومی کاربر</param>
        /// <returns>فایل تصویر پروفایل</returns>
        [HttpGet("{publicId}/profile-picture")]
        [RequirePermission("Users.ReadById")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetProfilePicture(Guid publicId)
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var currentUserName = User.Identity?.Name ?? "Unknown";

            // پیدا کردن کاربر
            var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.PublicId == publicId);
            if (user == null)
            {
                return Ok(new ResultDto { IsSuccess = false, Message = "کاربر یافت نشد" });
            }

            // بررسی وجود تصویر پروفایل
            if (!user.ProfilePictureId.HasValue)
            {
                return Ok(new ResultDto { IsSuccess = false, Message = "تصویر پروفایل یافت نشد" });
            }

            try
            {
                // دانلود فایل از سرویس
                var result = await _attachmentService.DownloadAsync(
                    user.ProfilePictureId.Value,
                    userId: GetUserId(),
                    ipAddress: ipAddress);

                if (!result.IsSuccess || result.FileStream == null)
                {
                    // ثبت رویداد ناموفق
                    await AuditLogService.LogEventAsync(
                        eventType: "ProfilePictureViewFailed",
                        entityType: "User",
                        entityId: publicId.ToString(),
                        isSuccess: false,
                        ipAddress: ipAddress,
                        userName: currentUserName,
                        operatingSystem: operatingSystem,
                        userAgent: userAgent,
                        description: $"خطا در دریافت تصویر پروفایل کاربر {user.UserName}: {result.ErrorMessage}",
                        ct: default);

                    return Ok(new ResultDto { IsSuccess = false, Message = "تصویر پروفایل یافت نشد" });
                }

                // ثبت رویداد موفق
                await AuditLogService.LogEventAsync(
                    eventType: "ProfilePictureViewed",
                    entityType: "User",
                    entityId: publicId.ToString(),
                    isSuccess: true,
                    ipAddress: ipAddress,
                    userName: currentUserName,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"تصویر پروفایل کاربر {user.UserName} مشاهده شد",
                    ct: default);

                // بازگرداندن فایل
                return File(result.FileStream, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                // ثبت رویداد خطا
                await AuditLogService.LogEventAsync(
                    eventType: "ProfilePictureViewFailed",
                    entityType: "User",
                    entityId: publicId.ToString(),
                    isSuccess: false,
                    ipAddress: ipAddress,
                    userName: currentUserName,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"خطا در دریافت تصویر پروفایل: {ex.Message}",
                    ct: default);

                return Ok(new ResultDto { IsSuccess = false, Message = "خطا در دریافت تصویر پروفایل" });
            }
        }

        /// <summary>
        /// حذف تصویر پروفایل کاربر
        /// ============================================
        /// پیاده‌سازی الزام FDP_RIP.2 (حذف امن)
        /// پیاده‌سازی الزام FAU_GEN.1 (ثبت رویداد)
        /// ============================================
        /// </summary>
        /// <param name="publicId">شناسه عمومی کاربر</param>
        /// <returns>نتیجه حذف</returns>
        [HttpDelete("{publicId}/profile-picture")]
        [RequirePermission("Users.DeleteProfilePicture")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteProfilePicture(Guid publicId)
        {
            var ipAddress = HttpContextHelper.GetIpAddress(HttpContext);
            var userAgent = HttpContextHelper.GetUserAgent(HttpContext);
            var operatingSystem = HttpContextHelper.GetOperatingSystem(userAgent);
            var currentUserName = User.Identity?.Name ?? "Unknown";

            // پیدا کردن کاربر
            var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.PublicId == publicId);
            if (user == null)
            {
                return Ok(new ResultDto { IsSuccess = false, Message = "کاربر یافت نشد" });
            }

            // بررسی وجود تصویر پروفایل
            if (!user.ProfilePictureId.HasValue)
            {
                return Ok(new ResultDto { IsSuccess = false, Message = "تصویر پروفایل یافت نشد" });
            }

            try
            {
                // حذف فایل از سرویس (حذف امن - FDP_RIP.2)
                var deleteResult = await _attachmentService.HardDeleteAsync(
                    user.ProfilePictureId.Value,
                    userId: GetUserId(),
                    ipAddress: ipAddress);

                if (!deleteResult)
                {
                    return Ok(new ResultDto { IsSuccess = false, Message = "خطا در حذف تصویر پروفایل" });
                }

                // پاک کردن شناسه از کاربر
                user.ProfilePictureId = null;
                await _context.SaveChangesAsync();

                // ثبت رویداد در Audit Log
                await AuditLogService.LogEventAsync(
                    eventType: "ProfilePictureDeleted",
                    entityType: "User",
                    entityId: publicId.ToString(),
                    isSuccess: true,
                    ipAddress: ipAddress,
                    userName: currentUserName,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"تصویر پروفایل کاربر {user.UserName} حذف شد",
                    ct: default);

                return Ok(new ResultDto { IsSuccess = true, Message = "تصویر پروفایل با موفقیت حذف شد" });
            }
            catch (Exception ex)
            {
                await AuditLogService.LogEventAsync(
                    eventType: "ProfilePictureDeleteFailed",
                    entityType: "User",
                    entityId: publicId.ToString(),
                    isSuccess: false,
                    ipAddress: ipAddress,
                    userName: currentUserName,
                    operatingSystem: operatingSystem,
                    userAgent: userAgent,
                    description: $"خطا در حذف تصویر پروفایل: {ex.Message}",
                    ct: default);

                return Ok(new ResultDto { IsSuccess = false, Message = "خطا در حذف تصویر پروفایل" });
            }
        }

        ///// <summary>
        ///// بررسی وجود تصویر پروفایل کاربر
        ///// </summary>
        ///// <param name="publicId">شناسه عمومی کاربر</param>
        ///// <returns>وضعیت تصویر پروفایل</returns>
        //[HttpHead("{publicId}/profile-picture")]
        //[RequirePermission("Users.Read")]
        //[ProducesResponseType(200)]
        //[ProducesResponseType(404)]
        //public async Task<IActionResult> CheckProfilePicture(Guid publicId)
        //{
        //    var user = await _context.tblUsers.FirstOrDefaultAsync(u => u.PublicId == publicId);
        //    if (user == null || !user.ProfilePictureId.HasValue)
        //    {
        //        return NotFound();
        //    }

        //    return Ok();
        //}

        #endregion

        #region DTOs

     

        /// <summary>
        /// فیلتر خروجی کاربران
        /// </summary>
        public class ExportUsersFilter
        {
            /// <summary>
            /// فقط کاربران فعال
            /// </summary>
            public bool? IsActive { get; set; }

            /// <summary>
            /// جستجو بر اساس نام کاربری
            /// </summary>
            public string? UserName { get; set; }
        }

        /// <summary>
        /// یک رکورد سابقه ورود/خروج برای فرانت
        /// </summary>
        public class LoginLogoutRecordDto
        {
            public long Id { get; set; }
            public string EventDateTime { get; set; } = string.Empty;
            public string EventType { get; set; } = string.Empty;
            public bool IsSuccess { get; set; }
            public string? ErrorMessage { get; set; }
            public string? IpAddress { get; set; }
            public string? UserName { get; set; }
            public string? OperatingSystem { get; set; }
            public string? UserAgent { get; set; }
            public string? Description { get; set; }
        }

        /// <summary>
        /// نتیجه صفحه‌بندی شده سوابق ورود/خروج
        /// </summary>
        public class LoginLogoutHistoryPagedResult
        {
            public List<LoginLogoutRecordDto> Items { get; set; } = new();
            public int TotalCount { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalPages { get; set; }
            public bool HasPreviousPage { get; set; }
            public bool HasNextPage { get; set; }
        }

        #endregion
    }
}

