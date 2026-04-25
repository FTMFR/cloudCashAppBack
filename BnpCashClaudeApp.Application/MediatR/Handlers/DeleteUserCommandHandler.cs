using BnpCashClaudeApp.Application.MediatR.Commands;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace BnpCashClaudeApp.Application.MediatR.Handlers
{
    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
    {
        private readonly IRepository<tblUser> _repository;

        public DeleteUserCommandHandler(IRepository<tblUser> repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var exists = await _repository.ExistsByPublicIdAsync(request.PublicId);
            if (!exists) return false;

            await _repository.DeleteByPublicIdAsync(request.PublicId);
            return true;
        }
    }
}
