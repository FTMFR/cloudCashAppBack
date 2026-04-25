using MediatR;
using System;

namespace BnpCashClaudeApp.Application.MediatR.Commands
{
    public class CreateShobeSettingCommand : IRequest<Guid>
    {
        public Guid? ShobePublicId { get; set; }
        public string SettingKey { get; set; }
        public string SettingName { get; set; }
        public string? Description { get; set; }
        public string SettingValue { get; set; } = "{}";
        public int SettingType { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsEditable { get; set; } = true;
        public int DisplayOrder { get; set; }
        public long TblUserGrpIdInsert { get; set; }
    }
}
