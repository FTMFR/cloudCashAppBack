using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.MediatR.Commands;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace BnpCashClaudeApp.Application.MediatR.Handlers
{
    public class UpdateMenuCommandHandler : IRequestHandler<UpdateMenuCommand, bool>
    {
        private readonly IRepository<tblMenu> _repository;
        private readonly IAuditLogService _auditLogService;

        public UpdateMenuCommandHandler(IRepository<tblMenu> repository, IAuditLogService auditLogService)
        {
            _repository = repository;
            _auditLogService = auditLogService;
        }

        public async Task<bool> Handle(UpdateMenuCommand request, CancellationToken cancellationToken)
        {
            var menu = await _repository.GetByPublicIdAsync(request.PublicId);
            if (menu == null) return false;

            var oldParentId = menu.ParentId;
            var changes = new Dictionary<string, (object? oldValue, object? newValue)>
            {
                { "Title", (menu.Title, request.Title) },
                { "Path", (menu.Path ?? "", request.Path ?? "") },
                { "ParentId", (oldParentId?.ToString() ?? "", "") },
                { "tblSoftwareId", (menu.tblSoftwareId?.ToString() ?? "", request.tblSoftwareId?.ToString() ?? "") }
            };

            menu.Title = request.Title;
            menu.Path = request.Path;
            menu.tblSoftwareId = request.tblSoftwareId;
            
            // تبدیل ParentPublicId به ParentId (Id داخلی)
            if (request.ParentPublicId.HasValue)
            {
                var parentMenu = await _repository.GetByPublicIdAsync(request.ParentPublicId.Value);
                menu.ParentId = parentMenu?.Id;
            }
            else
            {
                menu.ParentId = null;
            }
            changes["ParentId"] = (oldParentId?.ToString() ?? "", menu.ParentId?.ToString() ?? "");

            menu.SetZamanLastEdit(DateTime.Now);
            menu.TblUserGrpIdLastEdit = request.TblUserGrpIdLastEdit;

            await _repository.UpdateAsync(menu);

            await _auditLogService.LogEntityChangeAsync(
                eventType: "Update",
                entityType: "Menu",
                entityId: request.PublicId.ToString(),
                changes: changes,
                userId: request.AuditUserId,
                ct: cancellationToken);

            return true;
        }
    }
}
