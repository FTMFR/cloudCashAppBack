using BnpCashClaudeApp.api.Attributes;
using BnpCashClaudeApp.api.Controllers.Base;
using BnpCashClaudeApp.api.Extensions;
using BnpCashClaudeApp.api.Helpers;
using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Application.DTOs.Common;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.MediatR.Commands;
using BnpCashClaudeApp.Application.MediatR.Queries;
using BnpCashClaudeApp.Persistence.Migrations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading;

namespace BnpCashClaudeApp.api.Controllers.Navigation
{
    /// <summary>
    /// کنترلر مدیریت منوها
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF و FAU_GEN از استاندارد ISO 15408
    /// ============================================
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : AuditControllerBase
    {
        private readonly IMediator _mediator;
        private readonly NavigationDbContext _context;
        private readonly IPermissionService _permissionService;
        private readonly IDataExportService _dataExportService;

        public MenuController(
            IMediator mediator,
            NavigationDbContext context,
            IPermissionService permissionService,
            IDataExportService dataExportService,
            IAuditLogService auditLogService)
            : base(auditLogService)
        {
            _mediator = mediator;
            _context = context;
            _permissionService = permissionService;
            _dataExportService = dataExportService;
        }

        /// <summary>
        /// دریافت تمام منوها (بدون فیلتر دسترسی)
        /// </summary>
        [HttpGet]
        [RequirePermission("Menus.Read")]
        public async Task<ActionResult<List<MenuDto>>> GetAll()
        {
            try
            {
                var query = new GetAllMenusQuery();
                var result = await _mediator.Send(query);
                var protectedResult = await ProtectReadPayloadAsync(result, "Menu");
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        ///// <summary>
        ///// دریافت تمام منوها به صورت درختی (بدون فیلتر دسترسی)
        ///// </summary>
        //[HttpGet("tree")]
        //public async Task<ActionResult<List<MenuDto>>> GetTree()
        //{
        //    var query = new GetMenuTreeQuery();
        //    var result = await _mediator.Send(query);
        //    return Ok(result);
        //}

        /// <summary>
        /// دریافت منوهای قابل دسترس کاربر جاری به صورت درختی
        /// ============================================
        /// این endpoint فقط منوهایی را برمی‌گرداند که کاربر به آنها دسترسی دارد
        /// برای استفاده در فرانت بعد از لاگین
        /// ============================================
        /// </summary>
        [HttpGet("my-tree")]
        [ProducesResponseType(typeof(List<MenuDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<List<MenuDto>>> GetMyMenuTree()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "شناسه کاربر نامعتبر است" });
                }

                var query = new GetMyMenuTreeQuery { UserId = userId };
                var result = await _mediator.Send(query);
                var protectedResult = await ProtectReadPayloadAsync(result, "UserMenuTree", userId.ToString());
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت یک منو با شناسه عمومی
        /// </summary>
        [HttpGet("{publicId}")]
        [RequirePermission("Menus.Read")]
        public async Task<ActionResult<MenuDto>> GetById(Guid publicId)
        {
            try
            {
                var query = new GetMenuByIdQuery { PublicId = publicId };
                var result = await _mediator.Send(query);

                if (result == null)
                    return NotFound(new { success = false, message = "منو یافت نشد" });

                var protectedResult = await ProtectReadPayloadAsync(result, "Menu", publicId.ToString());
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// ایجاد منوی جدید
        /// </summary>
        [HttpPost]
        [RequirePermission("Menus.Create")]
        public async Task<ActionResult<Guid>> Create([FromBody] CreateMenuDto dto)
        {
            var tblUserGrpIdInsert = User.GetTblUserGrpIdInsert();
            if (tblUserGrpIdInsert == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new CreateMenuCommand
            {
                Title = dto.Title,
                Path = dto.Path,
                ParentPublicId = dto.ParentPublicId,
                TblUserGrpIdInsert = tblUserGrpIdInsert.Value,
                tblSoftwareId = dto.tblSoftwareId
            };

            var result = await _mediator.Send(command);
            await LogAuditEventAsync("Create", "Menu", result.ToString(), true);
            return CreatedAtAction(nameof(GetById), new { publicId = result }, result);
        }

        /// <summary>
        /// ویرایش منو
        /// </summary>
        [HttpPut("{publicId}")]
        [RequirePermission("Menus.Update")]
        public async Task<IActionResult> Update(Guid publicId, [FromBody] UpdateMenuDto dto)
        {
            var tblUserGrpIdLastEdit = User.GetTblUserGrpIdLastEdit();
            if (tblUserGrpIdLastEdit == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new UpdateMenuCommand
            {
                PublicId = publicId,
                Title = dto.Title,
                Path = dto.Path,
                ParentPublicId = dto.ParentPublicId,
                TblUserGrpIdLastEdit = tblUserGrpIdLastEdit.Value,
                tblSoftwareId = dto.tblSoftwareId,
                AuditUserId = User.GetUserId()
            };

            var result = await _mediator.Send(command);

            if (!result)
            {
                await LogAuditEventAsync("Update", "Menu", publicId.ToString(), false, "خطا در انجام عملیات");
                return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
            }

            return Ok(new ResultDto(true, "عملیات با موفقیت انجام شد"));
        }

        /// <summary>
        /// حذف منو
        /// </summary>
        //[HttpDelete("{publicId}")]
        //[RequirePermission("Menus.Delete")]
        //public async Task<IActionResult> Delete(Guid publicId)
        //{
        //    var command = new DeleteMenuCommand { PublicId = publicId };
        //    var result = await _mediator.Send(command);

        //    if (!result)
        //    {
        //        await LogAuditEventAsync("Delete", "Menu", publicId.ToString(), false, "خطا در انجام عملیات");
        //        return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
        //    }

        //    await LogAuditEventAsync("Delete", "Menu", publicId.ToString(), true);
        //    return Ok(new ResultDto(true, "عملیات با موفقیت انجام شد"));
        //}

        /// <summary>
        /// دیباگ: بررسی وضعیت منوها و Permission ها برای کاربر جاری
        /// </summary>
        //[HttpGet("debug")]
        //[RequirePermission("Menus.Read")]
        //public async Task<IActionResult> Debug()
        //{
        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (!long.TryParse(userIdClaim, out var userId))
        //    {
        //        return Unauthorized(new { message = "شناسه کاربر نامعتبر است" });
        //    }

        //    // دریافت Permission های کاربر
        //    var userPermissions = await _permissionService.GetUserPermissionsAsync(userId);

        //    // دریافت منوهای قابل دسترس
        //    var accessibleMenuIds = await _permissionService.GetUserAccessibleMenuIdsAsync(userId);

        //    // دریافت تمام منوها با MenuPermissions
        //    var allMenus = await _context.tblMenus
        //        .AsNoTracking()
        //        .Include(m => m.MenuPermissions)
        //            .ThenInclude(mp => mp.tblPermission)
        //        .OrderBy(m => m.Id)
        //        .ToListAsync();

        //    // تعداد MenuPermissions
        //    var totalMenuPermissions = await _context.tblMenuPermissions.CountAsync();

        //    var menuDetails = allMenus.Select(m => new
        //    {
        //        MenuId = m.Id,
        //        Title = m.Title,
        //        Path = m.Path,
        //        ParentId = m.ParentId,
        //        IsAccessible = accessibleMenuIds.Contains(m.Id),
        //        HasMenuPermissions = m.MenuPermissions.Any(),
        //        MenuPermissionsCount = m.MenuPermissions.Count,
        //        Permissions = m.MenuPermissions.Select(mp => new
        //        {
        //            PermissionName = mp.tblPermission?.Name,
        //            IsRequired = mp.IsRequired,
        //            UserHasIt = userPermissions.Contains(mp.tblPermission?.Name ?? "")
        //        }).ToList()
        //    }).ToList();

        //    return Ok(new
        //    {
        //        UserId = userId,
        //        UserName = User.Identity?.Name,
        //        UserPermissionsCount = userPermissions.Count,
        //        UserPermissions = userPermissions,
        //        TotalMenus = allMenus.Count,
        //        TotalMenuPermissionsInDb = totalMenuPermissions,
        //        MenusWithPermissions = allMenus.Count(m => m.MenuPermissions.Any()),
        //        MenusWithoutPermissions = allMenus.Count(m => !m.MenuPermissions.Any()),
        //        AccessibleMenusCount = accessibleMenuIds.Count,
        //        AccessibleMenuIds = accessibleMenuIds,
        //        MenuDetails = menuDetails
        //    });
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
