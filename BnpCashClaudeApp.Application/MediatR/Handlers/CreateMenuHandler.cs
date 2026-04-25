using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.MediatR.Commands;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Handlers
{
    /// <summary>
    /// Handler برای ایجاد منوی جدید
    /// ============================================
    /// بعد از ایجاد منو:
    /// 1. Permission مناسب را پیدا می‌کند
    /// 2. آن را به منو اختصاص می‌دهد
    /// 3. گروه Admin به صورت پیش‌فرض دسترسی می‌گیرد
    /// ============================================
    /// </summary>
    public class CreateMenuHandler : IRequestHandler<CreateMenuCommand, Guid>
    {
        private readonly IRepository<tblMenu> _repository;
        private readonly IRepository<tblPermission> _permissionRepository;
        private readonly IRepository<tblMenuPermission> _menuPermissionRepository;
        private readonly IRepository<tblGrp> _grpRepository;
        private readonly IRepository<tblGrpPermission> _grpPermissionRepository;

        public CreateMenuHandler(
            IRepository<tblMenu> repository,
            IRepository<tblPermission> permissionRepository,
            IRepository<tblMenuPermission> menuPermissionRepository,
            IRepository<tblGrp> grpRepository,
            IRepository<tblGrpPermission> grpPermissionRepository)
        {
            _repository = repository;
            _permissionRepository = permissionRepository;
            _menuPermissionRepository = menuPermissionRepository;
            _grpRepository = grpRepository;
            _grpPermissionRepository = grpPermissionRepository;
        }

        public async Task<Guid> Handle(CreateMenuCommand request, CancellationToken cancellationToken)
        {
            // تبدیل ParentPublicId به ParentId (Id داخلی)
            long? parentId = null;
            if (request.ParentPublicId.HasValue)
            {
                var parentMenu = await _repository.GetByPublicIdAsync(request.ParentPublicId.Value);
                parentId = parentMenu?.Id;
            }

            var menu = new tblMenu
            {
                Title = request.Title,
                Path = request.Path,
                ParentId = parentId,
                TblUserGrpIdInsert = request.TblUserGrpIdInsert,
                tblSoftwareId = request.tblSoftwareId
            };
            // تنظیم تاریخ به شمسی
            menu.SetZamanInsert(DateTime.Now);

            var result = await _repository.AddAsync(menu);

            // ============================================
            // اختصاص Permission به منوی جدید
            // ============================================
            await AssignPermissionToMenuAsync(result, request.TblUserGrpIdInsert);

            return result.PublicId;
        }

        /// <summary>
        /// اختصاص Permission مناسب به منو و اعطای دسترسی به گروه Admin
        /// </summary>
        private async Task AssignPermissionToMenuAsync(tblMenu menu, long createdBy)
        {
            // دریافت تمام Permission ها
            var allPermissions = await _permissionRepository.GetAllAsync();
            var permissions = allPermissions.ToList();

            // پیدا کردن Permission مناسب برای این منو
            var matchedPermission = FindMatchingPermission(menu, permissions);

            if (matchedPermission != null)
            {
                // ایجاد رابطه منو-Permission
                var menuPermission = new tblMenuPermission
                {
                    tblMenuId = menu.Id,
                    tblPermissionId = matchedPermission.Id,
                    IsRequired = true,
                    TblUserGrpIdInsert = createdBy
                };
                menuPermission.SetZamanInsert(DateTime.Now);

                await _menuPermissionRepository.AddAsync(menuPermission);

                // اعطای Permission به گروه Admin
                await GrantPermissionToAdminGroupAsync(matchedPermission.Id, createdBy);
            }
        }

        /// <summary>
        /// اعطای Permission به گروه Admin
        /// </summary>
        private async Task GrantPermissionToAdminGroupAsync(long permissionId, long grantedBy)
        {
            // پیدا کردن گروه Admin
            var allGroups = await _grpRepository.GetAllAsync();
            var adminGroup = allGroups.FirstOrDefault(g => 
                g.Title.ToLower() == "admin" || g.Title == "مدیران سیستم");

            if (adminGroup == null) return;

            // بررسی اینکه آیا قبلاً این Permission به Admin داده شده
            var existingPermissions = await _grpPermissionRepository.GetAllAsync();
            var alreadyGranted = existingPermissions.Any(gp => 
                gp.tblGrpId == adminGroup.Id && gp.tblPermissionId == permissionId);

            if (!alreadyGranted)
            {
                var grpPermission = new tblGrpPermission
                {
                    tblGrpId = adminGroup.Id,
                    tblPermissionId = permissionId,
                    IsGranted = true,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = grantedBy,
                    TblUserGrpIdInsert = grantedBy
                };
                grpPermission.SetZamanInsert(DateTime.Now);

                await _grpPermissionRepository.AddAsync(grpPermission);
            }
        }

        /// <summary>
        /// پیدا کردن Permission مناسب برای منو بر اساس Path و Title
        /// </summary>
        private tblPermission? FindMatchingPermission(tblMenu menu, List<tblPermission> permissions)
        {
            var path = (menu.Path ?? "").ToLower();
            var title = (menu.Title ?? "").ToLower();

            // Authentication - بدون Permission خاص (عمومی)
            if (path.Contains("auth"))
                return null;

            // Security - باید قبل از Users بررسی شود
            if (path.Contains("security/passwordpolicy") || title == "سیاست رمز عبور")
                return permissions.FirstOrDefault(p => p.Name == "Security.PasswordPolicy");
            if (path.Contains("security/lockoutpolicy") || title == "تنظیمات قفل حساب")
                return permissions.FirstOrDefault(p => p.Name == "Security.LockoutPolicy");
            if (path.Contains("security/terminatesessions") || title == "بستن نشست‌های کاربر")
                return permissions.FirstOrDefault(p => p.Name == "Security.TerminateSessions");
            if (path.Contains("security/userssecuritystatus") || title == "وضعیت امنیتی کاربران")
                return permissions.FirstOrDefault(p => p.Name == "Security.Read");
            if (path.Contains("security") || title.Contains("امنیت"))
                return permissions.FirstOrDefault(p => p.Name == "Security.Read");

            // AuditLog - باید قبل از Users بررسی شود
            if (path.Contains("auditlog/search") || title == "جستجوی لاگ")
                return permissions.FirstOrDefault(p => p.Name == "AuditLog.Search");
            if (path.Contains("auditlog/statistics") || title == "آمار امنیتی")
                return permissions.FirstOrDefault(p => p.Name == "AuditLog.Statistics");
            if (path.Contains("auditlog/user") || title == "لاگ‌های کاربر")
                return permissions.FirstOrDefault(p => p.Name == "AuditLog.Read");
            if (path.Contains("auditlog") || title.Contains("لاگ"))
                return permissions.FirstOrDefault(p => p.Name == "AuditLog.Read");

            // Sessions
            if (path.Contains("session") || title.Contains("نشست"))
                return permissions.FirstOrDefault(p => p.Name == "Sessions.Read");

            // Users
            if (path.Contains("users/create") || title == "تعریف کاربر جدید")
                return permissions.FirstOrDefault(p => p.Name == "Users.Create");
            if (path.Contains("users/activate") || title == "فعال/غیرفعال کردن کاربر")
                return permissions.FirstOrDefault(p => p.Name == "Users.Activate");
            if (path.Contains("users/resetpassword") || title == "ریست رمز عبور")
                return permissions.FirstOrDefault(p => p.Name == "Users.ResetPassword");
            if (path.Contains("users/unlock") || title == "باز کردن قفل کاربر")
                return permissions.FirstOrDefault(p => p.Name == "Users.Unlock");
            if (path.Contains("users/lockoutstatus") || title == "وضعیت قفل کاربر")
                return permissions.FirstOrDefault(p => p.Name == "Users.Read");
            if (path.Contains("users") || title.Contains("کاربر"))
                return permissions.FirstOrDefault(p => p.Name == "Users.Read");

            // Groups
            if (path.Contains("grp/create") || title == "تعریف گروه جدید")
                return permissions.FirstOrDefault(p => p.Name == "Groups.Create");
            if (path.Contains("grp/edit") || title == "ویرایش گروه")
                return permissions.FirstOrDefault(p => p.Name == "Groups.Update");
            if (path.Contains("grp") || path.Contains("group") || title.Contains("گروه"))
                return permissions.FirstOrDefault(p => p.Name == "Groups.Read");

            // Menus
            if (path.Contains("menu/create") || title == "تعریف منوی جدید")
                return permissions.FirstOrDefault(p => p.Name == "Menus.Create");
            if (path.Contains("menu/edit") || title == "ویرایش منو")
                return permissions.FirstOrDefault(p => p.Name == "Menus.Update");
            if (path.Contains("menu") || title.Contains("منو"))
                return permissions.FirstOrDefault(p => p.Name == "Menus.Read");

            // Permissions
            if (path.Contains("permission") || title.Contains("دسترسی"))
                return permissions.FirstOrDefault(p => p.Name == "Permissions.Read");

            // پیش‌فرض: Menus.Read برای منوهای بدون نگاشت صریح
            return permissions.FirstOrDefault(p => p.Name == "Menus.Read");
        }
    }
}

