using BnpCashClaudeApp.Application.MediatR.Commands;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace BnpCashClaudeApp.Application.MediatR.Handlers
{
    public class CreateGrpCommandHandler : IRequestHandler<CreateGrpCommand, Guid>
    {
        private readonly IRepository<tblGrp> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;
        private readonly IRepository<tblPermission> _permissionRepository;
        private readonly IRepository<tblGrpPermission> _grpPermissionRepository;

        /// <summary>
        /// نام‌های Permission های پایه احراز هویت و نشست‌ها
        /// این دسترسی‌ها به صورت خودکار به هر گروه جدید اعطا می‌شوند
        /// </summary>
        private static readonly string[] BaseAuthPermissionNames = new[]
        {
            "Auth.Login",
            "Auth.Logout",
            "Auth.LogoutAll",
            "Auth.ChangePassword",
            "Sessions.Read",
            "Sessions.Revoke",
            "Users.ReadById",
            "Users.UploadProfilePicture",
            "Users.DeleteProfilePicture"
        };

        public CreateGrpCommandHandler(
            IRepository<tblGrp> repository,
            IRepository<tblShobe> shobeRepository,
            IRepository<tblPermission> permissionRepository,
            IRepository<tblGrpPermission> grpPermissionRepository)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
            _permissionRepository = permissionRepository;
            _grpPermissionRepository = grpPermissionRepository;
        }

        public async Task<Guid> Handle(CreateGrpCommand request, CancellationToken cancellationToken)
        {
            tblGrp? parent = null;
            if (request.ParentPublicId.HasValue)
            {
                parent = await _repository.GetByPublicIdAsync(request.ParentPublicId.Value);
                if (parent == null)
                {
                    throw new ArgumentException("گروه والد یافت نشد.");
                }
            }

            // پیدا کردن شعبه بر اساس PublicId
            long? shobeId = null;
            if (request.ShobePublicId.HasValue)
            {
                var shobe = await _shobeRepository.GetByPublicIdAsync(request.ShobePublicId.Value);
                if (shobe == null)
                {
                    throw new ArgumentException("شعبه یافت نشد.");
                }
                shobeId = shobe.Id;
            }

            var allgroups= await _repository.GetAllAsync();
            var lastGrpCode = allgroups
                .Where(g=>g.GrpCode.HasValue)
                .Select(u => u.GrpCode.Value)
                .DefaultIfEmpty(0)
                .Max();
            var newGrpCode = lastGrpCode + 1;

            var group = new tblGrp
            {
                Title = request.Title,
                GrpCode = newGrpCode,
                TblUserGrpIdInsert = request.TblUserGrpIdInsert,
                ParentId = parent?.Id,
                Description = request.Description,
                tblShobeId = shobeId // اگر null باشد، گروه برای همه شعبات است
            };
            // تنظیم تاریخ به شمسی
            group.SetZamanInsert(DateTime.Now);

            var result = await _repository.AddAsync(group);

            // ============================================
            // اعطای خودکار دسترسی‌های پایه احراز هویت و Sessions.Read به گروه جدید
            // ============================================
            await AssignBaseAuthPermissionsAsync(result.Id, request.TblUserGrpIdInsert);

            return result.PublicId; // برگرداندن PublicId به جای Id
        }

        /// <summary>
        /// اعطای دسترسی‌های پایه احراز هویت و Sessions.Read به گروه جدید
        /// شامل: Auth.Login, Auth.Logout, Auth.LogoutAll, Auth.ChangePassword, Sessions.Read
        /// </summary>
        private async Task AssignBaseAuthPermissionsAsync(long groupId, long createdByUserGrpId)
        {
            var allPermissions = await _permissionRepository.GetAllAsync();
            var baseAuthPermissions = allPermissions
                .Where(p => BaseAuthPermissionNames.Contains(p.Name))
                .ToList();

            var now = DateTime.Now;

            foreach (var permission in baseAuthPermissions)
            {
                var grpPermission = new tblGrpPermission
                {
                    tblGrpId = groupId,
                    tblPermissionId = permission.Id,
                    IsGranted = true,
                    GrantedAt = now,
                    GrantedBy = createdByUserGrpId,
                    TblUserGrpIdInsert = createdByUserGrpId,
                    Notes = "دسترسی پایه احراز هویت - اعطای خودکار هنگام ساخت گروه"
                };
                grpPermission.SetZamanInsert(now);
                await _grpPermissionRepository.AddAsync(grpPermission);
            }
        }
    }
}
