using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace BnpCashClaudeApp.Application.MediatR.Commands
{
    public class DeleteUserCommand : IRequest<bool>
    {
        /// <summary>
        /// شناسه عمومی (GUID) - برای استفاده در API
        /// </summary>
        public Guid PublicId { get; set; }
    }
}
