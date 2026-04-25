using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace BnpCashClaudeApp.Application.MediatR.Queries
{
    public class GetMenuByIdQuery : IRequest<MenuDto>
    {
        public Guid PublicId { get; set; }
    }

    public class GetMenuByIdQueryHandler : IRequestHandler<GetMenuByIdQuery, MenuDto>
    {
        private readonly IRepository<tblMenu> _repository;

        public GetMenuByIdQueryHandler(IRepository<tblMenu> repository)
        {
            _repository = repository;
        }

        public async Task<MenuDto> Handle(GetMenuByIdQuery request, CancellationToken cancellationToken)
        {
            var menu = await _repository.GetByPublicIdAsync(request.PublicId);
            if (menu == null) return null;

            // اگر ParentId داریم، باید ParentPublicId را پیدا کنیم
            Guid? parentPublicId = null;
            if (menu.ParentId.HasValue)
            {
                var parentMenu = await _repository.GetByIdAsync(menu.ParentId.Value);
                parentPublicId = parentMenu?.PublicId;
            }

            return new MenuDto
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
                SoftwareCode = menu.Software?.Code
            };
        }
    }
}
