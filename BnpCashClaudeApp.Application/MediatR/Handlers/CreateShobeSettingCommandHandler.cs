using BnpCashClaudeApp.Application.MediatR.Commands;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Handlers
{
    public class CreateShobeSettingCommandHandler : IRequestHandler<CreateShobeSettingCommand, Guid>
    {
        private readonly IRepository<tblShobeSetting> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;

        public CreateShobeSettingCommandHandler(
            IRepository<tblShobeSetting> repository,
            IRepository<tblShobe> shobeRepository)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
        }

        public async Task<Guid> Handle(CreateShobeSettingCommand request, CancellationToken cancellationToken)
        {
            // اگر ShobePublicId مشخص شده باشد، باید Shobe را پیدا کنیم
            long? tblShobeId = null;
            if (request.ShobePublicId.HasValue)
            {
                var shobe = await _shobeRepository.GetByPublicIdAsync(request.ShobePublicId.Value);
                if (shobe == null)
                {
                    throw new ArgumentException("شعبه یافت نشد.");
                }
                tblShobeId = shobe.Id;
            }

            // بررسی یکتایی SettingKey و TblShobeId
            var allSettings = await _repository.GetAllAsync();
            var existing = allSettings.FirstOrDefault(s => 
                s.SettingKey == request.SettingKey && 
                s.TblShobeId == tblShobeId);

            if (existing != null)
            {
                throw new ArgumentException($"تنظیمات با کلید '{request.SettingKey}' برای این شعبه از قبل وجود دارد.");
            }

            var setting = new tblShobeSetting
            {
                TblShobeId = tblShobeId,
                SettingKey = request.SettingKey,
                SettingName = request.SettingName,
                Description = request.Description,
                SettingValue = request.SettingValue,
                SettingType = (ShobeSettingType)request.SettingType,
                IsActive = request.IsActive,
                IsEditable = request.IsEditable,
                DisplayOrder = request.DisplayOrder,
                TblUserGrpIdInsert = request.TblUserGrpIdInsert
            };

            // تنظیم تاریخ به شمسی
            setting.SetZamanInsert(DateTime.Now);

            var result = await _repository.AddAsync(setting);
            return result.PublicId;
        }
    }
}
