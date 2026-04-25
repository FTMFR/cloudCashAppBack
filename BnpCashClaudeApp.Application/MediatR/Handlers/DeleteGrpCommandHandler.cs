using BnpCashClaudeApp.Application.MediatR.Commands;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace BnpCashClaudeApp.Application.MediatR.Handlers
{
    public class DeleteGrpCommandHandler : IRequestHandler<DeleteGrpCommand, bool>
    {
        private readonly IRepository<tblGrp> _repository;
        private readonly IRepository<tblUserGrp> _userGrpRepository;

        public DeleteGrpCommandHandler(
            IRepository<tblGrp> repository,
            IRepository<tblUserGrp> userGrpRepository)
        {
            _repository = repository;
            _userGrpRepository = userGrpRepository;
        }

        public async Task<bool> Handle(DeleteGrpCommand request, CancellationToken cancellationToken)
        {
            var group = await _repository.GetByPublicIdAsync(request.PublicId);
            if (group == null) return false;

            // بررسی ۱: وجود زیرگروه
            var allGroup = await _repository.GetAllAsync();
            var hasChildren = allGroup.Any(s => s.ParentId == group.Id);
            if (hasChildren)
            {
                throw new InvalidOperationException("امکان حذف گروه با زیرگروه وجود ندارد. ابتدا زیرگروه‌ها را حذف کنید.");
            }

            // بررسی ۲: عدم استفاده در tblUserGrps (کاربران عضو گروه)
            var allUserGrps = await _userGrpRepository.GetAllAsync();
            var usersCount = allUserGrps.Count(ug => ug.tblGrpId == group.Id);
            if (usersCount > 0)
            {
                throw new InvalidOperationException(
                    $"امکان حذف گروه وجود ندارد. {usersCount} کاربر در این گروه عضویت دارند. ابتدا آن‌ها را از گروه خارج کنید.");
            }

            // tblGrpPermissions با CASCADE به‌طور خودکار حذف می‌شوند
            await _repository.DeleteByPublicIdAsync(request.PublicId);
            return true;
        }
    }
}
