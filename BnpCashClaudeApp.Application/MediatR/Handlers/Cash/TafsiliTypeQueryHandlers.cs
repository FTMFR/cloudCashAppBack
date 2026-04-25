using BnpCashClaudeApp.Application.DTOs.CashDtos;
using BnpCashClaudeApp.Application.MediatR.Queries.Cash;
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
    /// هندلر دریافت تمام انواع مشتری
    /// </summary>
    public class GetAllTafsiliTypesQueryHandler : IRequestHandler<GetAllTafsiliTypesQuery, List<TafsiliTypeDto>>
    {
        private readonly IRepository<tblTafsiliType> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;
        private readonly IRepository<tblAzaNoe> _azaNoeRepository;

        public GetAllTafsiliTypesQueryHandler(
            IRepository<tblTafsiliType> repository,
            IRepository<tblShobe> shobeRepository,
            IRepository<tblAzaNoe> azaNoeRepository)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
            _azaNoeRepository = azaNoeRepository;
        }

        public async Task<List<TafsiliTypeDto>> Handle(GetAllTafsiliTypesQuery request, CancellationToken cancellationToken)
        {
            var allTypes = (await _repository.GetAllAsync()).ToList();
            var allShobes = (await _shobeRepository.GetAllAsync()).ToList();
            var allAzaNoes = (await _azaNoeRepository.GetAllAsync()).ToList();

            // فیلتر بر اساس شعبه
            if (request.ShobePublicId.HasValue)
            {
                var shobe = allShobes.FirstOrDefault(s => s.PublicId == request.ShobePublicId.Value);
                if (shobe != null)
                {
                    allTypes = allTypes.Where(t => t.tblShobeId == shobe.Id).ToList();
                }
            }

            // فیلتر فقط موارد فعال
            if (request.OnlyActive)
            {
                allTypes = allTypes.Where(t => t.IsActive).ToList();
            }

            var result = allTypes.Select(t =>
            {
                var shobe = allShobes.FirstOrDefault(s => s.Id == t.tblShobeId);
                var parent = t.ParentId.HasValue ? allTypes.FirstOrDefault(p => p.Id == t.ParentId) : null;

                return new TafsiliTypeDto
                {
                    PublicId = t.PublicId,
                    ShobePublicId = shobe?.PublicId,
                    ShobeTitle = shobe?.Title,
                    ParentPublicId = parent?.PublicId,
                    ParentTitle = parent?.Title,
                    Title = t.Title,
                    CodeTafsiliType = t.CodeTafsiliType,
                    IsActive = t.IsActive,
                    ZamanInsert = t.ZamanInsert,
                    ZamanLastEdit = t.ZamanLastEdit,
                    AzaNoeCount = allAzaNoes.Count(a => a.tblTafsiliTypeId == t.Id && !a.IsDeleted)
                };
            }).ToList();

            return result;
        }
    }

    /// <summary>
    /// هندلر دریافت نوع مشتری با شناسه
    /// </summary>
    public class GetTafsiliTypeByIdQueryHandler : IRequestHandler<GetTafsiliTypeByIdQuery, TafsiliTypeDto?>
    {
        private readonly IRepository<tblTafsiliType> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;
        private readonly IRepository<tblAzaNoe> _azaNoeRepository;

        public GetTafsiliTypeByIdQueryHandler(
            IRepository<tblTafsiliType> repository,
            IRepository<tblShobe> shobeRepository,
            IRepository<tblAzaNoe> azaNoeRepository)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
            _azaNoeRepository = azaNoeRepository;
        }

        public async Task<TafsiliTypeDto?> Handle(GetTafsiliTypeByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByPublicIdAsync(request.PublicId);
            if (entity == null) return null;

            var allTypes = await _repository.GetAllAsync();
            var allShobes = await _shobeRepository.GetAllAsync();
            var allAzaNoes = await _azaNoeRepository.GetAllAsync();

            var shobe = allShobes.FirstOrDefault(s => s.Id == entity.tblShobeId);
            var parent = entity.ParentId.HasValue ? allTypes.FirstOrDefault(p => p.Id == entity.ParentId) : null;

            return new TafsiliTypeDto
            {
                PublicId = entity.PublicId,
                ShobePublicId = shobe?.PublicId,
                ShobeTitle = shobe?.Title,
                ParentPublicId = parent?.PublicId,
                ParentTitle = parent?.Title,
                Title = entity.Title,
                CodeTafsiliType = entity.CodeTafsiliType,
                IsActive = entity.IsActive,
                ZamanInsert = entity.ZamanInsert,
                ZamanLastEdit = entity.ZamanLastEdit,
                AzaNoeCount = allAzaNoes.Count(a => a.tblTafsiliTypeId == entity.Id && !a.IsDeleted)
            };
        }
    }

    /// <summary>
    /// هندلر دریافت انواع مشتری به صورت درختی
    /// </summary>
    public class GetTafsiliTypeTreeQueryHandler : IRequestHandler<GetTafsiliTypeTreeQuery, List<TafsiliTypeDto>>
    {
        private readonly IRepository<tblTafsiliType> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;
        private readonly IRepository<tblAzaNoe> _azaNoeRepository;

        public GetTafsiliTypeTreeQueryHandler(
            IRepository<tblTafsiliType> repository,
            IRepository<tblShobe> shobeRepository,
            IRepository<tblAzaNoe> azaNoeRepository)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
            _azaNoeRepository = azaNoeRepository;
        }

        public async Task<List<TafsiliTypeDto>> Handle(GetTafsiliTypeTreeQuery request, CancellationToken cancellationToken)
        {
            var allTypes = (await _repository.GetAllAsync()).ToList();
            var allShobes = (await _shobeRepository.GetAllAsync()).ToList();
            var allAzaNoes = (await _azaNoeRepository.GetAllAsync()).ToList();

            // فیلتر بر اساس شعبه
            if (request.ShobePublicId.HasValue)
            {
                var shobe = allShobes.FirstOrDefault(s => s.PublicId == request.ShobePublicId.Value);
                if (shobe != null)
                {
                    allTypes = allTypes.Where(t => t.tblShobeId == shobe.Id).ToList();
                }
            }

            // فیلتر فقط موارد فعال
            if (request.OnlyActive)
            {
                allTypes = allTypes.Where(t => t.IsActive).ToList();
            }

            // ساخت درخت از ریشه‌ها (ParentId == null)
            var rootItems = allTypes.Where(t => t.ParentId == null).ToList();

            return rootItems.Select(root => BuildTreeNode(root, allTypes, allShobes, allAzaNoes)).ToList();
        }

        private TafsiliTypeDto BuildTreeNode(
            tblTafsiliType entity,
            List<tblTafsiliType> allTypes,
            List<tblShobe> allShobes,
            List<tblAzaNoe> allAzaNoes)
        {
            var shobe = allShobes.FirstOrDefault(s => s.Id == entity.tblShobeId);
            var parent = entity.ParentId.HasValue ? allTypes.FirstOrDefault(p => p.Id == entity.ParentId) : null;
            var children = allTypes.Where(t => t.ParentId == entity.Id).ToList();

            return new TafsiliTypeDto
            {
                PublicId = entity.PublicId,
                ShobePublicId = shobe?.PublicId,
                ShobeTitle = shobe?.Title,
                ParentPublicId = parent?.PublicId,
                ParentTitle = parent?.Title,
                Title = entity.Title,
                CodeTafsiliType = entity.CodeTafsiliType,
                IsActive = entity.IsActive,
                ZamanInsert = entity.ZamanInsert,
                ZamanLastEdit = entity.ZamanLastEdit,
                AzaNoeCount = allAzaNoes.Count(a => a.tblTafsiliTypeId == entity.Id && !a.IsDeleted),
                Children = children.Select(c => BuildTreeNode(c, allTypes, allShobes, allAzaNoes)).ToList()
            };
        }
    }
}
