using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.MediatR.Commands;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Handlers
{
    public class UpdateShobeSettingCommandHandler : IRequestHandler<UpdateShobeSettingCommand, bool>
    {
        private readonly IRepository<tblShobeSetting> _repository;
        private readonly IAuditLogService _auditLogService;

        public UpdateShobeSettingCommandHandler(IRepository<tblShobeSetting> repository, IAuditLogService auditLogService)
        {
            _repository = repository;
            _auditLogService = auditLogService;
        }

        public async Task<bool> Handle(UpdateShobeSettingCommand request, CancellationToken cancellationToken)
        {
            var setting = await _repository.GetByPublicIdAsync(request.PublicId);
            if (setting == null)
                return false;

            if (!setting.IsEditable)
            {
                throw new InvalidOperationException("این تنظیم قابل ویرایش نیست.");
            }

            var changes = new Dictionary<string, (object? oldValue, object? newValue)>
            {
                { "SettingName", (setting.SettingName, request.SettingName) },
                { "Description", (setting.Description ?? "", request.Description ?? "") },
                { "SettingValue", (setting.SettingValue, request.SettingValue) },
                { "SettingType", (((int)setting.SettingType).ToString(), request.SettingType.ToString()) },
                { "IsActive", (setting.IsActive, request.IsActive) },
                { "IsEditable", (setting.IsEditable, request.IsEditable) },
                { "DisplayOrder", (setting.DisplayOrder, request.DisplayOrder) }
            };

            setting.SettingName = request.SettingName;
            setting.Description = request.Description;
            setting.SettingValue = request.SettingValue;
            setting.SettingType = (ShobeSettingType)request.SettingType;
            setting.IsActive = request.IsActive;
            setting.IsEditable = request.IsEditable;
            setting.DisplayOrder = request.DisplayOrder;
            setting.TblUserGrpIdLastEdit = request.TblUserGrpIdLastEdit;

            // تنظیم تاریخ ویرایش به شمسی
            setting.SetZamanLastEdit(DateTime.Now);

            await _repository.UpdateAsync(setting);

            await _auditLogService.LogEntityChangeAsync(
                eventType: "Update",
                entityType: "ShobeSetting",
                entityId: request.PublicId.ToString(),
                changes: changes,
                userId: request.AuditUserId,
                ct: cancellationToken);

            return true;
        }
    }
}
