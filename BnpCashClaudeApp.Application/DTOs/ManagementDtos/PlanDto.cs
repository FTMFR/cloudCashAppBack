using System;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Application.DTOs.ManagementDtos
{
    /// <summary>
    /// DTO برای نمایش پلن
    /// </summary>
    public class PlanDto
    {
        public Guid PublicId { get; set; }
        public Guid SoftwarePublicId { get; set; }
        public string SoftwareName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? MaxMemberCount { get; set; }
        public int? MaxUserCount { get; set; }
        public int? MaxBranchCount { get; set; }
        public int? MaxDbSizeMB { get; set; }
        public int? MaxDailyTransactions { get; set; }
        public List<string>? Features { get; set; }
        public decimal? BasePrice { get; set; }
        public decimal? MonthlyPrice { get; set; }
        public decimal? YearlyPrice { get; set; }
        public int PlanType { get; set; }
        public string PlanTypeName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public int DisplayOrder { get; set; }
        public string? ZamanInsert { get; set; }

        // آمار
        public int CustomersCount { get; set; }
    }

    /// <summary>
    /// DTO برای ایجاد پلن جدید
    /// </summary>
    public class CreatePlanDto
    {
        public Guid SoftwarePublicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? MaxMemberCount { get; set; }
        public int? MaxUserCount { get; set; }
        public int? MaxBranchCount { get; set; }
        public int? MaxDbSizeMB { get; set; }
        public int? MaxDailyTransactions { get; set; }
        public List<string>? Features { get; set; }
        public decimal? BasePrice { get; set; }
        public decimal? MonthlyPrice { get; set; }
        public decimal? YearlyPrice { get; set; }
        public int PlanType { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
    }

    /// <summary>
    /// DTO برای ویرایش پلن
    /// </summary>
    public class UpdatePlanDto
    {
        public Guid PublicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? MaxMemberCount { get; set; }
        public int? MaxUserCount { get; set; }
        public int? MaxBranchCount { get; set; }
        public int? MaxDbSizeMB { get; set; }
        public int? MaxDailyTransactions { get; set; }
        public List<string>? Features { get; set; }
        public decimal? BasePrice { get; set; }
        public decimal? MonthlyPrice { get; set; }
        public decimal? YearlyPrice { get; set; }
        public int PlanType { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public int DisplayOrder { get; set; }
    }
}
