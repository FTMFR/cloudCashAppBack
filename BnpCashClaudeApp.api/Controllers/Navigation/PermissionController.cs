using BnpCashClaudeApp.api.Attributes;
using BnpCashClaudeApp.api.Controllers.Base;
using BnpCashClaudeApp.api.Extensions;
using BnpCashClaudeApp.api.Helpers;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.api.Controllers.Navigation
{
    /// <summary>
    /// کنترلر مدیریت Permission ها
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF و FAU_GEN از استاندارد ISO 15408
    /// کنترل دسترسی دقیق بر اساس Permission
    /// ============================================
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("ApiPolicy")]
    public class PermissionController : AuditControllerBase
    {
        private readonly NavigationDbContext _context;
        private readonly IPermissionService _permissionService;
        private readonly IDataExportService _dataExportService;

        public PermissionController(
            NavigationDbContext context,
            IPermissionService permissionService,
            IDataExportService dataExportService,
            IAuditLogService auditLogService)
            : base(auditLogService)
        {
            _context = context;
            _permissionService = permissionService;
            _dataExportService = dataExportService;
        }

        #region DTOs

        public class PermissionDto
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Resource { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsActive { get; set; }
            public int DisplayOrder { get; set; }
        }

        public class GroupPermissionDto
        {
            public long GroupId { get; set; }
            public string GroupTitle { get; set; } = string.Empty;
            public List<PermissionDto> Permissions { get; set; } = new();
        }

        public class GrantPermissionRequest
        {
            public long GroupId { get; set; }
            public long PermissionId { get; set; }
        }

        public class BulkGrantPermissionRequest
        {
            public long GroupId { get; set; }
            public List<long> PermissionIds { get; set; } = new();
        }

        public class UserPermissionsDto
        {
            public long UserId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public List<string> Permissions { get; set; } = new();
            public Dictionary<string, List<string>> PermissionsByResource { get; set; } = new();
        }

        public class MenuPermissionDto
        {
            public long MenuId { get; set; }
            public string MenuTitle { get; set; } = string.Empty;
            public List<MenuPermissionDetailDto> Permissions { get; set; } = new();
        }

        public class MenuPermissionDetailDto
        {
            public long PermissionId { get; set; }
            public string PermissionName { get; set; } = string.Empty;
            public bool IsRequired { get; set; }
        }

        public class AssignMenuPermissionRequest
        {
            public long MenuId { get; set; }
            public long PermissionId { get; set; }
            public bool IsRequired { get; set; } = true;
        }

        public class BulkAssignMenuPermissionRequest
        {
            public long MenuId { get; set; }
            public List<MenuPermissionDetailDto> Permissions { get; set; } = new();
        }

        // DTOs برای UI مدیریت دسترسی
        public class GroupAccessTreeDto
        {
            public Guid GroupPublicId { get; set; }
            public string GroupTitle { get; set; } = string.Empty;
            public List<MenuAccessNodeDto> Menus { get; set; } = new();
        }

        public class MenuAccessNodeDto
        {
            public long MenuId { get; set; }
            public string MenuTitle { get; set; } = string.Empty;
            public string? MenuPath { get; set; }
            public bool IsMenu { get; set; }
            public bool HasAccess { get; set; }
            public List<PermissionAccessDto> Permissions { get; set; } = new();
            public List<MenuAccessNodeDto> Children { get; set; } = new();
        }

        public class PermissionAccessDto
        {
            public long PermissionId { get; set; }
            public string PermissionName { get; set; } = string.Empty;
            public string Resource { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public bool HasAccess { get; set; }
        }

        public class UpdateGroupAccessRequest
        {
            public List<PermissionAccessItem> AccessItems { get; set; } = new();
        }

        public class PermissionAccessItem
        {
            public long PermissionId { get; set; }
            public bool HasAccess { get; set; }
        }

        #endregion

        /// <summary>
        /// دریافت لیست تمام Permission ها
        /// </summary>
        //[HttpGet]
        //[RequirePermission("Permissions.Read")]
        //[ProducesResponseType(typeof(List<PermissionDto>), 200)]
        //public async Task<ActionResult<List<PermissionDto>>> GetAll()
        //{
        //    try
        //    {
        //        var permissions = await _context.tblPermissions
        //            .AsNoTracking()
        //            .OrderBy(p => p.Resource)
        //            .ThenBy(p => p.DisplayOrder)
        //            .Select(p => new PermissionDto
        //            {
        //                Id = p.Id,
        //                Name = p.Name,
        //                Resource = p.Resource,
        //                Action = p.Action,
        //                Description = p.Description,
        //                IsActive = p.IsActive,
        //                DisplayOrder = p.DisplayOrder
        //            })
        //            .ToListAsync();

        //        var protectedPermissions = await ProtectReadPayloadAsync(permissions, "Permission");
        //        return Ok(protectedPermissions);
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        return StatusCode(403, new { success = false, error = ex.Message });
        //    }
        //}

        /// <summary>
        /// دریافت Permission ها به تفکیک Resource
        /// </summary>
        //[HttpGet("by-resource")]
        //[RequirePermission("Permissions.Read")]
        //[ProducesResponseType(typeof(Dictionary<string, List<PermissionDto>>), 200)]
        //public async Task<ActionResult<Dictionary<string, List<PermissionDto>>>> GetByResource()
        //{
        //    try
        //    {
        //        var permissions = await _context.tblPermissions
        //            .AsNoTracking()
        //            .Where(p => p.IsActive)
        //            .OrderBy(p => p.DisplayOrder)
        //            .ToListAsync();

        //        var grouped = permissions
        //            .GroupBy(p => p.Resource)
        //            .ToDictionary(
        //                g => g.Key,
        //                g => g.Select(p => new PermissionDto
        //                {
        //                    Id = p.Id,
        //                    Name = p.Name,
        //                    Resource = p.Resource,
        //                    Action = p.Action,
        //                    Description = p.Description,
        //                    IsActive = p.IsActive,
        //                    DisplayOrder = p.DisplayOrder
        //                }).ToList()
        //            );

        //        var protectedGrouped = await ProtectReadPayloadAsync(grouped, "PermissionByResource");
        //        return Ok(protectedGrouped);
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        return StatusCode(403, new { success = false, error = ex.Message });
        //    }
        //}

        /// <summary>
        /// دریافت Permission های یک گروه
        /// </summary>
        //[HttpGet("group/{groupId}")]
        //[RequirePermission("Permissions.Read")]
        //[ProducesResponseType(typeof(GroupPermissionDto), 200)]
        //[ProducesResponseType(404)]
        //public async Task<ActionResult<GroupPermissionDto>> GetGroupPermissions(int groupId)
        //{
        //    try
        //    {
        //        var group = await _context.tblGrps
        //            .AsNoTracking()
        //            .FirstOrDefaultAsync(g => g.Id == groupId);

        //        if (group == null)
        //        {
        //            return NotFound(new { message = "گروه یافت نشد" });
        //        }

        //        var permissions = await _context.tblGrpPermissions
        //            .AsNoTracking()
        //            .Where(gp => gp.tblGrpId == groupId && gp.IsGranted)
        //            .Include(gp => gp.tblPermission)
        //            .Select(gp => new PermissionDto
        //            {
        //                Id = gp.tblPermission.Id,
        //                Name = gp.tblPermission.Name,
        //                Resource = gp.tblPermission.Resource,
        //                Action = gp.tblPermission.Action,
        //                Description = gp.tblPermission.Description,
        //                IsActive = gp.tblPermission.IsActive,
        //                DisplayOrder = gp.tblPermission.DisplayOrder
        //            })
        //            .ToListAsync();

        //        var response = new GroupPermissionDto
        //        {
        //            GroupId = group.Id,
        //            GroupTitle = group.Title,
        //            Permissions = permissions
        //        };

        //        var protectedResponse = await ProtectReadPayloadAsync(response, "GroupPermission", groupId.ToString());
        //        return Ok(protectedResponse);
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        return StatusCode(403, new { success = false, error = ex.Message });
        //    }
        //}

        /// <summary>
        /// دریافت Permission های کاربر جاری
        /// </summary>
        [HttpGet("my-permissions")]
        [ProducesResponseType(typeof(UserPermissionsDto), 200)]
        public async Task<ActionResult<UserPermissionsDto>> GetMyPermissions()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "شناسه کاربر نامعتبر است" });
                }

                var userName = User.Identity?.Name ?? "Unknown";
                var permissions = await _permissionService.GetUserPermissionsAsync(userId);
                var permissionsByResource = await _permissionService.GetUserPermissionsByResourceAsync(userId);

                var response = new UserPermissionsDto
                {
                    UserId = userId,
                    UserName = userName,
                    Permissions = permissions,
                    PermissionsByResource = permissionsByResource
                };

                var protectedResponse = await ProtectReadPayloadAsync(response, "UserPermission", userId.ToString());
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// اعطای Permission به گروه
        /// </summary>
        //[HttpPost("grant")]
        //[RequirePermission("Permissions.Manage")]
        //[ProducesResponseType(200)]
        //[ProducesResponseType(400)]
        //public async Task<IActionResult> GrantPermission([FromBody] GrantPermissionRequest request)
        //{
        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (!int.TryParse(userIdClaim, out var grantedBy))
        //    {
        //        return Unauthorized(new { message = "شناسه کاربر نامعتبر است" });
        //    }

        //    var group = await _context.tblGrps.FindAsync(request.GroupId);
        //    var permission = await _context.tblPermissions.FindAsync(request.PermissionId);

        //    if (group == null)
        //    {
        //        return BadRequest(new { message = "گروه یافت نشد" });
        //    }

        //    if (permission == null)
        //    {
        //        return BadRequest(new { message = "Permission یافت نشد" });
        //    }

        //    await _permissionService.GrantPermissionToGroupAsync(
        //        request.GroupId,
        //        request.PermissionId,
        //        grantedBy);

        //    await LogAuditEventAsync("PermissionGranted", "GrpPermission", $"{request.GroupId}-{request.PermissionId}", true,
        //        description: $"Permission {permission.Name} به گروه {group.Title} اعطا شد");

        //    return Ok(new
        //    {
        //        message = $"Permission {permission.Name} به گروه {group.Title} اعطا شد"
        //    });
        //}

        /// <summary>
        /// اعطای چندین Permission به گروه
        /// </summary>
        //[HttpPost("grant-bulk")]
        //[RequirePermission("Permissions.Manage")]
        //[ProducesResponseType(200)]
        //[ProducesResponseType(400)]
        //public async Task<IActionResult> GrantBulkPermissions([FromBody] BulkGrantPermissionRequest request)
        //{
        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (!int.TryParse(userIdClaim, out var grantedBy))
        //    {
        //        return Unauthorized(new { message = "شناسه کاربر نامعتبر است" });
        //    }

        //    var group = await _context.tblGrps.FindAsync(request.GroupId);
        //    if (group == null)
        //    {
        //        return BadRequest(new { message = "گروه یافت نشد" });
        //    }

        //    var grantedCount = 0;
        //    foreach (var permissionId in request.PermissionIds)
        //    {
        //        var permission = await _context.tblPermissions.FindAsync(permissionId);
        //        if (permission != null)
        //        {
        //            await _permissionService.GrantPermissionToGroupAsync(
        //                request.GroupId, 
        //                permissionId, 
        //                grantedBy);
        //            grantedCount++;
        //        }
        //    }

        //    await LogAuditEventAsync("PermissionGrantedBulk", "GrpPermission", request.GroupId.ToString(), true,
        //        description: $"{grantedCount} Permission به گروه {group.Title} اعطا شد");

        //    return Ok(new
        //    {
        //        message = $"{grantedCount} Permission به گروه {group.Title} اعطا شد"
        //    });
        //}

        /// <summary>
        /// حذف Permission از گروه
        /// </summary>
        //[HttpPost("revoke")]
        //[RequirePermission("Permissions.Manage")]
        //[ProducesResponseType(200)]
        //[ProducesResponseType(400)]
        //public async Task<IActionResult> RevokePermission([FromBody] GrantPermissionRequest request)
        //{
        //    var group = await _context.tblGrps.FindAsync(request.GroupId);
        //    var permission = await _context.tblPermissions.FindAsync(request.PermissionId);

        //    if (group == null)
        //    {
        //        return BadRequest(new { message = "گروه یافت نشد" });
        //    }

        //    if (permission == null)
        //    {
        //        return BadRequest(new { message = "Permission یافت نشد" });
        //    }

        //    await _permissionService.RevokePermissionFromGroupAsync(
        //        request.GroupId,
        //        request.PermissionId);

        //    await LogAuditEventAsync("PermissionRevoked", "GrpPermission", $"{request.GroupId}-{request.PermissionId}", true,
        //        description: $"Permission {permission.Name} از گروه {group.Title} حذف شد");

        //    return Ok(new
        //    {
        //        message = $"Permission {permission.Name} از گروه {group.Title} حذف شد"
        //    });
        //}

        /// <summary>
        /// بررسی دسترسی کاربر جاری به یک Permission
        /// </summary>
        //[HttpGet("check/{permission}")]
        //[ProducesResponseType(typeof(bool), 200)]
        //public async Task<ActionResult<bool>> CheckPermission(string permission)
        //{
        //    try
        //    {
        //        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //        if (!int.TryParse(userIdClaim, out var userId))
        //        {
        //            return Unauthorized(new { message = "شناسه کاربر نامعتبر است" });
        //        }

        //        var hasPermission = await _permissionService.HasPermissionAsync(userId, permission);
        //        return Ok(hasPermission);
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        return StatusCode(403, new { success = false, error = ex.Message });
        //    }
        //}

        #region Menu Permission Management

        /// <summary>
        /// اختصاص Permission به منو
        /// </summary>
        //[HttpPost("menu/assign")]
        //[RequirePermission("Permissions.Manage")]
        //[ProducesResponseType(200)]
        //[ProducesResponseType(400)]
        //public async Task<IActionResult> AssignPermissionToMenu([FromBody] AssignMenuPermissionRequest request)
        //{
        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (!int.TryParse(userIdClaim, out var assignedBy))
        //    {
        //        return Unauthorized(new { message = "شناسه کاربر نامعتبر است" });
        //    }

        //    var menu = await _context.tblMenus.FindAsync(request.MenuId);
        //    var permission = await _context.tblPermissions.FindAsync(request.PermissionId);

        //    if (menu == null)
        //    {
        //        return BadRequest(new { message = "منو یافت نشد" });
        //    }

        //    if (permission == null)
        //    {
        //        return BadRequest(new { message = "Permission یافت نشد" });
        //    }

        //    await _permissionService.AssignPermissionToMenuAsync(
        //        request.MenuId,
        //        request.PermissionId,
        //        request.IsRequired,
        //        assignedBy);

        //    await LogAuditEventAsync("MenuPermissionAssigned", "MenuPermission", $"{request.MenuId}-{request.PermissionId}", true,
        //        description: $"Permission {permission.Name} به منوی {menu.Title} اختصاص داده شد (الزامی: {request.IsRequired})");

        //    return Ok(new
        //    {
        //        message = $"Permission {permission.Name} به منوی {menu.Title} اختصاص داده شد (الزامی: {request.IsRequired})"
        //    });
        //}

        /// <summary>
        /// اختصاص چندین Permission به منو
        /// </summary>
        //[HttpPost("menu/assign-bulk")]
        //[RequirePermission("Permissions.Manage")]
        //[ProducesResponseType(200)]
        //[ProducesResponseType(400)]
        //public async Task<IActionResult> AssignBulkPermissionsToMenu([FromBody] BulkAssignMenuPermissionRequest request)
        //{
        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (!int.TryParse(userIdClaim, out var assignedBy))
        //    {
        //        return Unauthorized(new { message = "شناسه کاربر نامعتبر است" });
        //    }

        //    var menu = await _context.tblMenus.FindAsync(request.MenuId);
        //    if (menu == null)
        //    {
        //        return BadRequest(new { message = "منو یافت نشد" });
        //    }

        //    var assignedCount = 0;
        //    foreach (var perm in request.Permissions)
        //    {
        //        var permission = await _context.tblPermissions.FindAsync(perm.PermissionId);
        //        if (permission != null)
        //        {
        //            await _permissionService.AssignPermissionToMenuAsync(
        //                request.MenuId,
        //                perm.PermissionId,
        //                perm.IsRequired,
        //                assignedBy);
        //            assignedCount++;
        //        }
        //    }

        //    await LogAuditEventAsync("MenuPermissionAssignedBulk", "MenuPermission", request.MenuId.ToString(), true,
        //        description: $"{assignedCount} Permission به منوی {menu.Title} اختصاص داده شد");

        //    return Ok(new
        //    {
        //        message = $"{assignedCount} Permission به منوی {menu.Title} اختصاص داده شد"
        //    });
        //}

        /// <summary>
        /// حذف Permission از منو
        /// </summary>
        //[HttpPost("menu/remove")]
        //[RequirePermission("Permissions.Manage")]
        //[ProducesResponseType(200)]
        //[ProducesResponseType(400)]
        //public async Task<IActionResult> RemovePermissionFromMenu([FromBody] AssignMenuPermissionRequest request)
        //{
        //    var menu = await _context.tblMenus.FindAsync(request.MenuId);
        //    var permission = await _context.tblPermissions.FindAsync(request.PermissionId);

        //    if (menu == null)
        //    {
        //        return BadRequest(new { message = "منو یافت نشد" });
        //    }

        //    if (permission == null)
        //    {
        //        return BadRequest(new { message = "Permission یافت نشد" });
        //    }

        //    await _permissionService.RemovePermissionFromMenuAsync(
        //        request.MenuId,
        //        request.PermissionId);

        //    await LogAuditEventAsync("MenuPermissionRemoved", "MenuPermission", $"{request.MenuId}-{request.PermissionId}", true,
        //        description: $"Permission {permission.Name} از منوی {menu.Title} حذف شد");

        //    return Ok(new
        //    {
        //        message = $"Permission {permission.Name} از منوی {menu.Title} حذف شد"
        //    });
        //}

        /// <summary>
        /// دریافت درخت منوها و دسترسی‌ها برای یک گروه
        /// ============================================
        /// برای استفاده در UI مدیریت دسترسی
        /// تمام منوها به صورت درختی برگردانده می‌شوند
        /// برای هر منو، Permission های مرتبط و وضعیت دسترسی گروه نمایش داده می‌شود
        /// ============================================
        /// </summary>
        [HttpGet("group/{groupPublicId}/access-tree")]
        [RequirePermission("Permissions.Read")]
        [ProducesResponseType(typeof(GroupAccessTreeDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<GroupAccessTreeDto>> GetGroupAccessTree(Guid groupPublicId)
        {
            try
            {
                var group = await _context.tblGrps
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.PublicId == groupPublicId);

                if (group == null)
                {
                    return NotFound(new { message = "گروه یافت نشد" });
                }

                // دریافت تمام منوها
                var allMenus = await _context.tblMenus
                    .AsNoTracking()
                    .OrderBy(m => m.Id)
                    .ToListAsync();

                // دریافت Permission های گروه
                var groupPermissionIds = await _context.tblGrpPermissions
                    .AsNoTracking()
                    .Where(gp => gp.tblGrpId == group.Id && gp.IsGranted)
                    .Select(gp => gp.tblPermissionId)
                    .ToListAsync();

                var groupPermissionSet = new HashSet<long>(groupPermissionIds);

                // دریافت تمام Permission ها
                var allPermissions = await _context.tblPermissions
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .ToListAsync();

                // دریافت ارتباط منوها و Permission ها
                var menuPermissions = await _context.tblMenuPermissions
                    .AsNoTracking()
                    .Include(mp => mp.tblPermission)
                    .ToListAsync();

                // ساخت درخت منوها
                var rootMenus = allMenus
                    .Where(m => m.ParentId == null)
                    .Select(m => BuildMenuAccessNode(m, allMenus, allPermissions, menuPermissions, groupPermissionSet))
                    .ToList();

                var response = new GroupAccessTreeDto
                {
                    GroupPublicId = group.PublicId,
                    GroupTitle = group.Title,
                    Menus = rootMenus
                };

                var protectedResponse = await ProtectReadPayloadAsync(response, "GroupAccessTree", groupPublicId.ToString());
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// به‌روزرسانی دسترسی‌های یک گروه
        /// ============================================
        /// با تغییر toggle ها در UI، دسترسی‌های گروه به Permission ها به‌روزرسانی می‌شود
        /// ============================================
        /// </summary>
        [HttpPost("group/{groupPublicId}/update-access")]
        [RequirePermission("Permissions.Manage")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateGroupAccess(Guid groupPublicId, [FromBody] UpdateGroupAccessRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var grantedBy))
            {
                return Unauthorized(new { message = "شناسه کاربر نامعتبر است" });
            }

            var group = await _context.tblGrps.FirstOrDefaultAsync(g => g.PublicId == groupPublicId);
            if (group == null)
            {
                return BadRequest(new { message = "گروه یافت نشد" });
            }

            var groupId = group.Id;

            // دریافت Permission های فعلی گروه
            var currentPermissions = await _context.tblGrpPermissions
                .Where(gp => gp.tblGrpId == groupId)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var updatedCount = 0;
            var grantedCount = 0;
            var revokedCount = 0;

            foreach (var accessItem in request.AccessItems)
            {
                var existing = currentPermissions
                    .FirstOrDefault(gp => gp.tblPermissionId == accessItem.PermissionId);

                if (accessItem.HasAccess)
                {
                    // باید دسترسی داشته باشد
                    if (existing == null)
                    {
                        // ایجاد دسترسی جدید
                        await _permissionService.GrantPermissionToGroupAsync(
                            groupId,
                            accessItem.PermissionId,
                            grantedBy);
                        grantedCount++;
                    }
                    else if (!existing.IsGranted)
                    {
                        // فعال کردن دسترسی موجود
                        existing.IsGranted = true;
                        existing.GrantedAt = now;
                        existing.GrantedBy = grantedBy;
                        updatedCount++;
                    }
                }
                else
                {
                    // نباید دسترسی داشته باشد
                    if (existing != null && existing.IsGranted)
                    {
                        // حذف دسترسی
                        await _permissionService.RevokePermissionFromGroupAsync(
                            groupId,
                            accessItem.PermissionId);
                        revokedCount++;
                    }
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            // پاکسازی Cache
            await _permissionService.InvalidateGroupPermissionsCacheAsync(groupId);

            // پاکسازی Cache تمام کاربران این گروه
            var userIds = await _context.tblUserGrps
                .Where(ug => ug.tblGrpId == groupId)
                .Select(ug => ug.tblUserId)
                .ToListAsync();

            foreach (var userId in userIds)
            {
                await _permissionService.InvalidateUserPermissionsCacheAsync(userId);
            }

            await LogAuditEventAsync("GroupAccessUpdated", "GrpPermission", groupId.ToString(), true,
                description: $"دسترسی‌های گروه به‌روزرسانی شد - اعطا: {grantedCount}, لغو: {revokedCount}, بروز: {updatedCount}");

            return Ok(new
            {
                message = "دسترسی‌های گروه به‌روزرسانی شد",
                granted = grantedCount,
                revoked = revokedCount,
                updated = updatedCount
            });
        }

        /// <summary>
        /// ساخت گره درخت منو با اطلاعات دسترسی
        /// </summary>
        private MenuAccessNodeDto BuildMenuAccessNode(
            tblMenu menu,
            List<tblMenu> allMenus,
            List<tblPermission> allPermissions,
            List<tblMenuPermission> menuPermissions,
            HashSet<long> groupPermissionIds)
        {
            var node = new MenuAccessNodeDto
            {
                MenuId = menu.Id,
                MenuTitle = menu.Title,
                MenuPath = menu.Path,
                IsMenu = menu.IsMenu,
                HasAccess = false,
                Permissions = new List<PermissionAccessDto>()
            };

            // دریافت Permission های مرتبط با این منو
            var relatedMenuPermissions = menuPermissions
                .Where(mp => mp.tblMenuId == menu.Id)
                .ToList();

            // ساخت لیست Permission ها با وضعیت دسترسی
            foreach (var menuPerm in relatedMenuPermissions)
            {
                var perm = menuPerm.tblPermission;
                var hasAccess = groupPermissionIds.Contains(perm.Id);
                
                node.Permissions.Add(new PermissionAccessDto
                {
                    PermissionId = perm.Id,
                    PermissionName = perm.Name,
                    Resource = perm.Resource,
                    Action = perm.Action,
                    HasAccess = hasAccess
                });
            }

            // محاسبه دسترسی به منو
            if (relatedMenuPermissions.Any())
            {
                // منطق: اگر همه Permission های IsRequired=true را داشته باشد، دسترسی دارد
                var requiredPermissions = relatedMenuPermissions
                    .Where(mp => mp.IsRequired)
                    .Select(mp => mp.tblPermissionId)
                    .ToList();

                if (requiredPermissions.Any())
                {
                    // همه Permission های الزامی باید وجود داشته باشند
                    node.HasAccess = requiredPermissions.All(p => groupPermissionIds.Contains(p));
                }
                else
                {
                    // اگر هیچ Permission الزامی نباشد، داشتن حداقل یکی کافی است
                    var optionalPermissions = relatedMenuPermissions
                        .Select(mp => mp.tblPermissionId)
                        .ToList();

                    node.HasAccess = optionalPermissions.Any(p => groupPermissionIds.Contains(p));
                }
            }
            else
            {
                // اگر منو Permission نداشته باشد، همه دسترسی دارند
                node.HasAccess = true;
            }

            // ساخت فرزندان
            var children = allMenus
                .Where(m => m.ParentId == menu.Id)
                .Select(m => BuildMenuAccessNode(m, allMenus, allPermissions, menuPermissions, groupPermissionIds))
                .ToList();

            node.Children = children;

            return node;
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
