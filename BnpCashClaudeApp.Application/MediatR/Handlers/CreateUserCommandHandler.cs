using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.MediatR.Commands;
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
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
    {
        private readonly IRepository<tblUser> _userRepository;
        private readonly IRepository<tblUserGrp> _userGrpRepository;
        private readonly IRepository<tblGrp> _grpRepository;
        private readonly IPasswordHasher _passwordHasher;

        public CreateUserCommandHandler(
            IRepository<tblUser> userRepository,
            IRepository<tblUserGrp> userGrpRepository,
            IRepository<tblGrp> grpRepository,
            IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _userGrpRepository = userGrpRepository;
            _grpRepository = grpRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            // ============================================
            // اعتبارسنجی شماره موبایل (الزامی برای بازیابی رمز عبور و MFA)
            // ============================================
            if (string.IsNullOrWhiteSpace(request.MobileNumber))
                throw new ArgumentException("شماره موبایل الزامی است. برای بازیابی رمز عبور و MFA نیاز است.");

            // پیدا کردن گروه بر اساس PublicId
            var group = await _grpRepository.GetByPublicIdAsync(request.GrpPublicId);
            if (group == null)
                throw new ArgumentException($"گروه با شناسه {request.GrpPublicId} یافت نشد");

            // بررسی یکتایی نام کاربری (UserName در tblUsers یکتا است)
            var allUsers = await _userRepository.GetAllAsync();
            if (allUsers.Any(u => string.Equals(u.UserName, request.UserName, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("این نام کاربری قبلاً ثبت شده است.");

            // تولید خودکار UserCode: آخرین UserCode + 1 یا 1 اگر هیچ کاربری وجود نداشته باشد
            var lastUserCode = allUsers
                .Where(u => u.UserCode.HasValue)
                .Select(u => u.UserCode.Value)
                .DefaultIfEmpty(0)
                .Max();
            var newUserCode = lastUserCode + 1;

            // Hash کردن پسورد قبل از ذخیره
            var hashedPassword = _passwordHasher.HashPassword(request.Password);
            var now = DateTime.Now;

            var user = new tblUser
            {
                UserName = request.UserName,
                Password = hashedPassword,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                MobileNumber = request.MobileNumber,
                UserCode = newUserCode, // تولید خودکار
                TblUserGrpIdInsert = request.TblUserGrpIdInsert,
                IpAddress = request.IpAddress,
                // Multi-tenancy: مشتری و شعبه
                tblCustomerId = request.tblCustomerId,
                tblShobeId = request.tblShobeId
            };
            // تنظیم تاریخ به شمسی
            user.SetZamanInsert(now);

            // ذخیره یوزر
            var createdUser = await _userRepository.AddAsync(user);

            // همزمان ثبت ارتباط کاربر/گروه در tblUserGrp
            var userGrp = new tblUserGrp
            {
                tblUserId = createdUser.Id,
                tblGrpId = group.Id, // استفاده از Id داخلی برای Foreign Key
                TblUserGrpIdInsert = request.TblUserGrpIdInsert
            };
            // تنظیم تاریخ‌ها به شمسی
            userGrp.AssignmentDate = BnpCashClaudeApp.Domain.Common.BaseEntity.ToPersianDateTime(now);
            userGrp.SetZamanInsert(now);

            await _userGrpRepository.AddAsync(userGrp);

            return createdUser.PublicId; // برگرداندن PublicId به جای Id
        }
    }
}
