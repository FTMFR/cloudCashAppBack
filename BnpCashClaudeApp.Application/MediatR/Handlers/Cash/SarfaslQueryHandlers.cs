using BnpCashClaudeApp.Application.DTOs.CashDtos;
using BnpCashClaudeApp.Application.MediatR.Queries.Cash;
using BnpCashClaudeApp.Domain.Entities.CashSubsystem;
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
    /// هندلر دریافت تمام سرفصل‌ها
    /// </summary>
    public class GetAllSarfaslsQueryHandler : IRequestHandler<GetAllSarfaslsQuery, List<SarfaslDto>>
    {
        private readonly IRepository<tblSarfasl> _repository;
        private readonly IRepository<tblSarfaslType> _sarfaslTypeRepository;
        private readonly IRepository<tblSarfaslProtocol> _protocolRepository;
        private readonly IRepository<tblCombo> _comboRepository;

        public GetAllSarfaslsQueryHandler(
            IRepository<tblSarfasl> repository,
            IRepository<tblSarfaslType> sarfaslTypeRepository,
            IRepository<tblSarfaslProtocol> protocolRepository,
            IRepository<tblCombo> comboRepository)
        {
            _repository = repository;
            _sarfaslTypeRepository = sarfaslTypeRepository;
            _protocolRepository = protocolRepository;
            _comboRepository = comboRepository;
        }

        public async Task<List<SarfaslDto>> Handle(GetAllSarfaslsQuery request, CancellationToken cancellationToken)
        {
            var allSarfasls = (await _repository.GetAllAsync()).ToList();
            var allTypes = (await _sarfaslTypeRepository.GetAllAsync()).ToList();
            var allProtocols = (await _protocolRepository.GetAllAsync()).ToList();
            var allCombos = (await _comboRepository.GetAllAsync()).ToList();

            // فیلتر بر اساس شعبه
            if (request.TblShobeId.HasValue)
            {
                allSarfasls = allSarfasls.Where(s => s.tblShobeId == request.TblShobeId.Value).ToList();
            }

            // فیلتر بر اساس پروتکل
            if (request.SarfaslProtocolPublicId.HasValue)
            {
                var protocol = allProtocols.FirstOrDefault(p => p.PublicId == request.SarfaslProtocolPublicId.Value);
                if (protocol != null)
                {
                    allSarfasls = allSarfasls.Where(s => s.tblSarfaslProtocolId == protocol.Id).ToList();
                }
            }

            // فیلتر بر اساس نوع سرفصل
            if (request.SarfaslTypePublicId.HasValue)
            {
                var sarfaslType = allTypes.FirstOrDefault(t => t.PublicId == request.SarfaslTypePublicId.Value);
                if (sarfaslType != null)
                {
                    allSarfasls = allSarfasls.Where(s => s.tblSarfaslTypeId == sarfaslType.Id).ToList();
                }
            }

            // فیلتر فقط با جزء تفصیلی
            if (request.OnlyWithJoze.HasValue && request.OnlyWithJoze.Value)
            {
                allSarfasls = allSarfasls.Where(s => s.WithJoze).ToList();
            }

            var result = allSarfasls.Select(s => MapToDto(s, allSarfasls, allTypes, allProtocols, allCombos)).ToList();
            return result;
        }

        private SarfaslDto MapToDto(
            tblSarfasl entity,
            List<tblSarfasl> allSarfasls,
            List<tblSarfaslType> allTypes,
            List<tblSarfaslProtocol> allProtocols,
            List<tblCombo> allCombos)
        {
            var parent = entity.ParentId.HasValue ? allSarfasls.FirstOrDefault(s => s.Id == entity.ParentId) : null;
            var sarfaslType = entity.tblSarfaslTypeId.HasValue ? allTypes.FirstOrDefault(t => t.Id == entity.tblSarfaslTypeId) : null;
            var protocol = entity.tblSarfaslProtocolId.HasValue ? allProtocols.FirstOrDefault(p => p.Id == entity.tblSarfaslProtocolId) : null;
            var vazeiatZirGrp = entity.tblComboIdVazeiatZirGrp.HasValue ? allCombos.FirstOrDefault(c => c.Id == entity.tblComboIdVazeiatZirGrp) : null;
            var controlAmaliat = entity.tblComboIdControlAmaliat.HasValue ? allCombos.FirstOrDefault(c => c.Id == entity.tblComboIdControlAmaliat) : null;

            return new SarfaslDto
            {
                PublicId = entity.PublicId,
                TblShobeId = entity.tblShobeId,
                ParentPublicId = parent?.PublicId,
                ParentTitle = parent?.Title,
                ParentCodeSarfasl = parent?.CodeSarfasl,
                SarfaslTypePublicId = sarfaslType?.PublicId,
                SarfaslTypeTitle = sarfaslType?.Title,
                SarfaslProtocolPublicId = protocol?.PublicId,
                SarfaslProtocolTitle = protocol?.Title,
                CodeSarfasl = entity.CodeSarfasl,
                Title = entity.Title,
                Description = entity.Description,
                WithJoze = entity.WithJoze,
                TblComboIdVazeiatZirGrp = entity.tblComboIdVazeiatZirGrp,
                VazeiatZirGrpTitle = vazeiatZirGrp?.Title,
                TedadArghamZirGrp = entity.TedadArghamZirGrp,
                MizanEtebarBedehkar = entity.MizanEtebarBedehkar,
                MizanEtebarBestankar = entity.MizanEtebarBestankar,
                TblComboIdControlAmaliat = entity.tblComboIdControlAmaliat,
                ControlAmaliatTitle = controlAmaliat?.Title,
                NotShowInTaraz = entity.NotShowInTaraz,
                ZamanInsert = entity.ZamanInsert,
                ZamanLastEdit = entity.ZamanLastEdit,
                ChildrenCount = allSarfasls.Count(s => s.ParentId == entity.Id)
            };
        }
    }

    /// <summary>
    /// هندلر دریافت سرفصل با شناسه
    /// </summary>
    public class GetSarfaslByIdQueryHandler : IRequestHandler<GetSarfaslByIdQuery, SarfaslDto?>
    {
        private readonly IRepository<tblSarfasl> _repository;
        private readonly IRepository<tblSarfaslType> _sarfaslTypeRepository;
        private readonly IRepository<tblSarfaslProtocol> _protocolRepository;
        private readonly IRepository<tblCombo> _comboRepository;

        public GetSarfaslByIdQueryHandler(
            IRepository<tblSarfasl> repository,
            IRepository<tblSarfaslType> sarfaslTypeRepository,
            IRepository<tblSarfaslProtocol> protocolRepository,
            IRepository<tblCombo> comboRepository)
        {
            _repository = repository;
            _sarfaslTypeRepository = sarfaslTypeRepository;
            _protocolRepository = protocolRepository;
            _comboRepository = comboRepository;
        }

        public async Task<SarfaslDto?> Handle(GetSarfaslByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByPublicIdAsync(request.PublicId);
            if (entity == null) return null;

            var allSarfasls = (await _repository.GetAllAsync()).ToList();
            var allTypes = (await _sarfaslTypeRepository.GetAllAsync()).ToList();
            var allProtocols = (await _protocolRepository.GetAllAsync()).ToList();
            var allCombos = (await _comboRepository.GetAllAsync()).ToList();

            var parent = entity.ParentId.HasValue ? allSarfasls.FirstOrDefault(s => s.Id == entity.ParentId) : null;
            var sarfaslType = entity.tblSarfaslTypeId.HasValue ? allTypes.FirstOrDefault(t => t.Id == entity.tblSarfaslTypeId) : null;
            var protocol = entity.tblSarfaslProtocolId.HasValue ? allProtocols.FirstOrDefault(p => p.Id == entity.tblSarfaslProtocolId) : null;
            var vazeiatZirGrp = entity.tblComboIdVazeiatZirGrp.HasValue ? allCombos.FirstOrDefault(c => c.Id == entity.tblComboIdVazeiatZirGrp) : null;
            var controlAmaliat = entity.tblComboIdControlAmaliat.HasValue ? allCombos.FirstOrDefault(c => c.Id == entity.tblComboIdControlAmaliat) : null;

            return new SarfaslDto
            {
                PublicId = entity.PublicId,
                TblShobeId = entity.tblShobeId,
                ParentPublicId = parent?.PublicId,
                ParentTitle = parent?.Title,
                ParentCodeSarfasl = parent?.CodeSarfasl,
                SarfaslTypePublicId = sarfaslType?.PublicId,
                SarfaslTypeTitle = sarfaslType?.Title,
                SarfaslProtocolPublicId = protocol?.PublicId,
                SarfaslProtocolTitle = protocol?.Title,
                CodeSarfasl = entity.CodeSarfasl,
                Title = entity.Title,
                Description = entity.Description,
                WithJoze = entity.WithJoze,
                TblComboIdVazeiatZirGrp = entity.tblComboIdVazeiatZirGrp,
                VazeiatZirGrpTitle = vazeiatZirGrp?.Title,
                TedadArghamZirGrp = entity.TedadArghamZirGrp,
                MizanEtebarBedehkar = entity.MizanEtebarBedehkar,
                MizanEtebarBestankar = entity.MizanEtebarBestankar,
                TblComboIdControlAmaliat = entity.tblComboIdControlAmaliat,
                ControlAmaliatTitle = controlAmaliat?.Title,
                NotShowInTaraz = entity.NotShowInTaraz,
                ZamanInsert = entity.ZamanInsert,
                ZamanLastEdit = entity.ZamanLastEdit,
                ChildrenCount = allSarfasls.Count(s => s.ParentId == entity.Id)
            };
        }
    }

    /// <summary>
    /// هندلر دریافت سرفصل‌ها به صورت درختی
    /// </summary>
    public class GetSarfaslTreeQueryHandler : IRequestHandler<GetSarfaslTreeQuery, List<SarfaslDto>>
    {
        private readonly IRepository<tblSarfasl> _repository;
        private readonly IRepository<tblSarfaslType> _sarfaslTypeRepository;
        private readonly IRepository<tblSarfaslProtocol> _protocolRepository;
        private readonly IRepository<tblCombo> _comboRepository;

        public GetSarfaslTreeQueryHandler(
            IRepository<tblSarfasl> repository,
            IRepository<tblSarfaslType> sarfaslTypeRepository,
            IRepository<tblSarfaslProtocol> protocolRepository,
            IRepository<tblCombo> comboRepository)
        {
            _repository = repository;
            _sarfaslTypeRepository = sarfaslTypeRepository;
            _protocolRepository = protocolRepository;
            _comboRepository = comboRepository;
        }

        public async Task<List<SarfaslDto>> Handle(GetSarfaslTreeQuery request, CancellationToken cancellationToken)
        {
            var allSarfasls = (await _repository.GetAllAsync()).ToList();
            var allTypes = (await _sarfaslTypeRepository.GetAllAsync()).ToList();
            var allProtocols = (await _protocolRepository.GetAllAsync()).ToList();
            var allCombos = (await _comboRepository.GetAllAsync()).ToList();

            // فیلتر بر اساس شعبه
            if (request.TblShobeId.HasValue)
            {
                allSarfasls = allSarfasls.Where(s => s.tblShobeId == request.TblShobeId.Value).ToList();
            }

            // فیلتر بر اساس پروتکل
            if (request.SarfaslProtocolPublicId.HasValue)
            {
                var protocol = allProtocols.FirstOrDefault(p => p.PublicId == request.SarfaslProtocolPublicId.Value);
                if (protocol != null)
                {
                    allSarfasls = allSarfasls.Where(s => s.tblSarfaslProtocolId == protocol.Id).ToList();
                }
            }

            // ساخت درخت از ریشه‌ها (ParentId == null)
            var rootItems = allSarfasls.Where(s => s.ParentId == null).OrderBy(s => s.CodeSarfasl).ToList();

            return rootItems.Select(root => BuildTreeNode(root, allSarfasls, allTypes, allProtocols, allCombos, 0)).ToList();
        }

        private SarfaslDto BuildTreeNode(
            tblSarfasl entity,
            List<tblSarfasl> allSarfasls,
            List<tblSarfaslType> allTypes,
            List<tblSarfaslProtocol> allProtocols,
            List<tblCombo> allCombos,
            int level)
        {
            var parent = entity.ParentId.HasValue ? allSarfasls.FirstOrDefault(s => s.Id == entity.ParentId) : null;
            var sarfaslType = entity.tblSarfaslTypeId.HasValue ? allTypes.FirstOrDefault(t => t.Id == entity.tblSarfaslTypeId) : null;
            var protocol = entity.tblSarfaslProtocolId.HasValue ? allProtocols.FirstOrDefault(p => p.Id == entity.tblSarfaslProtocolId) : null;
            var vazeiatZirGrp = entity.tblComboIdVazeiatZirGrp.HasValue ? allCombos.FirstOrDefault(c => c.Id == entity.tblComboIdVazeiatZirGrp) : null;
            var controlAmaliat = entity.tblComboIdControlAmaliat.HasValue ? allCombos.FirstOrDefault(c => c.Id == entity.tblComboIdControlAmaliat) : null;
            var children = allSarfasls.Where(s => s.ParentId == entity.Id).OrderBy(s => s.CodeSarfasl).ToList();

            return new SarfaslDto
            {
                PublicId = entity.PublicId,
                TblShobeId = entity.tblShobeId,
                ParentPublicId = parent?.PublicId,
                ParentTitle = parent?.Title,
                ParentCodeSarfasl = parent?.CodeSarfasl,
                SarfaslTypePublicId = sarfaslType?.PublicId,
                SarfaslTypeTitle = sarfaslType?.Title,
                SarfaslProtocolPublicId = protocol?.PublicId,
                SarfaslProtocolTitle = protocol?.Title,
                CodeSarfasl = entity.CodeSarfasl,
                Title = entity.Title,
                Description = entity.Description,
                WithJoze = entity.WithJoze,
                TblComboIdVazeiatZirGrp = entity.tblComboIdVazeiatZirGrp,
                VazeiatZirGrpTitle = vazeiatZirGrp?.Title,
                TedadArghamZirGrp = entity.TedadArghamZirGrp,
                MizanEtebarBedehkar = entity.MizanEtebarBedehkar,
                MizanEtebarBestankar = entity.MizanEtebarBestankar,
                TblComboIdControlAmaliat = entity.tblComboIdControlAmaliat,
                ControlAmaliatTitle = controlAmaliat?.Title,
                NotShowInTaraz = entity.NotShowInTaraz,
                ZamanInsert = entity.ZamanInsert,
                ZamanLastEdit = entity.ZamanLastEdit,
                Level = level,
                ChildrenCount = children.Count,
                Children = children.Select(c => BuildTreeNode(c, allSarfasls, allTypes, allProtocols, allCombos, level + 1)).ToList()
            };
        }
    }

    /// <summary>
    /// هندلر دریافت زیرمجموعه‌های یک سرفصل
    /// </summary>
    public class GetSarfaslChildrenQueryHandler : IRequestHandler<GetSarfaslChildrenQuery, List<SarfaslDto>>
    {
        private readonly IRepository<tblSarfasl> _repository;
        private readonly IRepository<tblSarfaslType> _sarfaslTypeRepository;
        private readonly IRepository<tblSarfaslProtocol> _protocolRepository;
        private readonly IRepository<tblCombo> _comboRepository;

        public GetSarfaslChildrenQueryHandler(
            IRepository<tblSarfasl> repository,
            IRepository<tblSarfaslType> sarfaslTypeRepository,
            IRepository<tblSarfaslProtocol> protocolRepository,
            IRepository<tblCombo> comboRepository)
        {
            _repository = repository;
            _sarfaslTypeRepository = sarfaslTypeRepository;
            _protocolRepository = protocolRepository;
            _comboRepository = comboRepository;
        }

        public async Task<List<SarfaslDto>> Handle(GetSarfaslChildrenQuery request, CancellationToken cancellationToken)
        {
            var parent = await _repository.GetByPublicIdAsync(request.ParentPublicId);
            if (parent == null) return new List<SarfaslDto>();

            var allSarfasls = (await _repository.GetAllAsync()).ToList();
            var allTypes = (await _sarfaslTypeRepository.GetAllAsync()).ToList();
            var allProtocols = (await _protocolRepository.GetAllAsync()).ToList();
            var allCombos = (await _comboRepository.GetAllAsync()).ToList();

            var children = allSarfasls.Where(s => s.ParentId == parent.Id).OrderBy(s => s.CodeSarfasl).ToList();

            return children.Select(entity =>
            {
                var sarfaslType = entity.tblSarfaslTypeId.HasValue ? allTypes.FirstOrDefault(t => t.Id == entity.tblSarfaslTypeId) : null;
                var protocol = entity.tblSarfaslProtocolId.HasValue ? allProtocols.FirstOrDefault(p => p.Id == entity.tblSarfaslProtocolId) : null;
                var vazeiatZirGrp = entity.tblComboIdVazeiatZirGrp.HasValue ? allCombos.FirstOrDefault(c => c.Id == entity.tblComboIdVazeiatZirGrp) : null;
                var controlAmaliat = entity.tblComboIdControlAmaliat.HasValue ? allCombos.FirstOrDefault(c => c.Id == entity.tblComboIdControlAmaliat) : null;

                return new SarfaslDto
                {
                    PublicId = entity.PublicId,
                    TblShobeId = entity.tblShobeId,
                    ParentPublicId = parent.PublicId,
                    ParentTitle = parent.Title,
                    ParentCodeSarfasl = parent.CodeSarfasl,
                    SarfaslTypePublicId = sarfaslType?.PublicId,
                    SarfaslTypeTitle = sarfaslType?.Title,
                    SarfaslProtocolPublicId = protocol?.PublicId,
                    SarfaslProtocolTitle = protocol?.Title,
                    CodeSarfasl = entity.CodeSarfasl,
                    Title = entity.Title,
                    Description = entity.Description,
                    WithJoze = entity.WithJoze,
                    TblComboIdVazeiatZirGrp = entity.tblComboIdVazeiatZirGrp,
                    VazeiatZirGrpTitle = vazeiatZirGrp?.Title,
                    TedadArghamZirGrp = entity.TedadArghamZirGrp,
                    MizanEtebarBedehkar = entity.MizanEtebarBedehkar,
                    MizanEtebarBestankar = entity.MizanEtebarBestankar,
                    TblComboIdControlAmaliat = entity.tblComboIdControlAmaliat,
                    ControlAmaliatTitle = controlAmaliat?.Title,
                    NotShowInTaraz = entity.NotShowInTaraz,
                    ZamanInsert = entity.ZamanInsert,
                    ZamanLastEdit = entity.ZamanLastEdit,
                    ChildrenCount = allSarfasls.Count(s => s.ParentId == entity.Id)
                };
            }).ToList();
        }
    }

    /// <summary>
    /// هندلر جستجوی سرفصل بر اساس کد
    /// </summary>
    public class GetSarfaslByCodeQueryHandler : IRequestHandler<GetSarfaslByCodeQuery, SarfaslDto?>
    {
        private readonly IRepository<tblSarfasl> _repository;
        private readonly IRepository<tblSarfaslType> _sarfaslTypeRepository;
        private readonly IRepository<tblSarfaslProtocol> _protocolRepository;
        private readonly IRepository<tblCombo> _comboRepository;

        public GetSarfaslByCodeQueryHandler(
            IRepository<tblSarfasl> repository,
            IRepository<tblSarfaslType> sarfaslTypeRepository,
            IRepository<tblSarfaslProtocol> protocolRepository,
            IRepository<tblCombo> comboRepository)
        {
            _repository = repository;
            _sarfaslTypeRepository = sarfaslTypeRepository;
            _protocolRepository = protocolRepository;
            _comboRepository = comboRepository;
        }

        public async Task<SarfaslDto?> Handle(GetSarfaslByCodeQuery request, CancellationToken cancellationToken)
        {
            var allSarfasls = (await _repository.GetAllAsync()).ToList();

            tblSarfasl? entity;
            if (request.TblShobeId.HasValue)
            {
                entity = allSarfasls.FirstOrDefault(s => s.CodeSarfasl == request.CodeSarfasl && s.tblShobeId == request.TblShobeId.Value);
            }
            else
            {
                entity = allSarfasls.FirstOrDefault(s => s.CodeSarfasl == request.CodeSarfasl);
            }

            if (entity == null) return null;

            var allTypes = (await _sarfaslTypeRepository.GetAllAsync()).ToList();
            var allProtocols = (await _protocolRepository.GetAllAsync()).ToList();
            var allCombos = (await _comboRepository.GetAllAsync()).ToList();

            var parent = entity.ParentId.HasValue ? allSarfasls.FirstOrDefault(s => s.Id == entity.ParentId) : null;
            var sarfaslType = entity.tblSarfaslTypeId.HasValue ? allTypes.FirstOrDefault(t => t.Id == entity.tblSarfaslTypeId) : null;
            var protocol = entity.tblSarfaslProtocolId.HasValue ? allProtocols.FirstOrDefault(p => p.Id == entity.tblSarfaslProtocolId) : null;
            var vazeiatZirGrp = entity.tblComboIdVazeiatZirGrp.HasValue ? allCombos.FirstOrDefault(c => c.Id == entity.tblComboIdVazeiatZirGrp) : null;
            var controlAmaliat = entity.tblComboIdControlAmaliat.HasValue ? allCombos.FirstOrDefault(c => c.Id == entity.tblComboIdControlAmaliat) : null;

            return new SarfaslDto
            {
                PublicId = entity.PublicId,
                TblShobeId = entity.tblShobeId,
                ParentPublicId = parent?.PublicId,
                ParentTitle = parent?.Title,
                ParentCodeSarfasl = parent?.CodeSarfasl,
                SarfaslTypePublicId = sarfaslType?.PublicId,
                SarfaslTypeTitle = sarfaslType?.Title,
                SarfaslProtocolPublicId = protocol?.PublicId,
                SarfaslProtocolTitle = protocol?.Title,
                CodeSarfasl = entity.CodeSarfasl,
                Title = entity.Title,
                Description = entity.Description,
                WithJoze = entity.WithJoze,
                TblComboIdVazeiatZirGrp = entity.tblComboIdVazeiatZirGrp,
                VazeiatZirGrpTitle = vazeiatZirGrp?.Title,
                TedadArghamZirGrp = entity.TedadArghamZirGrp,
                MizanEtebarBedehkar = entity.MizanEtebarBedehkar,
                MizanEtebarBestankar = entity.MizanEtebarBestankar,
                TblComboIdControlAmaliat = entity.tblComboIdControlAmaliat,
                ControlAmaliatTitle = controlAmaliat?.Title,
                NotShowInTaraz = entity.NotShowInTaraz,
                ZamanInsert = entity.ZamanInsert,
                ZamanLastEdit = entity.ZamanLastEdit,
                ChildrenCount = allSarfasls.Count(s => s.ParentId == entity.Id)
            };
        }
    }
}
