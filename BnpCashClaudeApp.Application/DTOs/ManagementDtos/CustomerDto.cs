using System;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Application.DTOs.ManagementDtos
{
    /// <summary>
    /// DTO برای نمایش مشتری
    /// </summary>
    public class CustomerDto
    {
        public Guid PublicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public int CustomerType { get; set; }
        public string CustomerTypeName { get; set; } = string.Empty;
        public string? NationalId { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? CompanyNationalId { get; set; }
        public string? EconomicCode { get; set; }
        public string? ManagerName { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? PostalCode { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? LogoPath { get; set; }
        public string? Description { get; set; }
        public string? MembershipDate { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int? LoyaltyPoints { get; set; }
        public int CustomerLevel { get; set; }
        public string CustomerLevelName { get; set; } = string.Empty;
        public string? ZamanInsert { get; set; }

        // آمار
        public int SoftwaresCount { get; set; }
        public int ContactsCount { get; set; }
    }

    /// <summary>
    /// DTO برای ایجاد مشتری جدید
    /// </summary>
    public class CreateCustomerDto
    {
        public string Name { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public int CustomerType { get; set; } = 2;
        public string? NationalId { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? CompanyNationalId { get; set; }
        public string? EconomicCode { get; set; }
        public string? ManagerName { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? PostalCode { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; } = 1;
        public int CustomerLevel { get; set; } = 1;
    }

    /// <summary>
    /// DTO برای ویرایش مشتری
    /// </summary>
    public class UpdateCustomerDto
    {
        public Guid PublicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public int CustomerType { get; set; }
        public string? NationalId { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? CompanyNationalId { get; set; }
        public string? EconomicCode { get; set; }
        public string? ManagerName { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? PostalCode { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; }
        public int CustomerLevel { get; set; }
    }

    /// <summary>
    /// DTO برای نمایش مشتری با اشتراک‌ها
    /// </summary>
    public class CustomerWithSubscriptionsDto : CustomerDto
    {
        public List<CustomerSoftwareDto> Subscriptions { get; set; } = new List<CustomerSoftwareDto>();
        public List<CustomerContactDto> Contacts { get; set; } = new List<CustomerContactDto>();
    }
}
