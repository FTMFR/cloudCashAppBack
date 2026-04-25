using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace BnpCashClaudeApp.Application.MediatR.Queries
{
    public class GetUserByIdQuery : IRequest<UserDto>
    {
        public Guid PublicId { get; set; }
    }

    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
    {
        private readonly IRepository<tblUser> _userRepository;
        private readonly IRepository<tblUserGrp> _userGrpRepository;
        private readonly IRepository<tblGrp> _grpRepository;

        public GetUserByIdQueryHandler(
            IRepository<tblUser> userRepository,
            IRepository<tblUserGrp> userGrpRepository,
            IRepository<tblGrp> grpRepository)
        {
            _userRepository = userRepository;
            _userGrpRepository = userGrpRepository;
            _grpRepository = grpRepository;
        }

        public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByPublicIdAsync(request.PublicId);
            if (user == null) return null;

            // ============================================
            // واکشی اطلاعات گروه کاربر
            // ============================================
            tblGrp grp = null;
            var allUserGrps = await _userGrpRepository.GetAllAsync();
            var userGrp = allUserGrps.FirstOrDefault(ug => ug.tblUserId == user.Id);
            if (userGrp != null)
            {
                grp = await _grpRepository.GetByIdAsync(userGrp.tblGrpId);
            }

            return new UserDto
            {
                PublicId = user.PublicId,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                MobileNumber = user.MobileNumber,
                ZamanInsert = user.ZamanInsert,
                ZamanLastEdit = user.ZamanLastEdit,
                UserCode = user.UserCode,
                IpAddress = user.IpAddress,
                IsActive = user.IsActive,
                PasswordLastChangedAt = user.PasswordLastChangedAt,
                LastLoginAt = user.LastLoginAt,
                MustChangePassword = user.MustChangePassword,
                // Multi-tenancy
                tblCustomerId = user.tblCustomerId,
                CustomerName = user.Customer?.Name,
                tblShobeId = user.tblShobeId,
                ShobeName = user.Shobe?.Title,
                // اطلاعات گروه
                GrpPublicId = grp?.PublicId,
                GrpTitle = grp?.Title
            };
        }
    }
}
