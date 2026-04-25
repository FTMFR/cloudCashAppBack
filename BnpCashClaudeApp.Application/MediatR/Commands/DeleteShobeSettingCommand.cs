using MediatR;
using System;

namespace BnpCashClaudeApp.Application.MediatR.Commands
{
    public class DeleteShobeSettingCommand : IRequest<bool>
    {
        public Guid PublicId { get; set; }
    }
}
