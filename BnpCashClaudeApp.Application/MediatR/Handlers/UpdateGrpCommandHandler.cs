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
    public class UpdateGrpCommandHandler : IRequestHandler<UpdateGrpCommand, bool>
    {
        private readonly IRepository<tblGrp> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;
        private readonly IAuditLogService _auditLogService;

        public UpdateGrpCommandHandler(
            IRepository<tblGrp> repository,
            IRepository<tblShobe> shobeRepository,
            IAuditLogService auditLogService)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
            _auditLogService = auditLogService;
        }

        public async Task<bool> Handle(UpdateGrpCommand request, CancellationToken cancellationToken)
        {
            var group = await _repository.GetByPublicIdAsync(request.PublicId);
            if (group == null) return false;

            var oldShobeId = group.tblShobeId;
            var oldParentId = group.ParentId;
            var changes = new Dictionary<string, (object? oldValue, object? newValue)>
            {
                { "Title", (group.Title, request.Title) },
                { "Description", (group.Description ?? "", request.Description ?? "") },
                { "tblShobeId", (oldShobeId?.ToString() ?? "", "") },
                { "ParentId", (oldParentId?.ToString() ?? "", "") }
            };

            if (request.ParentPublicId!=null)
            {
                var parent = await _repository.GetByPublicIdAsync(request.ParentPublicId);
                if (parent == null)
                {
                    throw new ArgumentException("گروه والد یافت نشد.");
                }
                if (parent.Id == group.Id)
                {
                    throw new ArgumentException("نمی‌تواند والد خودش باشد.");
                }
                group.ParentId = parent.Id;
            }
            else
            {
                throw new ArgumentException("گروه والد یافت نشد.");
            }

            // پیدا کردن شعبه بر اساس PublicId
            if (request.ShobePublicId.HasValue)
            {
                var shobe = await _shobeRepository.GetByPublicIdAsync(request.ShobePublicId.Value);
                if (shobe == null)
                {
                    throw new ArgumentException("شعبه یافت نشد.");
                }
                group.tblShobeId = shobe.Id;
            }
            else
            {
                group.tblShobeId = null;
            }
            changes["tblShobeId"] = (oldShobeId?.ToString() ?? "", group.tblShobeId?.ToString() ?? "");
            changes["ParentId"] = (oldParentId?.ToString() ?? "", group.ParentId?.ToString() ?? "");

            group.Title = request.Title;
            group.Description = request.Description;
            group.SetZamanLastEdit(DateTime.Now);
            group.TblUserGrpIdLastEdit = request.TblUserGrpIdLastEdit;

            await _repository.UpdateAsync(group);

            await _auditLogService.LogEntityChangeAsync(
                eventType: "Update",
                entityType: "Grp",
                entityId: request.PublicId.ToString(),
                changes: changes,
                userId: request.AuditUserId,
                ct: cancellationToken);

            return true;
        }
    }
}
