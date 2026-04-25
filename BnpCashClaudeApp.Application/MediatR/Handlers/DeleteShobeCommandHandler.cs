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
    public class DeleteShobeCommandHandler : IRequestHandler<DeleteShobeCommand, bool>
    {
        private readonly IRepository<tblShobe> _repository;

        public DeleteShobeCommandHandler(IRepository<tblShobe> repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(DeleteShobeCommand request, CancellationToken cancellationToken)
        {
            var shobe = await _repository.GetByPublicIdAsync(request.PublicId);
            if (shobe == null)
                return false;

            // بررسی اینکه آیا این شعبه دارای زیرشعب است یا نه
            var allShobes = await _repository.GetAllAsync();
            var hasChildren = allShobes.Any(s => s.ParentId == shobe.Id);
            
            if (hasChildren)
            {
                throw new InvalidOperationException("امکان حذف شعبه با زیرشعب وجود ندارد. ابتدا زیرشعب‌ها را حذف کنید.");
            }

            await _repository.DeleteByPublicIdAsync(request.PublicId);
            return true;
        }
    }
}

