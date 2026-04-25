using System;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Application.DTOs.ManagementDtos
{
    /// <summary>
    /// DTO برای نمایش نرم‌افزار
    /// </summary>
    public class SoftwareDto
    {
        public Guid PublicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? CurrentVersion { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? DownloadUrl { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public string? ZamanInsert { get; set; }

        // آمار
        public int PlansCount { get; set; }
        public int CustomersCount { get; set; }
    }

    /// <summary>
    /// DTO برای ایجاد نرم‌افزار جدید
    /// </summary>
    public class CreateSoftwareDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? CurrentVersion { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? DownloadUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }

    /// <summary>
    /// DTO برای ویرایش نرم‌افزار
    /// </summary>
    public class UpdateSoftwareDto
    {
        public Guid PublicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? CurrentVersion { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? DownloadUrl { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// DTO برای نمایش نرم‌افزار با پلن‌ها
    /// </summary>
    public class SoftwareWithPlansDto : SoftwareDto
    {
        public List<PlanDto> Plans { get; set; } = new List<PlanDto>();
    }
}
