using MediatR;
using System;

namespace BnpCashClaudeApp.Application.MediatR.Commands
{
    public class UpdateShobeSettingCommand : IRequest<bool>
    {
        public Guid PublicId { get; set; }
        public string SettingName { get; set; }
        public string? Description { get; set; }
        public string SettingValue { get; set; }
        public int SettingType { get; set; }
        public bool IsActive { get; set; }
        public bool IsEditable { get; set; }
        public int DisplayOrder { get; set; }
        public long TblUserGrpIdLastEdit { get; set; }

        /// <summary>
        /// شناسه کاربر برای ثبت در Audit Log (اختیاری)
        /// </summary>
        public long? AuditUserId { get; set; }
    }
}
