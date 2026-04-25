using BnpCashClaudeApp.Application.DTOs;
using MediatR;
using System;

namespace BnpCashClaudeApp.Application.MediatR.Queries
{
    public class GetShobeSettingByIdQuery : IRequest<ShobeSettingDto?>
    {
        public Guid PublicId { get; set; }
    }
}
