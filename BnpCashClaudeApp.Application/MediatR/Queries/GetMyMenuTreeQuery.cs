using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Queries
{
    /// <summary>
    /// Query برای دریافت منوهای قابل دسترس کاربر به صورت درختی
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF از استاندارد ISO 15408
    /// فقط منوهایی که کاربر به آنها دسترسی دارد برگردانده می‌شوند
    /// ============================================
    /// </summary>
    public class GetMyMenuTreeQuery : IRequest<List<MenuDto>>
    {
        public long UserId { get; set; }
    }

    public class GetMyMenuTreeQueryHandler : IRequestHandler<GetMyMenuTreeQuery, List<MenuDto>>
    {
        private readonly IRepository<tblMenu> _menuRepository;
        private readonly IPermissionService _permissionService;

        public GetMyMenuTreeQueryHandler(
            IRepository<tblMenu> menuRepository,
            IPermissionService permissionService)
        {
            _menuRepository = menuRepository;
            _permissionService = permissionService;
        }

        public async Task<List<MenuDto>> Handle(GetMyMenuTreeQuery request, CancellationToken ct)
        {
            // دریافت منوهای قابل دسترس کاربر
            var accessibleMenuIds = await _permissionService.GetUserAccessibleMenuIdsAsync(request.UserId, ct);
            var accessibleSet = new HashSet<long>(accessibleMenuIds);

            // دریافت تمام منوها
            var allMenus = await _menuRepository.GetAllAsync();
            var menuList = allMenus.ToList();

            // فیلتر منوهای قابل دسترس
            var accessibleMenus = menuList
                .Where(m => accessibleSet.Contains(m.Id))
                .ToList();

            // پیدا کردن منوهای ریشه (منوهایی که والدشان در لیست قابل دسترس نیست یا null است)
            var rootMenus = accessibleMenus
                .Where(m => m.ParentId == null || !accessibleSet.Contains(m.ParentId.Value))
                .Select(m => MapToDto(m, accessibleMenus, accessibleSet))
                .ToList();

            return rootMenus;
        }

        private MenuDto MapToDto(tblMenu menu, List<tblMenu> accessibleMenus, HashSet<long> accessibleIds)
        {
            // پیدا کردن ParentPublicId
            Guid? parentPublicId = null;
            if (menu.ParentId.HasValue)
            {
                var parent = accessibleMenus.FirstOrDefault(p => p.Id == menu.ParentId.Value);
                parentPublicId = parent?.PublicId;
            }

            var dto = new MenuDto
            {
                PublicId = menu.PublicId,
                Title = menu.Title,
                Path = menu.Path,
                ParentPublicId = parentPublicId,
                IsMenu = menu.IsMenu,
                ZamanInsert = menu.ZamanInsert,
                Icon=menu.Icon
            };

            // فقط فرزندانی که کاربر به آنها دسترسی دارد
            var children = accessibleMenus
                .Where(m => m.ParentId == menu.Id && accessibleIds.Contains(m.Id))
                .Select(m => MapToDto(m, accessibleMenus, accessibleIds))
                .ToList();

            dto.Children = children;
            return dto;
        }
    }
}

