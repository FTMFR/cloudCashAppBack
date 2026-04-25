using System;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Application.DTOs.ManagementDtos
{
    /// <summary>
    /// DTO برای نمایش اشتراک مشتری روی نرم‌افزار
    /// </summary>
    public class CustomerSoftwareDto
    {
        public Guid PublicId { get; set; }
        public Guid CustomerPublicId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public Guid SoftwarePublicId { get; set; }
        public string SoftwareName { get; set; } = string.Empty;
        public string SoftwareCode { get; set; } = string.Empty;
        public Guid PlanPublicId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string PlanCode { get; set; } = string.Empty;
        public string LicenseKey { get; set; } = string.Empty;
        public int LicenseCount { get; set; }
        public int UsedCount { get; set; }
        public int RemainingCount => LicenseCount - UsedCount;
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public int SubscriptionType { get; set; }
        public string SubscriptionTypeName { get; set; } = string.Empty;
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string? InstalledVersion { get; set; }
        public string? LastActivationDate { get; set; }
        public string? LastActivationIp { get; set; }
        public int ActivationCount { get; set; }
        public int? MaxActivations { get; set; }
        public string? Notes { get; set; }
        public decimal? PaidAmount { get; set; }
        public int? DiscountPercent { get; set; }
        public string? ZamanInsert { get; set; }

        // اطلاعات پلن
        public int? MaxMemberCount { get; set; }
        public int? MaxUserCount { get; set; }
        public int? MaxBranchCount { get; set; }
    }

    /// <summary>
    /// DTO برای ایجاد اشتراک جدید مشتری
    /// </summary>
    public class CreateCustomerSoftwareDto
    {
        public Guid CustomerPublicId { get; set; }
        public Guid SoftwarePublicId { get; set; }
        public Guid PlanPublicId { get; set; }
        public string? LicenseKey { get; set; }
        public int LicenseCount { get; set; } = 1;
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public int SubscriptionType { get; set; } = 1;
        public int Status { get; set; } = 1;
        public int? MaxActivations { get; set; }
        public string? Notes { get; set; }
        public decimal? PaidAmount { get; set; }
        public int? DiscountPercent { get; set; }

        // دیتابیس اختصاصی (اختیاری)
        public CreateDbForSubscriptionDto? Database { get; set; }
    }

    /// <summary>
    /// DTO برای ویرایش اشتراک مشتری
    /// </summary>
    public class UpdateCustomerSoftwareDto
    {
        public Guid PublicId { get; set; }
        public Guid PlanPublicId { get; set; }
        public int LicenseCount { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public int SubscriptionType { get; set; }
        public int Status { get; set; }
        public int? MaxActivations { get; set; }
        public string? Notes { get; set; }
        public decimal? PaidAmount { get; set; }
        public int? DiscountPercent { get; set; }
    }

    /// <summary>
    /// DTO برای فعال‌سازی لایسنس
    /// </summary>
    public class ActivateLicenseDto
    {
        public string LicenseKey { get; set; } = string.Empty;
        public string? MachineId { get; set; }
        public string? IpAddress { get; set; }
        public string? Version { get; set; }
    }

    /// <summary>
    /// DTO برای نتیجه فعال‌سازی
    /// </summary>
    public class ActivationResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public CustomerSoftwareDto? Subscription { get; set; }
        public string? ConnectionString { get; set; }
        public int? MaxMemberCount { get; set; }
        public int? MaxUserCount { get; set; }
        public int? MaxBranchCount { get; set; }
        public List<string>? Features { get; set; }
    }

    /// <summary>
    /// DTO برای ایجاد دیتابیس همراه با اشتراک
    /// </summary>
    public class CreateDbForSubscriptionDto
    {
        public string Name { get; set; } = string.Empty;
        public string DbCode { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public int? Port { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int DbType { get; set; } = 1;
        public int Environment { get; set; } = 4;
    }
}
