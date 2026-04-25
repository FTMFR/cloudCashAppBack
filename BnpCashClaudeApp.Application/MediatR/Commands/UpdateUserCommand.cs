using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace BnpCashClaudeApp.Application.MediatR.Commands
{
    public class UpdateUserCommand : IRequest<bool>
    {
        /// <summary>
        /// شناسه عمومی (GUID) - برای استفاده در API
        /// </summary>
        public Guid PublicId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string MobileNumber { get; set; }
        public long TblUserGrpIdLastEdit { get; set; }

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

        /// <summary>
        /// شناسه عمومی گروه (اختیاری) - در صورت ارسال، گروه کاربر به‌روزرسانی می‌شود
        /// </summary>
        public Guid? GrpPublicId { get; set; }

        /// <summary>
        /// شناسه کاربر برای ثبت در Audit Log (اختیاری)
        /// </summary>
        public long? AuditUserId { get; set; }
    }
}
