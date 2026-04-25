using BnpCashClaudeApp.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BnpCashClaudeApp.Domain.Entities.NavigationSubsystem
{
    public class tblGrpMenu:BaseEntity
    {
        public int tblGrpId { get; set; } 
        public int tblMenuId { get; set; } 
        public int Status { get; set; }

        public virtual tblGrp tblGrp { get; set; }
        public virtual tblMenu tblMenu { get; set; }
    }
}
