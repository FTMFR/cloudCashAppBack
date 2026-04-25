using System;

namespace BnpCashClaudeApp.Application.DTOs.ManagementDtos
{
    /// <summary>
    /// DTO برای نمایش مخاطب مشتری
    /// </summary>
    public class CustomerContactDto
    {
        public Guid PublicId { get; set; }
        public Guid CustomerPublicId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public int ContactType { get; set; }
        public string ContactTypeName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Messenger { get; set; }
        public string? Description { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }
        public string? ZamanInsert { get; set; }
    }

    /// <summary>
    /// DTO برای ایجاد مخاطب جدید
    /// </summary>
    public class CreateCustomerContactDto
    {
        public Guid CustomerPublicId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public int ContactType { get; set; } = 1;
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Messenger { get; set; }
        public string? Description { get; set; }
        public bool IsPrimary { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO برای ویرایش مخاطب
    /// </summary>
    public class UpdateCustomerContactDto
    {
        public Guid PublicId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public int ContactType { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Messenger { get; set; }
        public string? Description { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }
    }
}
