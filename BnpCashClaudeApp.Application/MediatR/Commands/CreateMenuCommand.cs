using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace BnpCashClaudeApp.Application.MediatR.Commands
{
    public class CreateMenuCommand : IRequest<Guid>
    {
        public string Title { get; set; }
        public string Path { get; set; }
        /// <summary>
        /// شناسه عمومی منوی والد (GUID)
        /// </summary>
        public Guid? ParentPublicId { get; set; }
        public long TblUserGrpIdInsert { get; set; }

        /// <summary>
        /// شناسه نرم‌افزار (اختیاری)
        /// null = منوی عمومی (راهبری سیستم)
        /// </summary>
        public long? tblSoftwareId { get; set; }
    }
}
