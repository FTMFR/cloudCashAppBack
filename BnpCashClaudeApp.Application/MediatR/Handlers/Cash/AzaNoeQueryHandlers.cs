using BnpCashClaudeApp.Application.DTOs.CashDtos;
using BnpCashClaudeApp.Application.MediatR.Queries.Cash;
using BnpCashClaudeApp.Domain.Entities.CashSubsystem;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Handlers.Cash
{
    /// <summary>
    /// هندلر دریافت تمام حوزه‌ها
    /// </summary>
    public class GetAllAzaNoesQueryHandler : IRequestHandler<GetAllAzaNoesQuery, List<AzaNoeDto>>
    {
        private readonly IRepository<tblAzaNoe> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;
        private readonly IRepository<tblTafsiliType> _tafsiliTypeRepository;

        public GetAllAzaNoesQueryHandler(
            IRepository<tblAzaNoe> repository,
            IRepository<tblShobe> shobeRepository,
            IRepository<tblTafsiliType> tafsiliTypeRepository)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
            _tafsiliTypeRepository = tafsiliTypeRepository;
        }

        public async Task<List<AzaNoeDto>> Handle(GetAllAzaNoesQuery request, CancellationToken cancellationToken)
        {
            var allAzaNoes = (await _repository.GetAllAsync()).ToList();
            var allShobes = (await _shobeRepository.GetAllAsync()).ToList();
            var allTafsiliTypes = (await _tafsiliTypeRepository.GetAllAsync()).ToList();

            // فیلتر بر اساس شعبه
            if (request.ShobePublicId.HasValue)
            {
                var shobe = allShobes.FirstOrDefault(s => s.PublicId == request.ShobePublicId.Value);
                if (shobe != null)
                {
                    allAzaNoes = allAzaNoes.Where(a => a.tblShobeId == shobe.Id).ToList();
                }
            }

            // فیلتر بر اساس نوع مشتری
            if (request.TafsiliTypePublicId.HasValue)
            {
                var tafsiliType = allTafsiliTypes.FirstOrDefault(t => t.PublicId == request.TafsiliTypePublicId.Value);
                if (tafsiliType != null)
                {
                    allAzaNoes = allAzaNoes.Where(a => a.tblTafsiliTypeId == tafsiliType.Id).ToList();
                }
            }

            // فیلتر فقط موارد فعال
            if (request.OnlyActive)
            {
                allAzaNoes = allAzaNoes.Where(a => a.IsActive).ToList();
            }

            var result = allAzaNoes.Select(a =>
            {
                var shobe = allShobes.FirstOrDefault(s => s.Id == a.tblShobeId);
                var tafsiliType = allTafsiliTypes.FirstOrDefault(t => t.Id == a.tblTafsiliTypeId);

                return new AzaNoeDto
                {
                    PublicId = a.PublicId,
                    ShobePublicId = shobe?.PublicId,
                    ShobeTitle = shobe?.Title,
                    Title = a.Title,
                    CodeHoze = a.CodeHoze,
                    PishFarz = a.PishFarz,
                    TafsiliTypePublicId = tafsiliType?.PublicId ?? default,
                    TafsiliTypeTitle = tafsiliType?.Title,
                    TafsiliTypeCode = tafsiliType?.CodeTafsiliType ?? 0,
                    IsActive = a.IsActive,
                    ZamanInsert = a.ZamanInsert,
                    ZamanLastEdit = a.ZamanLastEdit
                };
            }).ToList();

            return result;
        }
    }

    /// <summary>
    /// هندلر دریافت حوزه با شناسه
    /// </summary>
    public class GetAzaNoeByIdQueryHandler : IRequestHandler<GetAzaNoeByIdQuery, AzaNoeDto?>
    {
        private readonly IRepository<tblAzaNoe> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;
        private readonly IRepository<tblTafsiliType> _tafsiliTypeRepository;

        public GetAzaNoeByIdQueryHandler(
            IRepository<tblAzaNoe> repository,
            IRepository<tblShobe> shobeRepository,
            IRepository<tblTafsiliType> tafsiliTypeRepository)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
            _tafsiliTypeRepository = tafsiliTypeRepository;
        }

        public async Task<AzaNoeDto?> Handle(GetAzaNoeByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByPublicIdAsync(request.PublicId);
            if (entity == null) return null;

            var allShobes = await _shobeRepository.GetAllAsync();
            var allTafsiliTypes = await _tafsiliTypeRepository.GetAllAsync();

            var shobe = allShobes.FirstOrDefault(s => s.Id == entity.tblShobeId);
            var tafsiliType = allTafsiliTypes.FirstOrDefault(t => t.Id == entity.tblTafsiliTypeId);

            return new AzaNoeDto
            {
                PublicId = entity.PublicId,
                ShobePublicId = shobe?.PublicId,
                ShobeTitle = shobe?.Title,
                Title = entity.Title,
                CodeHoze = entity.CodeHoze,
                PishFarz = entity.PishFarz,
                TafsiliTypePublicId = tafsiliType?.PublicId ?? default,
                TafsiliTypeTitle = tafsiliType?.Title,
                TafsiliTypeCode = tafsiliType?.CodeTafsiliType ?? 0,
                IsActive = entity.IsActive,
                ZamanInsert = entity.ZamanInsert,
                ZamanLastEdit = entity.ZamanLastEdit
            };
        }
    }

    /// <summary>
    /// هندلر دریافت حوزه‌های یک نوع مشتری خاص
    /// </summary>
    public class GetAzaNoesByTafsiliTypeQueryHandler : IRequestHandler<GetAzaNoesByTafsiliTypeQuery, List<AzaNoeDto>>
    {
        private readonly IRepository<tblAzaNoe> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;
        private readonly IRepository<tblTafsiliType> _tafsiliTypeRepository;

        public GetAzaNoesByTafsiliTypeQueryHandler(
            IRepository<tblAzaNoe> repository,
            IRepository<tblShobe> shobeRepository,
            IRepository<tblTafsiliType> tafsiliTypeRepository)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
            _tafsiliTypeRepository = tafsiliTypeRepository;
        }

        public async Task<List<AzaNoeDto>> Handle(GetAzaNoesByTafsiliTypeQuery request, CancellationToken cancellationToken)
        {
            var allAzaNoes = (await _repository.GetAllAsync()).ToList();
            var allShobes = (await _shobeRepository.GetAllAsync()).ToList();
            var allTafsiliTypes = (await _tafsiliTypeRepository.GetAllAsync()).ToList();

            var tafsiliType = allTafsiliTypes.FirstOrDefault(t => t.PublicId == request.TafsiliTypePublicId);
            if (tafsiliType == null) return new List<AzaNoeDto>();

            var filteredAzaNoes = allAzaNoes.Where(a => a.tblTafsiliTypeId == tafsiliType.Id);

            // فیلتر فقط موارد فعال
            if (request.OnlyActive)
            {
                filteredAzaNoes = filteredAzaNoes.Where(a => a.IsActive);
            }

            var result = filteredAzaNoes.Select(a =>
            {
                var shobe = allShobes.FirstOrDefault(s => s.Id == a.tblShobeId);

                return new AzaNoeDto
                {
                    PublicId = a.PublicId,
                    ShobePublicId = shobe?.PublicId,
                    ShobeTitle = shobe?.Title,
                    Title = a.Title,
                    CodeHoze = a.CodeHoze,
                    PishFarz = a.PishFarz,
                    TafsiliTypePublicId = tafsiliType.PublicId,
                    TafsiliTypeTitle = tafsiliType.Title,
                    TafsiliTypeCode = tafsiliType.CodeTafsiliType,
                    IsActive = a.IsActive,
                    ZamanInsert = a.ZamanInsert,
                    ZamanLastEdit = a.ZamanLastEdit
                };
            }).ToList();

            return result;
        }
    }
}
