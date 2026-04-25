using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس مدیریت Permission ها
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF از استاندارد ISO 15408
    /// بررسی و مدیریت دسترسی‌های کاربران
    /// ============================================
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// بررسی دسترسی کاربر به یک Permission خاص
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="permission">نام Permission (مثال: Users.Create)</param>
        /// <param name="ct">Cancellation Token</param>
        /// <returns>true اگر دسترسی دارد</returns>
        Task<bool> HasPermissionAsync(long userId, string permission, CancellationToken ct = default);

        /// <summary>
        /// بررسی دسترسی کاربر به Resource و Action
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="resource">نام Resource (مثال: Users)</param>
        /// <param name="action">نوع Action (مثال: Create)</param>
        /// <param name="ct">Cancellation Token</param>
        /// <returns>true اگر دسترسی دارد</returns>
        Task<bool> HasPermissionAsync(long userId, string resource, string action, CancellationToken ct = default);

        /// <summary>
        /// دریافت لیست تمام Permission های کاربر
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="ct">Cancellation Token</param>
        /// <returns>لیست نام Permission ها</returns>
        Task<List<string>> GetUserPermissionsAsync(long userId, CancellationToken ct = default);

        /// <summary>
        /// دریافت Permission های کاربر به تفکیک Resource
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="ct">Cancellation Token</param>
        /// <returns>Dictionary از Resource به لیست Actions</returns>
        Task<Dictionary<string, List<string>>> GetUserPermissionsByResourceAsync(long userId, CancellationToken ct = default);

        /// <summary>
        /// بررسی دسترسی کاربر به یک منو
        /// بر اساس Permission های مورد نیاز منو
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="menuId">شناسه منو</param>
        /// <param name="ct">Cancellation Token</param>
        /// <returns>true اگر دسترسی دارد</returns>
        Task<bool> HasMenuAccessAsync(long userId, long menuId, CancellationToken ct = default);

        /// <summary>
        /// دریافت لیست منوهای قابل دسترس کاربر
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="ct">Cancellation Token</param>
        /// <returns>لیست شناسه منوها</returns>
        Task<List<long>> GetUserAccessibleMenuIdsAsync(long userId, CancellationToken ct = default);

        /// <summary>
        /// بررسی دسترسی کاربر به چندین Permission همزمان (AND logic)
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="permissions">لیست Permission ها</param>
        /// <param name="ct">Cancellation Token</param>
        /// <returns>true اگر به همه Permission ها دسترسی دارد</returns>
        Task<bool> HasAllPermissionsAsync(long userId, IEnumerable<string> permissions, CancellationToken ct = default);

        /// <summary>
        /// بررسی دسترسی کاربر به حداقل یکی از Permission ها (OR logic)
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="permissions">لیست Permission ها</param>
        /// <param name="ct">Cancellation Token</param>
        /// <returns>true اگر به حداقل یکی دسترسی دارد</returns>
        Task<bool> HasAnyPermissionAsync(long userId, IEnumerable<string> permissions, CancellationToken ct = default);

        /// <summary>
        /// اعطای Permission به یک گروه
        /// </summary>
        /// <param name="groupId">شناسه گروه</param>
        /// <param name="permissionId">شناسه Permission</param>
        /// <param name="grantedBy">شناسه کاربر اعطا کننده</param>
        /// <param name="ct">Cancellation Token</param>
        Task GrantPermissionToGroupAsync(long groupId, long permissionId, long grantedBy, CancellationToken ct = default);

        /// <summary>
        /// حذف Permission از یک گروه
        /// </summary>
        /// <param name="groupId">شناسه گروه</param>
        /// <param name="permissionId">شناسه Permission</param>
        /// <param name="ct">Cancellation Token</param>
        Task RevokePermissionFromGroupAsync(long groupId, long permissionId, CancellationToken ct = default);

        /// <summary>
        /// اختصاص Permission به منو
        /// </summary>
        /// <param name="menuId">شناسه منو</param>
        /// <param name="permissionId">شناسه Permission</param>
        /// <param name="isRequired">آیا الزامی است (AND) یا اختیاری (OR)</param>
        /// <param name="assignedBy">شناسه کاربر اختصاص دهنده</param>
        /// <param name="ct">Cancellation Token</param>
        Task AssignPermissionToMenuAsync(long menuId, long permissionId, bool isRequired, long assignedBy, CancellationToken ct = default);

        /// <summary>
        /// حذف Permission از منو
        /// </summary>
        /// <param name="menuId">شناسه منو</param>
        /// <param name="permissionId">شناسه Permission</param>
        /// <param name="ct">Cancellation Token</param>
        Task RemovePermissionFromMenuAsync(long menuId, long permissionId, CancellationToken ct = default);

        /// <summary>
        /// پاکسازی Cache دسترسی‌های کاربر
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        Task InvalidateUserPermissionsCacheAsync(long userId);

        /// <summary>
        /// پاکسازی Cache دسترسی‌های گروه
        /// </summary>
        /// <param name="groupId">شناسه گروه</param>
        Task InvalidateGroupPermissionsCacheAsync(long groupId);
    }
}
