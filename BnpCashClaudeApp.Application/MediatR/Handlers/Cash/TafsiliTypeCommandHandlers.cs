using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.MediatR.Commands.Cash;
using BnpCashClaudeApp.Domain.Entities.CashSubsystem;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Handlers.Cash
{
    /// <summary>
    /// فیلدهای حساس برای محاسبه IntegrityHash در TafsiliType
    /// </summary>
    internal static class TafsiliTypeSensitiveFields
    {
        public static readonly string[] Fields = new[] { "Title", "CodeTafsiliType", "tblShobeId", "ParentId", "IsActive" };
    }

    /// <summary>
    /// هندلر ایجاد نوع مشتری
    /// </summary>
    public class CreateTafsiliTypeCommandHandler : IRequestHandler<CreateTafsiliTypeCommand, Guid>
    {
        private readonly IRepository<tblTafsiliType> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;
        private readonly IDataIntegrityService _dataIntegrityService;

        public CreateTafsiliTypeCommandHandler(
            IRepository<tblTafsiliType> repository,
            IRepository<tblShobe> shobeRepository,
            IDataIntegrityService dataIntegrityService)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
            _dataIntegrityService = dataIntegrityService;
        }

        public async Task<Guid> Handle(CreateTafsiliTypeCommand request, CancellationToken cancellationToken)
        {
            // پیدا کردن شعبه
            var shobe = await _shobeRepository.GetByPublicIdAsync(request.ShobePublicId);
            if (shobe == null)
            {
                throw new ArgumentException("شعبه یافت نشد.");
            }

            // پیدا کردن والد (اختیاری)
            long? parentId = null;
            if (request.ParentPublicId.HasValue)
            {
                var parent = await _repository.GetByPublicIdAsync(request.ParentPublicId.Value);
                if (parent == null)
                {
                    throw new ArgumentException("نوع مشتری والد یافت نشد.");
                }
                parentId = parent.Id;
            }

            // تولید CodeTafsiliType یکتا در کل سیستم
            var allTypes = await _repository.GetAllAsync();
            var maxCode = allTypes
                .Select(t => t.CodeTafsiliType)
                .DefaultIfEmpty(0)
                .Max();
            var newCode = maxCode + 1;

            var entity = new tblTafsiliType
            {
                tblShobeId = shobe.Id,
                ParentId = parentId,
                Title = request.Title,
                CodeTafsiliType = newCode,
                TblUserGrpIdInsert = request.TblUserGrpIdInsert,
                IsActive = true,
                IsDeleted = false
            };

            // تنظیم تاریخ به شمسی
            entity.SetZamanInsert(DateTime.Now);

            // محاسبه IntegrityHash
            entity.IntegrityHash = _dataIntegrityService.ComputeIntegrityHash(entity, TafsiliTypeSensitiveFields.Fields);

            var result = await _repository.AddAsync(entity);
            return result.PublicId;
        }
    }

    /// <summary>
    /// هندلر ویرایش نوع مشتری
    /// </summary>
    public class UpdateTafsiliTypeCommandHandler : IRequestHandler<UpdateTafsiliTypeCommand, bool>
    {
        private readonly IRepository<tblTafsiliType> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;
        private readonly IDataIntegrityService _dataIntegrityService;
        private readonly IAuditLogService _auditLogService;

        public UpdateTafsiliTypeCommandHandler(
            IRepository<tblTafsiliType> repository,
            IRepository<tblShobe> shobeRepository,
            IDataIntegrityService dataIntegrityService,
            IAuditLogService auditLogService)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
            _dataIntegrityService = dataIntegrityService;
            _auditLogService = auditLogService;
        }

        public async Task<bool> Handle(UpdateTafsiliTypeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByPublicIdAsync(request.PublicId);
            if (entity == null) return false;

            // پیدا کردن شعبه
            var shobe = await _shobeRepository.GetByPublicIdAsync(request.ShobePublicId);
            if (shobe == null)
            {
                throw new ArgumentException("شعبه یافت نشد.");
            }

            // پیدا کردن والد (اختیاری)
            long? parentId = null;
            if (request.ParentPublicId.HasValue)
            {
                var parent = await _repository.GetByPublicIdAsync(request.ParentPublicId.Value);
                if (parent == null)
                {
                    throw new ArgumentException("نوع مشتری والد یافت نشد.");
                }

                // جلوگیری از انتخاب خود به عنوان والد
                if (parent.Id == entity.Id)
                {
                    throw new ArgumentException("امکان انتخاب خود به عنوان والد وجود ندارد.");
                }

                parentId = parent.Id;
            }

            var oldParentId = entity.ParentId;
            var changes = new Dictionary<string, (object? oldValue, object? newValue)>
            {
                { "tblShobeId", (entity.tblShobeId.ToString(), shobe.Id.ToString()) },
                { "ParentId", (oldParentId?.ToString() ?? "", parentId?.ToString() ?? "") },
                { "Title", (entity.Title ?? "", request.Title ?? "") },
                { "IsActive", (entity.IsActive, request.IsActive) }
            };

            entity.tblShobeId = shobe.Id;
            entity.ParentId = parentId;
            entity.Title = request.Title;
            entity.IsActive = request.IsActive;
            entity.TblUserGrpIdLastEdit = request.TblUserGrpIdLastEdit;

            // تنظیم تاریخ ویرایش به شمسی
            entity.SetZamanLastEdit(DateTime.Now);

            // به‌روزرسانی IntegrityHash
            entity.IntegrityHash = _dataIntegrityService.ComputeIntegrityHash(entity, TafsiliTypeSensitiveFields.Fields);

            await _repository.UpdateAsync(entity);

            await _auditLogService.LogEntityChangeAsync(
                eventType: "Update",
                entityType: "TafsiliType",
                entityId: request.PublicId.ToString(),
                changes: changes,
                userId: request.AuditUserId,
                ct: cancellationToken);

            return true;
        }
    }

    /// <summary>
    /// هندلر حذف نوع مشتری (Soft Delete)
    /// </summary>
    public class DeleteTafsiliTypeCommandHandler : IRequestHandler<DeleteTafsiliTypeCommand, bool>
    {
        private readonly IRepository<tblTafsiliType> _repository;
        private readonly IRepository<tblAzaNoe> _azaNoeRepository;

        public DeleteTafsiliTypeCommandHandler(
            IRepository<tblTafsiliType> repository,
            IRepository<tblAzaNoe> azaNoeRepository)
        {
            _repository = repository;
            _azaNoeRepository = azaNoeRepository;
        }

        public async Task<bool> Handle(DeleteTafsiliTypeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByPublicIdAsync(request.PublicId);
            if (entity == null) return false;

            // بررسی وجود زیرمجموعه
            var allTypes = await _repository.GetAllAsync();
            var hasChildren = allTypes.Any(t => t.ParentId == entity.Id && !t.IsDeleted);
            if (hasChildren)
            {
                throw new InvalidOperationException("امکان حذف نوع مشتری با زیرمجموعه وجود ندارد. ابتدا زیرمجموعه‌ها را حذف کنید.");
            }

            // بررسی استفاده در حوزه‌ها
            var allAzaNoes = await _azaNoeRepository.GetAllAsync();
            var usedInAzaNoe = allAzaNoes.Any(a => a.tblTafsiliTypeId == entity.Id && !a.IsDeleted);
            if (usedInAzaNoe)
            {
                throw new InvalidOperationException("امکان حذف نوع مشتری وجود ندارد. این نوع مشتری در حوزه‌ها استفاده شده است.");
            }

            // Soft Delete
            entity.IsDeleted = true;
            entity.IsActive = false;
            entity.TblUserGrpIdLastEdit = request.TblUserGrpIdLastEdit;
            entity.SetZamanLastEdit(DateTime.Now);

            await _repository.UpdateAsync(entity);
            return true;
        }
    }
}
