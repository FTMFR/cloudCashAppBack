using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace BnpCashClaudeApp.Application.MediatR.Commands
{
    public class CreateUserCommand : IRequest<Guid>
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string MobileNumber { get; set; }
        // UserCode به صورت خودکار تولید می‌شود - از ورودی حذف شد
        public long TblUserGrpIdInsert { get; set; }
        public string IpAddress { get; set; }

        /// <summary>
        /// شناسه عمومی گروه (GUID) - برای استفاده در API
        /// </summary>
        public Guid GrpPublicId { get; set; }

        /// <summary>
        /// شناسه مشتری (اختیاری)
        /// null = کاربر سیستمی (Super Admin)
        /// </summary>
        public long? tblCustomerId { get; set; }

        /// <summary>
        /// شناسه شعبه (اختیاری)
        /// null = کاربر سطح مشتری (دسترسی به همه شعب)
        /// </summary>
        public long? tblShobeId { get; set; }
    }
}
