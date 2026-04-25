using MediatR;
using System;

namespace BnpCashClaudeApp.Application.MediatR.Commands
{
    public class UpdateShobeCommand : IRequest<bool>
    {
        /// <summary>
        /// شناسه عمومی (GUID) - برای استفاده در API
        /// </summary>
        public Guid PublicId { get; set; }
        public string Title { get; set; }
        public int ShobeCode { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? PostalCode { get; set; }
        public Guid? ParentPublicId { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public long TblUserGrpIdLastEdit { get; set; }

        /// <summary>
        /// شناسه کاربر برای ثبت در Audit Log (اختیاری)
        /// </summary>
        public long? AuditUserId { get; set; }
    }
}

