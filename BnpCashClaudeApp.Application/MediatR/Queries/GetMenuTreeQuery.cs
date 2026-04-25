using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace BnpCashClaudeApp.Application.MediatR.Queries
{
    public class GetMenuTreeQuery : IRequest<List<MenuDto>>
    {
    }

    public class GetMenuTreeQueryHandler : IRequestHandler<GetMenuTreeQuery, List<MenuDto>>
    {
        private readonly IRepository<tblMenu> _repository;

        public GetMenuTreeQueryHandler(IRepository<tblMenu> repository)
        {
            _repository = repository;
        }

        public async Task<List<MenuDto>> Handle(GetMenuTreeQuery request, CancellationToken cancellationToken)
        {
            var allMenus = await _repository.GetAllAsync();
            var menuList = allMenus.ToList();

            var rootMenus = menuList.Where(m => m.ParentId == null)
                .Select(m => MapToDto(m, menuList))
                .ToList();

            return rootMenus;
        }

        private MenuDto MapToDto(tblMenu menu, List<tblMenu> allMenus)
        {
            // پیدا کردن ParentPublicId
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
            };

            var children = allMenus.Where(m => m.ParentId == menu.Id)
                .Select(m => MapToDto(m, allMenus))
                .ToList();

            dto.Children = children;
            return dto;
        }
    }
}
