using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// پیاده‌سازی سرویس مدیریت Permission ها
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF از استاندارد ISO 15408
    /// پیاده‌سازی الزام FPT_FLS.1.1 (الزام 46) - Fail-Secure
    /// بررسی و مدیریت دسترسی‌های کاربران با استفاده از Cache
    /// ============================================
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly NavigationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PermissionService> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly IResourceAuthorizationPolicyService _resourceAuthorizationPolicyService;
        private readonly IFailSecureService _failSecureService;
        private readonly FailSecureSettings _failSecureSettings;
        private const int CACHE_EXPIRATION_MINUTES = 5;

        public PermissionService(
            NavigationDbContext context,
            IMemoryCache cache,
            ILogger<PermissionService> logger,
            IAuditLogService auditLogService,
            IResourceAuthorizationPolicyService resourceAuthorizationPolicyService,
            IFailSecureService failSecureService,
            IOptions<FailSecureSettings> failSecureSettings)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _auditLogService = auditLogService;
            _resourceAuthorizationPolicyService = resourceAuthorizationPolicyService;
            _failSecureService = failSecureService;
            _failSecureSettings = failSecureSettings.Value;
        }

        /// <summary>
        /// بررسی اینکه آیا کاربر عضو گروه Admin است
        /// </summary>
        private async Task<bool> IsUserAdminAsync(long userId, CancellationToken ct = default)
        {
            var cacheKey = $"user_is_admin_{userId}";
            if (_cache.TryGetValue<bool>(cacheKey, out var isAdmin))
            {
                return isAdmin;
            }

            // بررسی مستقیم از دیتابیس
            isAdmin = await _context.tblUserGrps
                .AnyAsync(ug => ug.tblUserId == userId && 
                               ug.tblGrp.Title == "Admin" && 
                               ug.tblGrp.IsActive, ct);

            _cache.Set(cacheKey, isAdmin, TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES));
            
            _logger.LogDebug("User {UserId} is admin: {IsAdmin}", userId, isAdmin);
            
            return isAdmin;
        }

        public async Task<bool> HasPermissionAsync(long userId, string permission, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(permission))
                return false;

            permission = permission.Trim();

            if (!await IsPermissionAllowedByPolicyAsync(userId, permission, ct))
            {
                return false;
            }

            try
            {
                // کاربر Admin به همه چیز دسترسی دارد
                if (await IsUserAdminAsync(userId, ct))
                {
                    _logger.LogDebug("User {UserId} is Admin, granting permission {Permission}", userId, permission);
                    return true;
                }

                var userPermissions = await GetUserPermissionsAsync(userId, ct);
                return userPermissions.Any(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                // ============================================
                // FPT_FLS.1.1 (الزام 46): Fail-Secure
                // در صورت خطا، دسترسی رد می‌شود (Deny by Default)
                // ============================================
                _logger.LogError(ex,
                    "FAIL-SECURE: Permission check failed for user {UserId}, permission {Permission}. ACCESS DENIED.",
                    userId, permission);

                // ثبت در Audit Log (یا فایل اگر دیتابیس قطع است)
                try
                {
                    await _auditLogService.LogEventAsync(
                        eventType: "FailSecureActivated",
                        entityType: "Permission",
                        entityId: permission,
                        isSuccess: false,
                        errorMessage: $"FAIL-SECURE: Permission check failed - {ex.GetType().Name}",
                        userId: (int)userId,
                        description: $"FAIL-SECURE: Access denied for user {userId} to permission {permission} due to system failure: {ex.GetType().Name}",
                        ct: default);
                }
                catch (Exception dbEx)
                {
                    // دیتابیس قطع است - ذخیره در فایل
                    _logger.LogWarning(dbEx, "Database unavailable. Logging failure to file.");
                    await _failSecureService.LogFailureToFileAsync(
                        failureType: ex.GetType().Name,
                        operationName: $"HasPermissionAsync({permission})",
                        details: $"User: {userId}, Permission: {permission}, Error: {ex.Message}");
                }

                // Fail-Secure: در صورت خطا، دسترسی رد می‌شود
                return _failSecureSettings.DefaultAccessOnFailure;
            }
        }

        public async Task<bool> HasPermissionAsync(long userId, string resource, string action, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(action))
                return false;

            var permission = $"{resource.Trim()}.{action.Trim()}";
            return await HasPermissionAsync(userId, permission, ct);
        }

        public async Task<List<string>> GetUserPermissionsAsync(long userId, CancellationToken ct = default)
        {
            // بررسی Cache
            var cacheKey = $"user_permissions_{userId}";
            if (_cache.TryGetValue<List<string>>(cacheKey, out var cachedPermissions))
            {
                _logger.LogDebug("Permissions for user {UserId} retrieved from cache: {Count}", userId, cachedPermissions?.Count ?? 0);
                return cachedPermissions ?? new List<string>();
            }

            // دریافت از دیتابیس
            var userPermissions = await _context.tblUserGrps
                .Where(ug => ug.tblUserId == userId)
                .Where(ug => ug.tblGrp.IsActive)
                .SelectMany(ug => ug.tblGrp.GroupPermissions)
                .Where(gp => gp.IsGranted && gp.tblPermission.IsActive)
                .Select(gp => gp.tblPermission.Name)
                .Distinct()
                .ToListAsync(ct);

            // ذخیره در Cache
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES * 2));

            _cache.Set(cacheKey, userPermissions, cacheOptions);

            _logger.LogDebug("Loaded {Count} permissions for user {UserId} from database", 
                userPermissions.Count, userId);

            return userPermissions;
        }

        public async Task<Dictionary<string, List<string>>> GetUserPermissionsByResourceAsync(long userId, CancellationToken ct = default)
        {
            var permissions = await GetUserPermissionsAsync(userId, ct);
            var result = new Dictionary<string, List<string>>();

            foreach (var permission in permissions)
            {
                var parts = permission.Split('.');
                if (parts.Length >= 2)
                {
                    var resource = parts[0];
                    var action = parts[1];

                    if (!result.ContainsKey(resource))
                        result[resource] = new List<string>();

                    result[resource].Add(action);
                }
            }

            return result;
        }

        private async Task<bool> IsPermissionAllowedByPolicyAsync(long userId, string permission, CancellationToken ct)
        {
            var policyResult = _resourceAuthorizationPolicyService.ValidatePermission(permission);
            if (policyResult.IsAllowed)
            {
                return true;
            }

            _logger.LogWarning(
                "Authorization policy denied permission {Permission} for user {UserId}. Reason: {Reason}",
                permission, userId, policyResult.DenialReason);

            try
            {
                await _auditLogService.LogEventAsync(
                    eventType: "AuthorizationPolicyDenied",
                    entityType: "PermissionPolicy",
                    entityId: permission,
                    isSuccess: false,
                    errorMessage: policyResult.DenialReason,
                    userId: userId,
                    description: $"Authorization policy denied permission '{permission}' for user {userId}. Reason: {policyResult.DenialReason}",
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to persist AuthorizationPolicyDenied audit event for user {UserId}, permission {Permission}",
                    userId, permission);
            }

            return false;
        }

        /// <summary>
        /// بررسی دسترسی کاربر به منو بر اساس Permission های مورد نیاز
        /// </summary>
        public async Task<bool> HasMenuAccessAsync(long userId, long menuId, CancellationToken ct = default)
        {
            // کاربر Admin به همه منوها دسترسی دارد
            if (await IsUserAdminAsync(userId, ct))
            {
                return true;
            }

            // بررسی Cache
            var cacheKey = $"user_menu_access_{userId}_{menuId}";
            if (_cache.TryGetValue<bool>(cacheKey, out var cachedAccess))
            {
                return cachedAccess;
            }

            // دریافت Permission های مورد نیاز منو
            var menuPermissions = await _context.tblMenuPermissions
                .Where(mp => mp.tblMenuId == menuId)
                .Include(mp => mp.tblPermission)
                .ToListAsync(ct);

            // اگر منو Permission نیاز ندارد، همه می‌توانند ببینند
            if (!menuPermissions.Any())
            {
                _cache.Set(cacheKey, true, TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES));
                return true;
            }

            // دریافت Permission های کاربر
            var userPermissions = await GetUserPermissionsAsync(userId, ct);

            bool hasAccess;
            
            // بررسی منطق AND/OR
            var requiredPermissions = menuPermissions.Where(mp => mp.IsRequired).Select(mp => mp.tblPermission.Name).ToList();
            var optionalPermissions = menuPermissions.Where(mp => !mp.IsRequired).Select(mp => mp.tblPermission.Name).ToList();

            if (requiredPermissions.Any())
            {
                // اگر Permission های الزامی وجود دارد، همه باید وجود داشته باشند (AND)
                hasAccess = requiredPermissions.All(p => userPermissions.Contains(p));
            }
            else if (optionalPermissions.Any())
            {
                // اگر فقط Permission های اختیاری وجود دارد، یکی کافی است (OR)
                hasAccess = optionalPermissions.Any(p => userPermissions.Contains(p));
            }
            else
            {
                hasAccess = true;
            }

            // ذخیره در Cache
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES));

            _cache.Set(cacheKey, hasAccess, cacheOptions);

            return hasAccess;
        }

        /// <summary>
        /// دریافت لیست منوهای قابل دسترس کاربر
        /// </summary>
        public async Task<List<long>> GetUserAccessibleMenuIdsAsync(long userId, CancellationToken ct = default)
        {
            _logger.LogDebug("GetUserAccessibleMenuIdsAsync called for user {UserId}", userId);

            // کاربر Admin به همه منوها دسترسی دارد
            if (await IsUserAdminAsync(userId, ct))
            {
                var allMenuIds = await _context.tblMenus
                    .Select(m => m.Id)
                    .ToListAsync(ct);

                _logger.LogDebug("Admin user {UserId} has access to all {Count} menus", userId, allMenuIds.Count);
                return allMenuIds;
            }

            // بررسی Cache
            var cacheKey = $"user_accessible_menus_{userId}";
            if (_cache.TryGetValue<List<long>>(cacheKey, out var cachedMenuIds))
            {
                _logger.LogDebug("User {UserId} accessible menus from cache: {Count}", userId, cachedMenuIds?.Count ?? 0);
                return cachedMenuIds ?? new List<long>();
            }

            // دریافت Permission های کاربر
            var userPermissions = await GetUserPermissionsAsync(userId, ct);
            _logger.LogDebug("User {UserId} has {Count} permissions", userId, userPermissions.Count);

            // دریافت تمام منوها
            var allMenus = await _context.tblMenus
                .Include(m => m.MenuPermissions)
                    .ThenInclude(mp => mp.tblPermission)
                .ToListAsync(ct);

            _logger.LogDebug("Total menus in database: {Count}", allMenus.Count);

            var accessibleMenuIds = new List<long>();

            foreach (var menu in allMenus)
            {
                // اگر منو Permission نیاز ندارد، همه می‌توانند ببینند
                if (!menu.MenuPermissions.Any())
                {
                    accessibleMenuIds.Add(menu.Id);
                    continue;
                }

                var requiredPermissions = menu.MenuPermissions.Where(mp => mp.IsRequired).Select(mp => mp.tblPermission.Name).ToList();
                var optionalPermissions = menu.MenuPermissions.Where(mp => !mp.IsRequired).Select(mp => mp.tblPermission.Name).ToList();

                bool hasAccess;

                if (requiredPermissions.Any())
                {
                    // AND logic
                    hasAccess = requiredPermissions.All(p => userPermissions.Contains(p));
                }
                else if (optionalPermissions.Any())
                {
                    // OR logic
                    hasAccess = optionalPermissions.Any(p => userPermissions.Contains(p));
                }
                else
                {
                    hasAccess = true;
                }

                if (hasAccess)
                {
                    accessibleMenuIds.Add(menu.Id);
                }
            }

            // ذخیره در Cache
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES * 2));

            _cache.Set(cacheKey, accessibleMenuIds, cacheOptions);

            _logger.LogDebug("User {UserId} has access to {Count} menus", userId, accessibleMenuIds.Count);

            return accessibleMenuIds;
        }

        public async Task<bool> HasAllPermissionsAsync(long userId, IEnumerable<string> permissions, CancellationToken ct = default)
        {
            var requestedPermissions = permissions?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (!requestedPermissions.Any())
                return true;

            try
            {
                foreach (var permission in requestedPermissions)
                {
                    if (!await IsPermissionAllowedByPolicyAsync(userId, permission, ct))
                    {
                        return false;
                    }
                }

                // کاربر Admin به همه چیز دسترسی دارد
                if (await IsUserAdminAsync(userId, ct))
                    return true;

                var userPermissions = await GetUserPermissionsAsync(userId, ct);
                var userPermissionSet = new HashSet<string>(userPermissions, StringComparer.OrdinalIgnoreCase);
                return requestedPermissions.All(userPermissionSet.Contains);
            }
            catch (Exception ex)
            {
                // ============================================
                // FPT_FLS.1.1 (الزام 46): Fail-Secure
                // ============================================
                _logger.LogError(ex,
                    "FAIL-SECURE: HasAllPermissions check failed for user {UserId}. ACCESS DENIED.",
                    userId);

                try
                {
                    await _auditLogService.LogEventAsync(
                        eventType: "FailSecureActivated",
                        entityType: "Permission",
                        entityId: string.Join(",", requestedPermissions),
                        isSuccess: false,
                        errorMessage: $"FAIL-SECURE: HasAllPermissions check failed - {ex.GetType().Name}",
                        userId: (int)userId,
                        description: $"FAIL-SECURE: Access denied for user {userId} due to system failure",
                        ct: default);
                }
                catch (Exception dbEx)
                {
                    // دیتابیس قطع است - ذخیره در فایل
                    _logger.LogWarning(dbEx, "Database unavailable. Logging failure to file.");
                    await _failSecureService.LogFailureToFileAsync(
                        failureType: ex.GetType().Name,
                        operationName: "HasAllPermissionsAsync",
                        details: $"User: {userId}, Permissions: {string.Join(",", requestedPermissions)}, Error: {ex.Message}");
                }

                return _failSecureSettings.DefaultAccessOnFailure;
            }
        }

        public async Task<bool> HasAnyPermissionAsync(long userId, IEnumerable<string> permissions, CancellationToken ct = default)
        {
            var requestedPermissions = permissions?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (!requestedPermissions.Any())
                return false;

            try
            {
                var policyAllowedPermissions = new List<string>();
                foreach (var permission in requestedPermissions)
                {
                    if (await IsPermissionAllowedByPolicyAsync(userId, permission, ct))
                    {
                        policyAllowedPermissions.Add(permission);
                    }
                }

                if (!policyAllowedPermissions.Any())
                {
                    return false;
                }

                // کاربر Admin به همه چیز دسترسی دارد
                if (await IsUserAdminAsync(userId, ct))
                    return true;

                var userPermissions = await GetUserPermissionsAsync(userId, ct);
                var userPermissionSet = new HashSet<string>(userPermissions, StringComparer.OrdinalIgnoreCase);
                return policyAllowedPermissions.Any(userPermissionSet.Contains);
            }
            catch (Exception ex)
            {
                // ============================================
                // FPT_FLS.1.1 (الزام 46): Fail-Secure
                // ============================================
                _logger.LogError(ex,
                    "FAIL-SECURE: HasAnyPermission check failed for user {UserId}. ACCESS DENIED.",
                    userId);

                try
                {
                    await _auditLogService.LogEventAsync(
                        eventType: "FailSecureActivated",
                        entityType: "Permission",
                        entityId: string.Join(",", requestedPermissions),
                        isSuccess: false,
                        errorMessage: $"FAIL-SECURE: HasAnyPermission check failed - {ex.GetType().Name}",
                        userId: (int)userId,
                        description: $"FAIL-SECURE: Access denied for user {userId} due to system failure",
                        ct: default);
                }
                catch (Exception dbEx)
                {
                    // دیتابیس قطع است - ذخیره در فایل
                    _logger.LogWarning(dbEx, "Database unavailable. Logging failure to file.");
                    await _failSecureService.LogFailureToFileAsync(
                        failureType: ex.GetType().Name,
                        operationName: "HasAnyPermissionAsync",
                        details: $"User: {userId}, Permissions: {string.Join(",", requestedPermissions)}, Error: {ex.Message}");
                }

                return _failSecureSettings.DefaultAccessOnFailure;
            }
        }

        public async Task GrantPermissionToGroupAsync(long groupId, long permissionId, long grantedBy, CancellationToken ct = default)
        {
            // بررسی وجود Permission و Group
            var permission = await _context.tblPermissions.FindAsync(new object[] { permissionId }, ct);
            var group = await _context.tblGrps.FindAsync(new object[] { groupId }, ct);

            if (permission == null || group == null)
            {
                _logger.LogWarning("Cannot grant permission {PermissionId} to group {GroupId} - not found", 
                    permissionId, groupId);
                return;
            }

            // بررسی وجود رابطه قبلی
            var existing = await _context.tblGrpPermissions
                .FirstOrDefaultAsync(gp => gp.tblGrpId == groupId && gp.tblPermissionId == permissionId, ct);

            if (existing != null)
            {
                if (!existing.IsGranted)
                {
                    existing.IsGranted = true;
                    existing.GrantedAt = DateTime.UtcNow;
                    existing.GrantedBy = grantedBy;
                }
            }
            else
            {
                var groupPermission = new tblGrpPermission
                {
                    tblGrpId = groupId,
                    tblPermissionId = permissionId,
                    IsGranted = true,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = grantedBy,
                    TblUserGrpIdInsert = grantedBy
                };
                groupPermission.SetZamanInsert(DateTime.UtcNow);

                _context.tblGrpPermissions.Add(groupPermission);
            }

            await _context.SaveChangesAsync(ct);

            // پاکسازی Cache
            await InvalidateGroupPermissionsCacheAsync(groupId);

            // ثبت در Audit Log
            await _auditLogService.LogEventAsync(
                eventType: "GrantPermission",
                entityType: "Permission",
                entityId: permissionId.ToString(),
                isSuccess: true,
                description: $"Permission {permission.Name} granted to group {group.Title}",
                userId: grantedBy,
                ct: ct);

            _logger.LogInformation("Permission {PermissionName} granted to group {GroupTitle} by user {GrantedBy}", 
                permission.Name, group.Title, grantedBy);
        }

        public async Task RevokePermissionFromGroupAsync(long groupId, long permissionId, CancellationToken ct = default)
        {
            var groupPermission = await _context.tblGrpPermissions
                .FirstOrDefaultAsync(gp => gp.tblGrpId == groupId && gp.tblPermissionId == permissionId, ct);

            if (groupPermission != null)
            {
                _context.tblGrpPermissions.Remove(groupPermission);
                await _context.SaveChangesAsync(ct);

                // پاکسازی Cache
                await InvalidateGroupPermissionsCacheAsync(groupId);

                // ثبت در Audit Log
                await _auditLogService.LogEventAsync(
                    eventType: "RevokePermission",
                    entityType: "Permission",
                    entityId: permissionId.ToString(),
                    isSuccess: true,
                    description: $"Permission {permissionId} revoked from group {groupId}",
                    ct: ct);

                _logger.LogInformation("Permission {PermissionId} revoked from group {GroupId}", 
                    permissionId, groupId);
            }
        }

        /// <summary>
        /// اختصاص Permission به منو
        /// </summary>
        public async Task AssignPermissionToMenuAsync(long menuId, long permissionId, bool isRequired, long assignedBy, CancellationToken ct = default)
        {
            // بررسی وجود Permission و Menu
            var permission = await _context.tblPermissions.FindAsync(new object[] { permissionId }, ct);
            var menu = await _context.tblMenus.FindAsync(new object[] { menuId }, ct);

            if (permission == null || menu == null)
            {
                _logger.LogWarning("Cannot assign permission {PermissionId} to menu {MenuId} - not found", 
                    permissionId, menuId);
                return;
            }

            // بررسی وجود رابطه قبلی
            var existing = await _context.tblMenuPermissions
                .FirstOrDefaultAsync(mp => mp.tblMenuId == menuId && mp.tblPermissionId == permissionId, ct);

            if (existing != null)
            {
                existing.IsRequired = isRequired;
            }
            else
            {
                var menuPermission = new tblMenuPermission
                {
                    tblMenuId = menuId,
                    tblPermissionId = permissionId,
                    IsRequired = isRequired,
                    TblUserGrpIdInsert = assignedBy
                };
                menuPermission.SetZamanInsert(DateTime.UtcNow);

                _context.tblMenuPermissions.Add(menuPermission);
            }

            await _context.SaveChangesAsync(ct);

            // ثبت در Audit Log
            await _auditLogService.LogEventAsync(
                eventType: "AssignPermissionToMenu",
                entityType: "MenuPermission",
                entityId: $"{menuId}_{permissionId}",
                isSuccess: true,
                description: $"Permission {permission.Name} assigned to menu {menu.Title} (IsRequired: {isRequired})",
                userId: assignedBy,
                ct: ct);

            _logger.LogInformation("Permission {PermissionName} assigned to menu {MenuTitle} (IsRequired: {IsRequired}) by user {AssignedBy}", 
                permission.Name, menu.Title, isRequired, assignedBy);
        }

        /// <summary>
        /// حذف Permission از منو
        /// </summary>
        public async Task RemovePermissionFromMenuAsync(long menuId, long permissionId, CancellationToken ct = default)
        {
            var menuPermission = await _context.tblMenuPermissions
                .FirstOrDefaultAsync(mp => mp.tblMenuId == menuId && mp.tblPermissionId == permissionId, ct);

            if (menuPermission != null)
            {
                _context.tblMenuPermissions.Remove(menuPermission);
                await _context.SaveChangesAsync(ct);

                // ثبت در Audit Log
                await _auditLogService.LogEventAsync(
                    eventType: "RemovePermissionFromMenu",
                    entityType: "MenuPermission",
                    entityId: $"{menuId}_{permissionId}",
                    isSuccess: true,
                    description: $"Permission {permissionId} removed from menu {menuId}",
                    ct: ct);

                _logger.LogInformation("Permission {PermissionId} removed from menu {MenuId}", 
                    permissionId, menuId);
            }
        }

        public async Task InvalidateUserPermissionsCacheAsync(long userId)
        {
            _cache.Remove($"user_permissions_{userId}");
            _cache.Remove($"user_accessible_menus_{userId}");
            _cache.Remove($"user_is_admin_{userId}");
            
            await Task.CompletedTask;
        }

        public async Task InvalidateGroupPermissionsCacheAsync(long groupId)
        {
            // دریافت کاربران این گروه
            var userIds = await _context.tblUserGrps
                .Where(ug => ug.tblGrpId == groupId)
                .Select(ug => ug.tblUserId)
                .ToListAsync();

            // پاکسازی cache برای همه کاربران گروه
            foreach (var userId in userIds)
            {
                await InvalidateUserPermissionsCacheAsync(userId);
            }

            _logger.LogDebug("Invalidated permission cache for {Count} users in group {GroupId}", 
                userIds.Count, groupId);
        }
    }
}
