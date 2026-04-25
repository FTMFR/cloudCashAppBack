using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.MediatR.Commands;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Handlers
{
    public class UpdateShobeCommandHandler : IRequestHandler<UpdateShobeCommand, bool>
    {
        private readonly IRepository<tblShobe> _repository;
        private readonly IAuditLogService _auditLogService;

        public UpdateShobeCommandHandler(IRepository<tblShobe> repository, IAuditLogService auditLogService)
        {
            _repository = repository;
            _auditLogService = auditLogService;
        }

        public async Task<bool> Handle(UpdateShobeCommand request, CancellationToken cancellationToken)
        {
            var shobe = await _repository.GetByPublicIdAsync(request.PublicId);
            if (shobe == null)
                return false;

            var oldParentId = shobe.ParentId;
            var changes = new Dictionary<string, (object? oldValue, object? newValue)>
            {
                { "Title", (shobe.Title, request.Title) },
                { "ShobeCode", (shobe.ShobeCode, request.ShobeCode) },
                { "Address", (shobe.Address ?? "", request.Address ?? "") },
                { "Phone", (shobe.Phone ?? "", request.Phone ?? "") },
                { "PostalCode", (shobe.PostalCode ?? "", request.PostalCode ?? "") },
                { "IsActive", (shobe.IsActive, request.IsActive) },
                { "Description", (shobe.Description ?? "", request.Description ?? "") },
                { "DisplayOrder", (shobe.DisplayOrder, request.DisplayOrder) },
                { "ParentId", (oldParentId?.ToString() ?? "", "") }
            };

            // اعتبارسنجی ShobeCode: باید بزرگتر از صفر باشد
            if (request.ShobeCode <= 0)
            {
                throw new ArgumentException("کد شعبه باید بزرگتر از صفر باشد.");
            }

            // اعتبارسنجی یکتایی ShobeCode (به جز شعبه فعلی)
            var allShobes = await _repository.GetAllAsync();
            if (allShobes.Any(s => s.ShobeCode == request.ShobeCode && s.Id != shobe.Id))
            {
                throw new ArgumentException($"کد شعبه '{request.ShobeCode}' قبلاً برای شعبه دیگری ثبت شده است.");
            }

            // اگر ParentPublicId مشخص شده باشد، باید Parent را پیدا کنیم
            if (request.ParentPublicId.HasValue)
            {
                var parent = await _repository.GetByPublicIdAsync(request.ParentPublicId.Value);
                if (parent == null)
                {
                    throw new ArgumentException("شعبه والد یافت نشد.");
                }
                // جلوگیری از ایجاد حلقه (شعبه نمی‌تواند والد خودش باشد)
                if (parent.Id == shobe.Id)
                {
                    throw new ArgumentException("شعبه نمی‌تواند والد خودش باشد.");
                }
                shobe.ParentId = parent.Id;
            }
            else
            {
                shobe.ParentId = null;
            }
            changes["ParentId"] = (oldParentId?.ToString() ?? "", shobe.ParentId?.ToString() ?? "");

            shobe.Title = request.Title;
            shobe.ShobeCode = request.ShobeCode;
            shobe.Address = request.Address;
            shobe.Phone = request.Phone;
            shobe.PostalCode = request.PostalCode;
            shobe.IsActive = request.IsActive;
            shobe.Description = request.Description;
            shobe.DisplayOrder = request.DisplayOrder;
            shobe.TblUserGrpIdLastEdit = request.TblUserGrpIdLastEdit;

            // تنظیم تاریخ ویرایش به شمسی
            shobe.SetZamanLastEdit(DateTime.Now);

            await _repository.UpdateAsync(shobe);

            await _auditLogService.LogEntityChangeAsync(
                eventType: "Update",
                entityType: "Shobe",
                entityId: request.PublicId.ToString(),
                changes: changes,
                userId: request.AuditUserId,
                ct: cancellationToken);

            return true;
        }
    }
}

