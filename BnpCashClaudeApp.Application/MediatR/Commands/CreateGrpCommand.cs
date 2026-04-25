using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace BnpCashClaudeApp.Application.MediatR.Commands
{
    public class CreateGrpCommand : IRequest<Guid>
    {
        public string Title { get; set; }
        //public int GrpCode { get; set; }
        public long TblUserGrpIdInsert { get; set; }
        public Guid? ParentPublicId { get; set; }
        public string? Description { get; set; }

        /// <summary>
        /// شناسه عمومی شعبه - اگر null باشد گروه برای همه شعبات است
        /// </summary>
        public Guid? ShobePublicId { get; set; }
    }
}