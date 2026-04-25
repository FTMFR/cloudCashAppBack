using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.MediatR.Commands.Cash;
using BnpCashClaudeApp.Domain.Entities.CashSubsystem;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Handlers.Cash
{
    /// <summary>
    /// فیلدهای حساس برای محاسبه IntegrityHash در AzaNoe
    /// </summary>
    internal static class AzaNoeSensitiveFields
    {
        public static readonly string[] Fields = new[] { "Title", "CodeHoze", "tblShobeId", "tblTafsiliTypeId", "PishFarz", "IsActive" };
    }

    /// <summary>
    /// هندلر ایجاد حوزه
    /// </summary>
    public class CreateAzaNoeCommandHandler : IRequestHandler<CreateAzaNoeCommand, Guid>
    {
        private readonly IRepository<tblAzaNoe> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;
        private readonly IRepository<tblTafsiliType> _tafsiliTypeRepository;
        private readonly IDataIntegrityService _dataIntegrityService;

        public CreateAzaNoeCommandHandler(
            IRepository<tblAzaNoe> repository,
            IRepository<tblShobe> shobeRepository,
            IRepository<tblTafsiliType> tafsiliTypeRepository,
            IDataIntegrityService dataIntegrityService)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
            _tafsiliTypeRepository = tafsiliTypeRepository;
            _dataIntegrityService = dataIntegrityService;
        }

        public async Task<Guid> Handle(CreateAzaNoeCommand request, CancellationToken cancellationToken)
        {
            // پیدا کردن شعبه
            var shobe = await _shobeRepository.GetByPublicIdAsync(request.ShobePublicId);
            if (shobe == null)
            {
                throw new ArgumentException("شعبه یافت نشد.");
            }

            // پیدا کردن نوع مشتری
            var tafsiliType = await _tafsiliTypeRepository.GetByPublicIdAsync(request.TafsiliTypePublicId);
            if (tafsiliType == null)
            {
                throw new ArgumentException("نوع مشتری یافت نشد.");
            }

            var entity = new tblAzaNoe
            {
                tblShobeId = shobe.Id,
                Title = request.Title,
                CodeHoze = request.CodeHoze,
                PishFarz = request.PishFarz,
                tblTafsiliTypeId = tafsiliType.Id,
                TblUserGrpIdInsert = request.TblUserGrpIdInsert,
                IsActive = true,
                IsDeleted = false
            };

            // تنظیم تاریخ به شمسی
            entity.SetZamanInsert(DateTime.Now);

            // محاسبه IntegrityHash
            entity.IntegrityHash = _dataIntegrityService.ComputeIntegrityHash(entity, AzaNoeSensitiveFields.Fields);

            var result = await _repository.AddAsync(entity);
            return result.PublicId;
        }
    }

    /// <summary>
    /// هندلر ویرایش حوزه
    /// </summary>
    public class UpdateAzaNoeCommandHandler : IRequestHandler<UpdateAzaNoeCommand, bool>
    {
        private readonly IRepository<tblAzaNoe> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;
        private readonly IRepository<tblTafsiliType> _tafsiliTypeRepository;
        private readonly IDataIntegrityService _dataIntegrityService;
        private readonly IAuditLogService _auditLogService;

        public UpdateAzaNoeCommandHandler(
            IRepository<tblAzaNoe> repository,
            IRepository<tblShobe> shobeRepository,
            IRepository<tblTafsiliType> tafsiliTypeRepository,
            IDataIntegrityService dataIntegrityService,
            IAuditLogService auditLogService)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
            _tafsiliTypeRepository = tafsiliTypeRepository;
            _dataIntegrityService = dataIntegrityService;
            _auditLogService = auditLogService;
        }

        public async Task<bool> Handle(UpdateAzaNoeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByPublicIdAsync(request.PublicId);
            if (entity == null) return false;

            // پیدا کردن شعبه
            var shobe = await _shobeRepository.GetByPublicIdAsync(request.ShobePublicId);
            if (shobe == null)
            {
                throw new ArgumentException("شعبه یافت نشد.");
            }

            // پیدا کردن نوع مشتری
            var tafsiliType = await _tafsiliTypeRepository.GetByPublicIdAsync(request.TafsiliTypePublicId);
            if (tafsiliType == null)
            {
                throw new ArgumentException("نوع مشتری یافت نشد.");
            }

            var changes = new Dictionary<string, (object? oldValue, object? newValue)>
            {
                { "tblShobeId", (entity.tblShobeId.ToString(), shobe.Id.ToString()) },
                { "Title", (entity.Title ?? "", request.Title ?? "") },
                { "CodeHoze", (entity.CodeHoze.ToString(), request.CodeHoze.ToString()) },
                { "PishFarz", (entity.PishFarz, request.PishFarz) },
                { "tblTafsiliTypeId", (entity.tblTafsiliTypeId.ToString(), tafsiliType.Id.ToString()) },
                { "IsActive", (entity.IsActive, request.IsActive) }
            };

            entity.tblShobeId = shobe.Id;
            entity.Title = request.Title;
            entity.CodeHoze = request.CodeHoze;
            entity.PishFarz = request.PishFarz;
            entity.tblTafsiliTypeId = tafsiliType.Id;
            entity.IsActive = request.IsActive;
            entity.TblUserGrpIdLastEdit = request.TblUserGrpIdLastEdit;

            // تنظیم تاریخ ویرایش به شمسی
            entity.SetZamanLastEdit(DateTime.Now);

            // به‌روزرسانی IntegrityHash
            entity.IntegrityHash = _dataIntegrityService.ComputeIntegrityHash(entity, AzaNoeSensitiveFields.Fields);

            await _repository.UpdateAsync(entity);

            await _auditLogService.LogEntityChangeAsync(
                eventType: "Update",
                entityType: "AzaNoe",
                entityId: request.PublicId.ToString(),
                changes: changes,
                userId: request.AuditUserId,
                ct: cancellationToken);

            return true;
        }
    }

    /// <summary>
    /// هندلر حذف حوزه (Soft Delete)
    /// </summary>
    public class DeleteAzaNoeCommandHandler : IRequestHandler<DeleteAzaNoeCommand, bool>
    {
        private readonly IRepository<tblAzaNoe> _repository;

        public DeleteAzaNoeCommandHandler(IRepository<tblAzaNoe> repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(DeleteAzaNoeCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByPublicIdAsync(request.PublicId);
            if (entity == null) return false;

            // TODO: در آینده می‌توان بررسی استفاده در اعضا را اضافه کرد

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
