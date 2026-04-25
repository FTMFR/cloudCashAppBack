using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace BnpCashClaudeApp.Application.MediatR.Queries
{
        public class GetAllMenusQuery : IRequest<List<MenuDto>>
        {
        }

        public class GetAllMenusQueryHandler : IRequestHandler<GetAllMenusQuery, List<MenuDto>>
        {
            private readonly IRepository<tblMenu> _repository;

            public GetAllMenusQueryHandler(IRepository<tblMenu> repository)
            {
                _repository = repository;
            }

        public async Task<List<MenuDto>> Handle(GetAllMenusQuery request, CancellationToken cancellationToken)
        {
            var menus = await _repository.GetAllAsync();
            var menuList = menus.ToList();

            var rootMenus = menuList
                .Where(x => x.ParentId == null)
                .Select(x => MapToDto(x, menuList))
                .ToList();

            return rootMenus;
        }


        private MenuDto MapToDto(tblMenu menu, List<tblMenu> allMenus)
        {
            Guid? parentPublicId = null;

            if (menu.ParentId.HasValue)
            {
                var parent = allMenus.FirstOrDefault(p => p.Id == menu.ParentId.Value);
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
                Icon = menu.Icon,
                // اطلاعات نرم‌افزار
                tblSoftwareId = menu.tblSoftwareId,
                SoftwareName = menu.Software?.Name,
                SoftwareCode = menu.Software?.Code,
                Children = new List<MenuDto>()
            };

            var children = allMenus
                .Where(x => x.ParentId == menu.Id)
                .Select(x => MapToDto(x, allMenus))
                .ToList();

            dto.Children = children;

            return dto;
        }

    }
}
