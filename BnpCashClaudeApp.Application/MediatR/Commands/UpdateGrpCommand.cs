using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace BnpCashClaudeApp.Application.MediatR.Commands
{
    public class UpdateGrpCommand : IRequest<bool>
    {
        /// <summary>
        /// شناسه عمومی (GUID) - برای استفاده در API
        /// </summary>
        public Guid PublicId { get; set; }
        public string Title { get; set; }
        //public int GrpCode { get; set; }
        public long TblUserGrpIdLastEdit { get; set; }
        public Guid ParentPublicId { get; set; }
        public string? Description { get; set; }

        /// <summary>
        /// شناسه عمومی شعبه - اگر null باشد گروه برای همه شعبات است
        /// </summary>
        public Guid? ShobePublicId { get; set; }

        /// <summary>
        /// شناسه کاربر برای ثبت در Audit Log (اختیاری)
        /// </summary>
        public long? AuditUserId { get; set; }
    }
}
