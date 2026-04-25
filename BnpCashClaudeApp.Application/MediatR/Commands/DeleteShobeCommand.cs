using MediatR;
using System;

namespace BnpCashClaudeApp.Application.MediatR.Commands
{
    public class DeleteShobeCommand : IRequest<bool>
    {
        /// <summary>
        /// شناسه عمومی (GUID) - برای استفاده در API
        /// </summary>
        public Guid PublicId { get; set; }
    }
}

