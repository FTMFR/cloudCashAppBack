using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Application.MediatR.Queries;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Handlers
{
    public class GetShobeSettingByIdQueryHandler : IRequestHandler<GetShobeSettingByIdQuery, ShobeSettingDto?>
    {
        private readonly IRepository<tblShobeSetting> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;

        public GetShobeSettingByIdQueryHandler(
            IRepository<tblShobeSetting> repository,
            IRepository<tblShobe> shobeRepository)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
        }

        public async Task<ShobeSettingDto?> Handle(GetShobeSettingByIdQuery request, CancellationToken cancellationToken)
        {
            var setting = await _repository.GetByPublicIdAsync(request.PublicId);
            if (setting == null)
                return null;

            tblShobe? shobe = null;
            if (setting.TblShobeId.HasValue)
            {
                shobe = await _shobeRepository.GetByIdAsync(setting.TblShobeId.Value);
            }

            return new ShobeSettingDto
            {
                PublicId = setting.PublicId,
                ShobePublicId = shobe?.PublicId,
                ShobeTitle = shobe?.Title,
                SettingKey = setting.SettingKey,
                SettingName = setting.SettingName,
                Description = setting.Description,
                SettingValue = setting.SettingValue,
                SettingType = (int)setting.SettingType,
                IsActive = setting.IsActive,
                IsEditable = setting.IsEditable,
                DisplayOrder = setting.DisplayOrder,
                ZamanInsert = setting.ZamanInsert,
                ZamanLastEdit = setting.ZamanLastEdit
            };
        }
    }
}
