using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.MediatR.Commands.Cash;
using BnpCashClaudeApp.Domain.Entities.CashSubsystem;
using System.Collections.Generic;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Handlers.Cash
{
    /// <summary>
    /// فیلدهای حساس برای محاسبه IntegrityHash در Sarfasl
    /// </summary>
    internal static class SarfaslSensitiveFields
    {
        public static readonly string[] Fields = new[] { 
            "CodeSarfasl", "Title", "tblShobeId", "ParentId", 
            "tblSarfaslTypeId", "MizanEtebarBedehkar", "MizanEtebarBestankar" 
        };
    }

    /// <summary>
    /// هندلر ایجاد سرفصل جدید
    /// </summary>
    public class CreateSarfaslCommandHandler : IRequestHandler<CreateSarfaslCommand, Guid>
    {
        private readonly IRepository<tblSarfasl> _repository;
        private readonly IRepository<tblSarfaslType> _sarfaslTypeRepository;
        private readonly IRepository<tblSarfaslProtocol> _protocolRepository;
        private readonly IDataIntegrityService _dataIntegrityService;

        public CreateSarfaslCommandHandler(
            IRepository<tblSarfasl> repository,
            IRepository<tblSarfaslType> sarfaslTypeRepository,
            IRepository<tblSarfaslProtocol> protocolRepository,
            IDataIntegrityService dataIntegrityService)
        {
            _repository = repository;
            _sarfaslTypeRepository = sarfaslTypeRepository;
            _protocolRepository = protocolRepository;
            _dataIntegrityService = dataIntegrityService;
        }

        public async Task<Guid> Handle(CreateSarfaslCommand request, CancellationToken cancellationToken)
        {
            // بررسی کد سرفصل
            if (string.IsNullOrWhiteSpace(request.CodeSarfasl))
            {
                throw new ArgumentException("کد سرفصل نمی‌تواند خالی باشد.");
            }

            // پیدا کردن والد (اختیاری)
            long? parentId = null;
            if (request.ParentPublicId.HasValue)
            {
                var parent = await _repository.GetByPublicIdAsync(request.ParentPublicId.Value);
                if (parent == null)
                {
                    throw new ArgumentException("سرفصل والد یافت نشد.");
                }
                parentId = parent.Id;
            }

            // پیدا کردن نوع سرفصل (اختیاری)
            long? sarfaslTypeId = null;
            if (request.SarfaslTypePublicId.HasValue)
            {
                var sarfaslType = await _sarfaslTypeRepository.GetByPublicIdAsync(request.SarfaslTypePublicId.Value);
                if (sarfaslType == null)
                {
                    throw new ArgumentException("نوع سرفصل یافت نشد.");
                }
                sarfaslTypeId = sarfaslType.Id;
            }

            // پیدا کردن پروتکل (اختیاری)
            long? protocolId = null;
            if (request.SarfaslProtocolPublicId.HasValue)
            {
                var protocol = await _protocolRepository.GetByPublicIdAsync(request.SarfaslProtocolPublicId.Value);
                if (protocol == null)
                {
                    throw new ArgumentException("پروتکل سرفصل یافت نشد.");
                }
                protocolId = protocol.Id;
            }

            var entity = new tblSarfasl
            {
                tblShobeId = request.TblShobeId,
                ParentId = parentId,
                tblSarfaslTypeId = sarfaslTypeId,
                tblSarfaslProtocolId = protocolId,
                CodeSarfasl = request.CodeSarfasl,
                Title = request.Title,
                Description = request.Description,
                WithJoze = request.WithJoze,
                tblComboIdVazeiatZirGrp = request.TblComboIdVazeiatZirGrp,
                TedadArghamZirGrp = request.TedadArghamZirGrp,
                MizanEtebarBedehkar = request.MizanEtebarBedehkar,
                MizanEtebarBestankar = request.MizanEtebarBestankar,
                tblComboIdControlAmaliat = request.TblComboIdControlAmaliat,
                NotShowInTaraz = request.NotShowInTaraz,
                TblUserGrpIdInsert = request.TblUserGrpIdInsert
            };

            // تنظیم تاریخ به شمسی
            entity.SetZamanInsert(DateTime.Now);

            // محاسبه IntegrityHash
            entity.IntegrityHash = _dataIntegrityService.ComputeIntegrityHash(entity, SarfaslSensitiveFields.Fields);

            var result = await _repository.AddAsync(entity);
            return result.PublicId;
        }
    }

    /// <summary>
    /// هندلر ویرایش سرفصل
    /// </summary>
    public class UpdateSarfaslCommandHandler : IRequestHandler<UpdateSarfaslCommand, bool>
    {
        private readonly IRepository<tblSarfasl> _repository;
        private readonly IRepository<tblSarfaslType> _sarfaslTypeRepository;
        private readonly IRepository<tblSarfaslProtocol> _protocolRepository;
        private readonly IDataIntegrityService _dataIntegrityService;
        private readonly IAuditLogService _auditLogService;

        public UpdateSarfaslCommandHandler(
            IRepository<tblSarfasl> repository,
            IRepository<tblSarfaslType> sarfaslTypeRepository,
            IRepository<tblSarfaslProtocol> protocolRepository,
            IDataIntegrityService dataIntegrityService,
            IAuditLogService auditLogService)
        {
            _repository = repository;
            _sarfaslTypeRepository = sarfaslTypeRepository;
            _protocolRepository = protocolRepository;
            _dataIntegrityService = dataIntegrityService;
            _auditLogService = auditLogService;
        }

        public async Task<bool> Handle(UpdateSarfaslCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByPublicIdAsync(request.PublicId);
            if (entity == null) return false;

            var oldParentId = entity.ParentId;
            var oldSarfaslTypeId = entity.tblSarfaslTypeId;
            var oldProtocolId = entity.tblSarfaslProtocolId;
            var changes = new Dictionary<string, (object? oldValue, object? newValue)>
            {
                { "tblShobeId", (entity.tblShobeId, request.TblShobeId) },
                { "CodeSarfasl", (entity.CodeSarfasl ?? "", request.CodeSarfasl ?? "") },
                { "Title", (entity.Title ?? "", request.Title ?? "") },
                { "Description", (entity.Description ?? "", request.Description ?? "") },
                { "WithJoze", (entity.WithJoze, request.WithJoze) },
                { "TedadArghamZirGrp", (entity.TedadArghamZirGrp?.ToString() ?? "", request.TedadArghamZirGrp?.ToString() ?? "") },
                { "MizanEtebarBedehkar", (entity.MizanEtebarBedehkar, request.MizanEtebarBedehkar) },
                { "MizanEtebarBestankar", (entity.MizanEtebarBestankar, request.MizanEtebarBestankar) },
                { "NotShowInTaraz", (entity.NotShowInTaraz, request.NotShowInTaraz) },
                { "ParentId", (oldParentId?.ToString() ?? "", "") },
                { "tblSarfaslTypeId", (oldSarfaslTypeId?.ToString() ?? "", "") },
                { "tblSarfaslProtocolId", (oldProtocolId?.ToString() ?? "", "") }
            };

            // بررسی کد سرفصل
            if (string.IsNullOrWhiteSpace(request.CodeSarfasl))
            {
                throw new ArgumentException("کد سرفصل نمی‌تواند خالی باشد.");
            }

            // پیدا کردن والد (اختیاری)
            long? parentId = null;
            if (request.ParentPublicId.HasValue)
            {
                var parent = await _repository.GetByPublicIdAsync(request.ParentPublicId.Value);
                if (parent == null)
                {
                    throw new ArgumentException("سرفصل والد یافت نشد.");
                }

                // جلوگیری از انتخاب خود به عنوان والد
                if (parent.Id == entity.Id)
                {
                    throw new ArgumentException("امکان انتخاب خود به عنوان والد وجود ندارد.");
                }

                parentId = parent.Id;
            }

            // پیدا کردن نوع سرفصل (اختیاری)
            long? sarfaslTypeId = null;
            if (request.SarfaslTypePublicId.HasValue)
            {
                var sarfaslType = await _sarfaslTypeRepository.GetByPublicIdAsync(request.SarfaslTypePublicId.Value);
                if (sarfaslType == null)
                {
                    throw new ArgumentException("نوع سرفصل یافت نشد.");
                }
                sarfaslTypeId = sarfaslType.Id;
            }

            // پیدا کردن پروتکل (اختیاری)
            long? protocolId = null;
            if (request.SarfaslProtocolPublicId.HasValue)
            {
                var protocol = await _protocolRepository.GetByPublicIdAsync(request.SarfaslProtocolPublicId.Value);
                if (protocol == null)
                {
                    throw new ArgumentException("پروتکل سرفصل یافت نشد.");
                }
                protocolId = protocol.Id;
            }

            entity.tblShobeId = request.TblShobeId;
            entity.ParentId = parentId;
            entity.tblSarfaslTypeId = sarfaslTypeId;
            entity.tblSarfaslProtocolId = protocolId;
            entity.CodeSarfasl = request.CodeSarfasl;
            entity.Title = request.Title;
            entity.Description = request.Description;
            entity.WithJoze = request.WithJoze;
            entity.tblComboIdVazeiatZirGrp = request.TblComboIdVazeiatZirGrp;
            entity.TedadArghamZirGrp = request.TedadArghamZirGrp;
            entity.MizanEtebarBedehkar = request.MizanEtebarBedehkar;
            entity.MizanEtebarBestankar = request.MizanEtebarBestankar;
            entity.tblComboIdControlAmaliat = request.TblComboIdControlAmaliat;
            entity.NotShowInTaraz = request.NotShowInTaraz;
            entity.TblUserGrpIdLastEdit = request.TblUserGrpIdLastEdit;

            changes["ParentId"] = (oldParentId?.ToString() ?? "", parentId?.ToString() ?? "");
            changes["tblSarfaslTypeId"] = (oldSarfaslTypeId?.ToString() ?? "", sarfaslTypeId?.ToString() ?? "");
            changes["tblSarfaslProtocolId"] = (oldProtocolId?.ToString() ?? "", protocolId?.ToString() ?? "");

            // تنظیم تاریخ ویرایش به شمسی
            entity.SetZamanLastEdit(DateTime.Now);

            // به‌روزرسانی IntegrityHash
            entity.IntegrityHash = _dataIntegrityService.ComputeIntegrityHash(entity, SarfaslSensitiveFields.Fields);

            await _repository.UpdateAsync(entity);

            await _auditLogService.LogEntityChangeAsync(
                eventType: "Update",
                entityType: "Sarfasl",
                entityId: request.PublicId.ToString(),
                changes: changes,
                userId: request.AuditUserId,
                ct: cancellationToken);

            return true;
        }
    }

    /// <summary>
    /// هندلر حذف سرفصل
    /// </summary>
    public class DeleteSarfaslCommandHandler : IRequestHandler<DeleteSarfaslCommand, bool>
    {
        private readonly IRepository<tblSarfasl> _repository;

        public DeleteSarfaslCommandHandler(IRepository<tblSarfasl> repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(DeleteSarfaslCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByPublicIdAsync(request.PublicId);
            if (entity == null) return false;

            // بررسی وجود زیرمجموعه
            var allSarfasls = await _repository.GetAllAsync();
            var hasChildren = allSarfasls.Any(s => s.ParentId == entity.Id);
            if (hasChildren)
            {
                throw new InvalidOperationException("امکان حذف سرفصل با زیرمجموعه وجود ندارد. ابتدا زیرمجموعه‌ها را حذف کنید.");
            }

            // حذف فیزیکی
            await _repository.DeleteAsync(entity.Id);
            return true;
        }
    }
}
