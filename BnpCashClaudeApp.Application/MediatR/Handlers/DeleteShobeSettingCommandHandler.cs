using BnpCashClaudeApp.Application.MediatR.Commands;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Handlers
{
    public class DeleteShobeSettingCommandHandler : IRequestHandler<DeleteShobeSettingCommand, bool>
    {
        private readonly IRepository<tblShobeSetting> _repository;

        public DeleteShobeSettingCommandHandler(IRepository<tblShobeSetting> repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(DeleteShobeSettingCommand request, CancellationToken cancellationToken)
        {
            var setting = await _repository.GetByPublicIdAsync(request.PublicId);
            if (setting == null)
                return false;

            if (!setting.IsEditable)
            {
                throw new InvalidOperationException("این تنظیم قابل حذف نیست.");
            }

            await _repository.DeleteByPublicIdAsync(request.PublicId);
            return true;
        }
    }
}
