using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace BnpCashClaudeApp.Application.MediatR.Queries
{
    public class GetAllUsersQuery : IRequest<List<UserDto>>
    {
    }

    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, List<UserDto>>
    {
        private readonly IRepository<tblUser> _userRepository;
        private readonly IRepository<tblUserGrp> _userGrpRepository;
        private readonly IRepository<tblGrp> _grpRepository;

        public GetAllUsersQueryHandler(
            IRepository<tblUser> userRepository,
            IRepository<tblUserGrp> userGrpRepository,
            IRepository<tblGrp> grpRepository)
        {
            _userRepository = userRepository;
            _userGrpRepository = userGrpRepository;
            _grpRepository = grpRepository;
        }

        public async Task<List<UserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetAllAsync();
            var userGrps = await _userGrpRepository.GetAllAsync();
            var grps = await _grpRepository.GetAllAsync();

            // ============================================
            // ساختن دیکشنری گروه‌ها برای دسترسی سریع
            // ============================================
            var grpDict = grps.ToDictionary(g => g.Id, g => g);

            // ساختن دیکشنری ارتباط کاربر-گروه (اولین گروه هر کاربر)
            var userGrpDict = userGrps
                .GroupBy(ug => ug.tblUserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.First()
                );

            return users.Select(u =>
            {
                tblGrp grp = null;
                if (userGrpDict.TryGetValue(u.Id, out var userGrp) && grpDict.TryGetValue(userGrp.tblGrpId, out var foundGrp))
                {
                    grp = foundGrp;
                }

                return new UserDto
                {
                    PublicId = u.PublicId,
                    UserName = u.UserName,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Phone = u.Phone,
                    MobileNumber = u.MobileNumber,
                    UserCode = u.UserCode,
                    IpAddress = u.IpAddress,
                    IsActive = u.IsActive,
                    ZamanInsert = u.ZamanInsert,
                    ZamanLastEdit = u.ZamanLastEdit,
                    PasswordLastChangedAt = u.PasswordLastChangedAt,
                    LastLoginAt = u.LastLoginAt,
                    MustChangePassword = u.MustChangePassword,
                    // Multi-tenancy
                    tblCustomerId = u.tblCustomerId,
                    CustomerName = u.Customer?.Name,
                    tblShobeId = u.tblShobeId,
                    ShobeName = u.Shobe?.Title,
                    // اطلاعات گروه
                    GrpPublicId = grp?.PublicId,
                    GrpTitle = grp?.Title
                };
            }).ToList();
        }
    }
}
