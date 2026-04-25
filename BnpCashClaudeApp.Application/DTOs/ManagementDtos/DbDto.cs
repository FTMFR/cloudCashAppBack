using System;

namespace BnpCashClaudeApp.Application.DTOs.ManagementDtos
{
    /// <summary>
    /// DTO برای نمایش دیتابیس
    /// </summary>
    public class DbDto
    {
        public Guid PublicId { get; set; }
        public Guid? CustomerPublicId { get; set; }
        public string? CustomerName { get; set; }
        public Guid SoftwarePublicId { get; set; }
        public string SoftwareName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DbCode { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public int? Port { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public string? Username { get; set; }
        public int DbType { get; set; }
        public string DbTypeName { get; set; } = string.Empty;
        public int Environment { get; set; }
        public string EnvironmentName { get; set; } = string.Empty;
        public bool IsShared { get; set; }
        public string? TenantId { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsReadOnly { get; set; }
        public int? MaxSizeMB { get; set; }
        public int? CurrentSizeMB { get; set; }
        public string? LastBackupDate { get; set; }
        public string? LastConnectionTestDate { get; set; }
        public bool? LastConnectionTestResult { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public string? ZamanInsert { get; set; }

        // آمار
        public int SubscriptionsCount { get; set; }
    }

    /// <summary>
    /// DTO برای ایجاد دیتابیس جدید
    /// </summary>
    public class CreateDbDto
    {
        public Guid? CustomerPublicId { get; set; }
        public Guid SoftwarePublicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DbCode { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public int? Port { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ConnectionString { get; set; }
        public int DbType { get; set; } = 1;
        public int Environment { get; set; } = 4;
        public bool IsShared { get; set; } = false;
        public string? TenantId { get; set; }
        public bool IsPrimary { get; set; } = true;
        public bool IsReadOnly { get; set; } = false;
        public int? MaxSizeMB { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; } = 1;
        public int DisplayOrder { get; set; } = 0;
    }

    /// <summary>
    /// DTO برای ویرایش دیتابیس
    /// </summary>
    public class UpdateDbDto
    {
        public Guid PublicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DbCode { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public int? Port { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ConnectionString { get; set; }
        public int DbType { get; set; }
        public int Environment { get; set; }
        public bool IsShared { get; set; }
        public string? TenantId { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsReadOnly { get; set; }
        public int? MaxSizeMB { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// DTO برای تست اتصال دیتابیس
    /// </summary>
    public class TestDbConnectionDto
    {
        public string ServerName { get; set; } = string.Empty;
        public int? Port { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int DbType { get; set; } = 1;
    }

    /// <summary>
    /// DTO برای نتیجه تست اتصال
    /// </summary>
    public class DbConnectionTestResultDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ResponseTimeMs { get; set; }
        public string? ServerVersion { get; set; }
    }
}
