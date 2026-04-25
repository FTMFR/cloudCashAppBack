using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Application.MediatR.Queries;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Handlers
{
    public class GetAllShobeSettingsQueryHandler : IRequestHandler<GetAllShobeSettingsQuery, List<ShobeSettingDto>>
    {
        private readonly IRepository<tblShobeSetting> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;

        public GetAllShobeSettingsQueryHandler(
            IRepository<tblShobeSetting> repository,
            IRepository<tblShobe> shobeRepository)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
        }

        public async Task<List<ShobeSettingDto>> Handle(GetAllShobeSettingsQuery request, CancellationToken cancellationToken)
        {
            var allSettings = await _repository.GetAllAsync();
            var allShobes = await _shobeRepository.GetAllAsync();
            var shobeList = allShobes.ToList();

            IEnumerable<tblShobeSetting> settings = allSettings;

            // فیلتر بر اساس ShobePublicId در صورت وجود
            if (request.ShobePublicId.HasValue)
            {
                var shobe = shobeList.FirstOrDefault(s => s.PublicId == request.ShobePublicId.Value);
                if (shobe != null)
                {
                    settings = settings.Where(s => s.TblShobeId == shobe.Id);
                }
                else
                {
                    // اگر شعبه یافت نشد، لیست خالی برگردان
                    return new List<ShobeSettingDto>();
                }
            }

            return settings.Select(s =>
            {
                var shobe = s.TblShobeId.HasValue 
                    ? shobeList.FirstOrDefault(sh => sh.Id == s.TblShobeId.Value)
                    : null;

                return new ShobeSettingDto
                {
                    PublicId = s.PublicId,
                    ShobePublicId = shobe?.PublicId,
                    ShobeTitle = shobe?.Title,
                    SettingKey = s.SettingKey,
                    SettingName = s.SettingName,
                    Description = s.Description,
                    SettingValue = s.SettingValue,
                    SettingType = (int)s.SettingType,
                    IsActive = s.IsActive,
                    IsEditable = s.IsEditable,
                    DisplayOrder = s.DisplayOrder,
                    ZamanInsert = s.ZamanInsert,
                    ZamanLastEdit = s.ZamanLastEdit
                };
            }).ToList();
        }
    }
}
