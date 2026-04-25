using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.MediatR.Commands;
using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace BnpCashClaudeApp.Application.MediatR.Handlers
{
    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, bool>
    {
        private readonly IRepository<tblUser> _repository;
        private readonly IRepository<tblUserGrp> _userGrpRepository;
        private readonly IRepository<tblGrp> _grpRepository;
        private readonly IAuditLogService _auditLogService;

        public UpdateUserCommandHandler(
            IRepository<tblUser> repository,
            IRepository<tblUserGrp> userGrpRepository,
            IRepository<tblGrp> grpRepository,
            IAuditLogService auditLogService)
        {
            _repository = repository;
            _userGrpRepository = userGrpRepository;
            _grpRepository = grpRepository;
            _auditLogService = auditLogService;
        }

        public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            // ============================================
            // اعتبارسنجی شماره موبایل (الزامی برای بازیابی رمز عبور و MFA)
            // ============================================
            if (string.IsNullOrWhiteSpace(request.MobileNumber))
                throw new ArgumentException("شماره موبایل الزامی است. برای بازیابی رمز عبور و MFA نیاز است.");

            var user = await _repository.GetByPublicIdAsync(request.PublicId);
            if (user == null) return false;

            var changes = new Dictionary<string, (object? oldValue, object? newValue)>
            {
                { "FirstName", (user.FirstName, request.FirstName) },
                { "LastName", (user.LastName, request.LastName) },
                { "Email", (user.Email ?? "", request.Email ?? "") },
                { "Phone", (user.Phone ?? "", request.Phone ?? "") },
                { "MobileNumber", (user.MobileNumber ?? "", request.MobileNumber ?? "") },
                { "tblCustomerId", (user.tblCustomerId?.ToString() ?? "", request.tblCustomerId?.ToString() ?? "") },
                { "tblShobeId", (user.tblShobeId?.ToString() ?? "", request.tblShobeId?.ToString() ?? "") }
            };

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;
            user.Phone = request.Phone;
            user.MobileNumber = request.MobileNumber;
            user.SetZamanLastEdit(DateTime.Now); // تاریخ به شمسی
            user.TblUserGrpIdLastEdit = request.TblUserGrpIdLastEdit;
            // Multi-tenancy: مشتری و شعبه
            user.tblCustomerId = request.tblCustomerId;
            user.tblShobeId = request.tblShobeId;

            await _repository.UpdateAsync(user);

            // به‌روزرسانی گروه کاربر در صورت ارسال GrpPublicId
            if (request.GrpPublicId.HasValue)
            {
                var group = await _grpRepository.GetByPublicIdAsync(request.GrpPublicId.Value);
                if (group == null)
                    throw new ArgumentException($"گروه با شناسه {request.GrpPublicId} یافت نشد");

                var allUserGrps = (await _userGrpRepository.GetAllAsync()).ToList();
                var userGrps = allUserGrps.Where(ug => ug.tblUserId == user.Id).ToList();
                foreach (var ug in userGrps)
                    await _userGrpRepository.DeleteAsync(ug.Id);

                var now = DateTime.Now;
                var newUserGrp = new tblUserGrp
                {
                    tblUserId = user.Id,
                    tblGrpId = group.Id,
                    TblUserGrpIdInsert = request.TblUserGrpIdLastEdit
                };
                newUserGrp.AssignmentDate = BaseEntity.ToPersianDateTime(now);
                newUserGrp.SetZamanInsert(now);
                await _userGrpRepository.AddAsync(newUserGrp);
            }

            await _auditLogService.LogEntityChangeAsync(
                eventType: "Update",
                entityType: "User",
                entityId: request.PublicId.ToString(),
                changes: changes,
                userId: request.AuditUserId,
                ct: cancellationToken);

            return true;
        }
    }
}
