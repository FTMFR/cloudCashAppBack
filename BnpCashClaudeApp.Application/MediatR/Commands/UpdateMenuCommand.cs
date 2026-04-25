using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace BnpCashClaudeApp.Application.MediatR.Commands
{
    public class UpdateMenuCommand : IRequest<bool>
    {
        /// <summary>
        /// شناسه عمومی (GUID) - برای استفاده در API
        /// </summary>
        public Guid PublicId { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
        /// <summary>
        /// شناسه عمومی منوی والد (GUID)
        /// </summary>
        public Guid? ParentPublicId { get; set; }
        public long TblUserGrpIdLastEdit { get; set; }

        /// <summary>
        /// شناسه نرم‌افزار (اختیاری)
        /// null = منوی عمومی (راهبری سیستم)
        /// </summary>
        public long? tblSoftwareId { get; set; }

        /// <summary>
        /// شناسه کاربر برای ثبت در Audit Log (اختیاری)
        /// </summary>
        public long? AuditUserId { get; set; }
    }
}
