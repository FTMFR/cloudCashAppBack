using MediatR;
using System;

namespace BnpCashClaudeApp.Application.MediatR.Commands
{
    public class CreateShobeCommand : IRequest<Guid>
    {
        public string Title { get; set; }
        public int ShobeCode { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? PostalCode { get; set; }
        public Guid? ParentPublicId { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public long TblUserGrpIdInsert { get; set; }
    }
}

