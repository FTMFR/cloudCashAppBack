using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BnpCashClaudeApp.Application.DTOs
{
    /// <summary>
    /// DTO گروه برای نمایش اطلاعات
    /// ============================================
    /// تمام تاریخ‌ها به صورت شمسی (string) هستند
    /// ============================================
    /// </summary>
    public class GrpDto
    {
        /// <summary>
        /// شناسه عمومی (GUID) - برای استفاده در API
        /// </summary>
        public Guid PublicId { get; set; }
        public string Title { get; set; }
        public int? GrpCode { get; set; }

        /// <summary>
        /// تاریخ ایجاد رکورد (شمسی)
        /// </summary>
        public string ZamanInsert { get; set; }

        /// <summary>
        /// تاریخ آخرین ویرایش (شمسی)
        /// </summary>
        public string? ZamanLastEdit { get; set; }

        public Guid? ParentPublicId { get; set; }
        public string? Description { get; set; }

        /// <summary>
        /// شناسه شعبه - اگر null باشد برای همه شعبات است
        /// </summary>
        public long? tblShobeId { get; set; }

        /// <summary>
        /// شناسه عمومی شعبه
        /// </summary>
        public Guid? ShobePublicId { get; set; }

        /// <summary>
        /// نام شعبه
        /// </summary>
        public string? ShobeTitle { get; set; }
        public List<GrpDto>? Children { get; set; }
    }

    public class CreateGrpDto
    {
        public string Title { get; set; }
        //public int GrpCode { get; set; }
        public Guid? ParentPublicId { get; set; }

        /// <summary>
        /// شناسه عمومی شعبه - اگر null باشد گروه برای همه شعبات است
        /// </summary>
        public Guid? ShobePublicId { get; set; }
        public string? Description { get; set; }

    }

    public class UpdateGrpDto
    {
        public string Title { get; set; }
        //public int GrpCode { get; set; }
        public Guid ParentPublicId { get; set; }

        /// <summary>
        /// شناسه عمومی شعبه - اگر null باشد گروه برای همه شعبات است
        /// </summary>
        public Guid? ShobePublicId { get; set; }
    }
    
}



