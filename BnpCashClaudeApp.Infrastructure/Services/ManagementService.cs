using BnpCashClaudeApp.Application.DTOs.ManagementDtos;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.ManagementSubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// پیاده‌سازی سرویس راهبری سیستم
    /// از NavigationDbContext استفاده می‌کند (همه جداول در یک دیتابیس)
    /// </summary>
    public class ManagementService : IManagementService
    {
        private readonly NavigationDbContext _context;
        private readonly ILogger<ManagementService> _logger;
        private readonly IDatabaseConnectionService _databaseConnectionService;
        public ManagementService(
            NavigationDbContext context,
            ILogger<ManagementService> logger,
            IDatabaseConnectionService databaseConnectionService)
        {
            _context = context;
            _logger = logger;
            _databaseConnectionService = databaseConnectionService;
        }

        // ============================================
        // نرم‌افزارها (Software)
        // ============================================

        public async Task<List<SoftwareDto>> GetSoftwaresAsync(bool? isActive = null, CancellationToken ct = default)
        {
            var query = _context.tblSoftwares.AsQueryable();

            if (isActive.HasValue)
                query = query.Where(s => s.IsActive == isActive.Value);

            return await query
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .Select(s => new SoftwareDto
                {
                    PublicId = s.PublicId,
                    Name = s.Name,
                    Code = s.Code,
                    CurrentVersion = s.CurrentVersion,
                    Description = s.Description,
                    Icon = s.Icon,
                    WebsiteUrl = s.WebsiteUrl,
                    DownloadUrl = s.DownloadUrl,
                    IsActive = s.IsActive,
                    DisplayOrder = s.DisplayOrder,
                    ZamanInsert = s.ZamanInsert,
                    PlansCount = s.Plans.Count,
                    CustomersCount = s.CustomerSoftwares.Select(cs => cs.tblCustomerId).Distinct().Count()
                })
                .ToListAsync(ct);
        }

        public async Task<SoftwareDto?> GetSoftwareByPublicIdAsync(Guid publicId, CancellationToken ct = default)
        {
            return await _context.tblSoftwares
                .Where(s => s.PublicId == publicId)
                .Select(s => new SoftwareDto
                {
                    PublicId = s.PublicId,
                    Name = s.Name,
                    Code = s.Code,
                    CurrentVersion = s.CurrentVersion,
                    Description = s.Description,
                    Icon = s.Icon,
                    WebsiteUrl = s.WebsiteUrl,
                    DownloadUrl = s.DownloadUrl,
                    IsActive = s.IsActive,
                    DisplayOrder = s.DisplayOrder,
                    ZamanInsert = s.ZamanInsert,
                    PlansCount = s.Plans.Count,
                    CustomersCount = s.CustomerSoftwares.Select(cs => cs.tblCustomerId).Distinct().Count()
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<SoftwareDto?> GetSoftwareByCodeAsync(string code, CancellationToken ct = default)
        {
            return await _context.tblSoftwares
                .Where(s => s.Code == code)
                .Select(s => new SoftwareDto
                {
                    PublicId = s.PublicId,
                    Name = s.Name,
                    Code = s.Code,
                    CurrentVersion = s.CurrentVersion,
                    Description = s.Description,
                    Icon = s.Icon,
                    WebsiteUrl = s.WebsiteUrl,
                    DownloadUrl = s.DownloadUrl,
                    IsActive = s.IsActive,
                    DisplayOrder = s.DisplayOrder,
                    ZamanInsert = s.ZamanInsert,
                    PlansCount = s.Plans.Count,
                    CustomersCount = s.CustomerSoftwares.Select(cs => cs.tblCustomerId).Distinct().Count()
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task<SoftwareWithPlansDto?> GetSoftwareWithPlansAsync(Guid publicId, CancellationToken ct = default)
        {
            var software = await _context.tblSoftwares
                .Include(s => s.Plans)
                .FirstOrDefaultAsync(s => s.PublicId == publicId, ct);

            if (software == null) return null;

            return new SoftwareWithPlansDto
            {
                PublicId = software.PublicId,
                Name = software.Name,
                Code = software.Code,
                CurrentVersion = software.CurrentVersion,
                Description = software.Description,
                Icon = software.Icon,
                WebsiteUrl = software.WebsiteUrl,
                DownloadUrl = software.DownloadUrl,
                IsActive = software.IsActive,
                DisplayOrder = software.DisplayOrder,
                ZamanInsert = software.ZamanInsert,
                PlansCount = software.Plans.Count,
                Plans = software.Plans
                    .OrderBy(p => p.DisplayOrder)
                    .Select(p => MapPlanToDto(p, software))
                    .ToList()
            };
        }

        public async Task<SoftwareDto> CreateSoftwareAsync(CreateSoftwareDto dto, long userId, CancellationToken ct = default)
        {
            var software = new tblSoftware
            {
                Name = dto.Name,
                Code = dto.Code,
                CurrentVersion = dto.CurrentVersion,
                Description = dto.Description,
                Icon = dto.Icon,
                WebsiteUrl = dto.WebsiteUrl,
                DownloadUrl = dto.DownloadUrl,
                IsActive = dto.IsActive,
                DisplayOrder = dto.DisplayOrder,
                TblUserGrpIdInsert = userId
            };

            _context.tblSoftwares.Add(software);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("نرم‌افزار جدید ایجاد شد: {Code} - {Name}", dto.Code, dto.Name);

            return (await GetSoftwareByPublicIdAsync(software.PublicId, ct))!;
        }

        public async Task<SoftwareDto?> UpdateSoftwareAsync(UpdateSoftwareDto dto, long userId, CancellationToken ct = default)
        {
            var software = await _context.tblSoftwares.FirstOrDefaultAsync(s => s.PublicId == dto.PublicId, ct);
            if (software == null) return null;

            software.Name = dto.Name;
            software.Code = dto.Code;
            software.CurrentVersion = dto.CurrentVersion;
            software.Description = dto.Description;
            software.Icon = dto.Icon;
            software.WebsiteUrl = dto.WebsiteUrl;
            software.DownloadUrl = dto.DownloadUrl;
            software.IsActive = dto.IsActive;
            software.DisplayOrder = dto.DisplayOrder;
            software.TblUserGrpIdLastEdit = userId;

            await _context.SaveChangesAsync(ct);

            return await GetSoftwareByPublicIdAsync(dto.PublicId, ct);
        }

        public async Task<bool> DeleteSoftwareAsync(Guid publicId, CancellationToken ct = default)
        {
            var software = await _context.tblSoftwares.FirstOrDefaultAsync(s => s.PublicId == publicId, ct);
            if (software == null) return false;

            _context.tblSoftwares.Remove(software);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        // ============================================
        // پلن‌ها (Plan)
        // ============================================

        public async Task<List<PlanDto>> GetPlansAsync(Guid? softwarePublicId = null, bool? isActive = null, CancellationToken ct = default)
        {
            var query = _context.tblPlans
                .Include(p => p.Software)
                .AsQueryable();

            if (softwarePublicId.HasValue)
                query = query.Where(p => p.Software!.PublicId == softwarePublicId.Value);

            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);

            var plans = await query
                .OrderBy(p => p.Software!.Name)
                .ThenBy(p => p.DisplayOrder)
                .ToListAsync(ct);

            return plans.Select(p => MapPlanToDto(p, p.Software!)).ToList();
        }

        public async Task<PlanDto?> GetPlanByPublicIdAsync(Guid publicId, CancellationToken ct = default)
        {
            var plan = await _context.tblPlans
                .Include(p => p.Software)
                .FirstOrDefaultAsync(p => p.PublicId == publicId, ct);

            if (plan == null) return null;

            return MapPlanToDto(plan, plan.Software!);
        }

        public async Task<PlanDto> CreatePlanAsync(CreatePlanDto dto, long userId, CancellationToken ct = default)
        {
            var software = await _context.tblSoftwares.FirstOrDefaultAsync(s => s.PublicId == dto.SoftwarePublicId, ct);
            if (software == null)
                throw new ArgumentException("نرم‌افزار یافت نشد");

            var plan = new tblPlan
            {
                tblSoftwareId = software.Id,
                Name = dto.Name,
                Code = dto.Code,
                Description = dto.Description,
                MaxMemberCount = dto.MaxMemberCount,
                MaxUserCount = dto.MaxUserCount,
                MaxBranchCount = dto.MaxBranchCount,
                MaxDbSizeMB = dto.MaxDbSizeMB,
                MaxDailyTransactions = dto.MaxDailyTransactions,
                FeaturesJson = dto.Features != null ? JsonSerializer.Serialize(dto.Features) : null,
                BasePrice = dto.BasePrice,
                MonthlyPrice = dto.MonthlyPrice,
                YearlyPrice = dto.YearlyPrice,
                PlanType = dto.PlanType,
                IsActive = dto.IsActive,
                IsDefault = dto.IsDefault,
                DisplayOrder = dto.DisplayOrder,
                TblUserGrpIdInsert = userId
            };

            _context.tblPlans.Add(plan);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("پلن جدید ایجاد شد: {Code} برای نرم‌افزار {Software}", dto.Code, software.Name);

            return (await GetPlanByPublicIdAsync(plan.PublicId, ct))!;
        }

        public async Task<PlanDto?> UpdatePlanAsync(UpdatePlanDto dto, long userId, CancellationToken ct = default)
        {
            var plan = await _context.tblPlans.FirstOrDefaultAsync(p => p.PublicId == dto.PublicId, ct);
            if (plan == null) return null;

            plan.Name = dto.Name;
            plan.Code = dto.Code;
            plan.Description = dto.Description;
            plan.MaxMemberCount = dto.MaxMemberCount;
            plan.MaxUserCount = dto.MaxUserCount;
            plan.MaxBranchCount = dto.MaxBranchCount;
            plan.MaxDbSizeMB = dto.MaxDbSizeMB;
            plan.MaxDailyTransactions = dto.MaxDailyTransactions;
            plan.FeaturesJson = dto.Features != null ? JsonSerializer.Serialize(dto.Features) : null;
            plan.BasePrice = dto.BasePrice;
            plan.MonthlyPrice = dto.MonthlyPrice;
            plan.YearlyPrice = dto.YearlyPrice;
            plan.PlanType = dto.PlanType;
            plan.IsActive = dto.IsActive;
            plan.IsDefault = dto.IsDefault;
            plan.DisplayOrder = dto.DisplayOrder;
            plan.TblUserGrpIdLastEdit = userId;

            await _context.SaveChangesAsync(ct);

            return await GetPlanByPublicIdAsync(dto.PublicId, ct);
        }

        public async Task<bool> DeletePlanAsync(Guid publicId, CancellationToken ct = default)
        {
            var plan = await _context.tblPlans.FirstOrDefaultAsync(p => p.PublicId == publicId, ct);
            if (plan == null) return false;

            _context.tblPlans.Remove(plan);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        // ============================================
        // مشتریان (Customer)
        // ============================================

        public async Task<List<CustomerDto>> GetCustomersAsync(int? status = null, string? searchTerm = null, CancellationToken ct = default)
        {
            var query = _context.tblCustomers.AsQueryable();

            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c =>
                    c.Name.Contains(searchTerm) ||
                    c.CustomerCode.Contains(searchTerm) ||
                    (c.Phone != null && c.Phone.Contains(searchTerm)) ||
                    (c.Mobile != null && c.Mobile.Contains(searchTerm)));
            }

            return await query
                .OrderBy(c => c.Name)
                .Select(c => MapCustomerToDto(c))
                .ToListAsync(ct);
        }

        public async Task<CustomerDto?> GetCustomerByPublicIdAsync(Guid publicId, CancellationToken ct = default)
        {
            var customer = await _context.tblCustomers.FirstOrDefaultAsync(c => c.PublicId == publicId, ct);
            if (customer == null) return null;

            return MapCustomerToDto(customer);
        }

        public async Task<CustomerDto?> GetCustomerByCodeAsync(string customerCode, CancellationToken ct = default)
        {
            var customer = await _context.tblCustomers.FirstOrDefaultAsync(c => c.CustomerCode == customerCode, ct);
            if (customer == null) return null;

            return MapCustomerToDto(customer);
        }

        public async Task<CustomerWithSubscriptionsDto?> GetCustomerWithSubscriptionsAsync(Guid publicId, CancellationToken ct = default)
        {
            var customer = await _context.tblCustomers
                .Include(c => c.CustomerSoftwares)
                    .ThenInclude(cs => cs.Software)
                .Include(c => c.CustomerSoftwares)
                    .ThenInclude(cs => cs.Plan)
                .Include(c => c.Contacts)
                .FirstOrDefaultAsync(c => c.PublicId == publicId, ct);

            if (customer == null) return null;

            var dto = new CustomerWithSubscriptionsDto
            {
                PublicId = customer.PublicId,
                Name = customer.Name,
                CustomerCode = customer.CustomerCode,
                CustomerType = customer.CustomerType,
                CustomerTypeName = GetCustomerTypeName(customer.CustomerType),
                NationalId = customer.NationalId,
                RegistrationNumber = customer.RegistrationNumber,
                CompanyNationalId = customer.CompanyNationalId,
                EconomicCode = customer.EconomicCode,
                ManagerName = customer.ManagerName,
                Phone = customer.Phone,
                Mobile = customer.Mobile,
                Fax = customer.Fax,
                Email = customer.Email,
                Website = customer.Website,
                Address = customer.Address,
                PostalCode = customer.PostalCode,
                Province = customer.Province,
                City = customer.City,
                LogoPath = customer.LogoPath,
                Description = customer.Description,
                MembershipDate = customer.MembershipDate,
                Status = customer.Status,
                StatusName = GetCustomerStatusName(customer.Status),
                LoyaltyPoints = customer.LoyaltyPoints,
                CustomerLevel = customer.CustomerLevel,
                CustomerLevelName = GetCustomerLevelName(customer.CustomerLevel),
                ZamanInsert = customer.ZamanInsert,
                SoftwaresCount = customer.CustomerSoftwares.Count,
                ContactsCount = customer.Contacts.Count,
                Subscriptions = customer.CustomerSoftwares
                    .Select(cs => MapCustomerSoftwareToDto(cs))
                    .ToList(),
                Contacts = customer.Contacts
                    .Select(cc => MapCustomerContactToDto(cc, customer))
                    .ToList()
            };

            return dto;
        }

        public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto, long userId, CancellationToken ct = default)
        {
            var customer = new tblCustomer
            {
                Name = dto.Name,
                CustomerCode = dto.CustomerCode,
                CustomerType = dto.CustomerType,
                NationalId = dto.NationalId,
                RegistrationNumber = dto.RegistrationNumber,
                CompanyNationalId = dto.CompanyNationalId,
                EconomicCode = dto.EconomicCode,
                ManagerName = dto.ManagerName,
                Phone = dto.Phone,
                Mobile = dto.Mobile,
                Fax = dto.Fax,
                Email = dto.Email,
                Website = dto.Website,
                Address = dto.Address,
                PostalCode = dto.PostalCode,
                Province = dto.Province,
                City = dto.City,
                Description = dto.Description,
                MembershipDate = BaseEntity.GetNowPersian(),
                Status = dto.Status,
                CustomerLevel = dto.CustomerLevel,
                TblUserGrpIdInsert = userId
            };

            _context.tblCustomers.Add(customer);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("مشتری جدید ایجاد شد: {Code} - {Name}", dto.CustomerCode, dto.Name);

            return (await GetCustomerByPublicIdAsync(customer.PublicId, ct))!;
        }

        public async Task<CustomerDto?> UpdateCustomerAsync(UpdateCustomerDto dto, long userId, CancellationToken ct = default)
        {
            var customer = await _context.tblCustomers.FirstOrDefaultAsync(c => c.PublicId == dto.PublicId, ct);
            if (customer == null) return null;

            customer.Name = dto.Name;
            customer.CustomerCode = dto.CustomerCode;
            customer.CustomerType = dto.CustomerType;
            customer.NationalId = dto.NationalId;
            customer.RegistrationNumber = dto.RegistrationNumber;
            customer.CompanyNationalId = dto.CompanyNationalId;
            customer.EconomicCode = dto.EconomicCode;
            customer.ManagerName = dto.ManagerName;
            customer.Phone = dto.Phone;
            customer.Mobile = dto.Mobile;
            customer.Fax = dto.Fax;
            customer.Email = dto.Email;
            customer.Website = dto.Website;
            customer.Address = dto.Address;
            customer.PostalCode = dto.PostalCode;
            customer.Province = dto.Province;
            customer.City = dto.City;
            customer.Description = dto.Description;
            customer.Status = dto.Status;
            customer.CustomerLevel = dto.CustomerLevel;
            customer.TblUserGrpIdLastEdit = userId;

            await _context.SaveChangesAsync(ct);

            return await GetCustomerByPublicIdAsync(dto.PublicId, ct);
        }

        public async Task<bool> DeleteCustomerAsync(Guid publicId, CancellationToken ct = default)
        {
            var customer = await _context.tblCustomers.FirstOrDefaultAsync(c => c.PublicId == publicId, ct);
            if (customer == null) return false;

            _context.tblCustomers.Remove(customer);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        // ============================================
        // مخاطبین مشتری
        // ============================================

        public async Task<List<CustomerContactDto>> GetCustomerContactsAsync(Guid customerPublicId, CancellationToken ct = default)
        {
            var customer = await _context.tblCustomers
                .Include(c => c.Contacts)
                .FirstOrDefaultAsync(c => c.PublicId == customerPublicId, ct);

            if (customer == null) return new List<CustomerContactDto>();

            return customer.Contacts
                .OrderByDescending(c => c.IsPrimary)
                .ThenBy(c => c.FullName)
                .Select(cc => MapCustomerContactToDto(cc, customer))
                .ToList();
        }

        public async Task<CustomerContactDto> CreateCustomerContactAsync(CreateCustomerContactDto dto, long userId, CancellationToken ct = default)
        {
            var customer = await _context.tblCustomers.FirstOrDefaultAsync(c => c.PublicId == dto.CustomerPublicId, ct);
            if (customer == null)
                throw new ArgumentException("مشتری یافت نشد");

            var contact = new tblCustomerContact
            {
                tblCustomerId = customer.Id,
                FullName = dto.FullName,
                JobTitle = dto.JobTitle,
                ContactType = dto.ContactType,
                Phone = dto.Phone,
                Mobile = dto.Mobile,
                Email = dto.Email,
                Messenger = dto.Messenger,
                Description = dto.Description,
                IsPrimary = dto.IsPrimary,
                IsActive = dto.IsActive,
                TblUserGrpIdInsert = userId
            };

            _context.tblCustomerContacts.Add(contact);
            await _context.SaveChangesAsync(ct);

            return MapCustomerContactToDto(contact, customer);
        }

        public async Task<CustomerContactDto?> UpdateCustomerContactAsync(UpdateCustomerContactDto dto, long userId, CancellationToken ct = default)
        {
            var contact = await _context.tblCustomerContacts
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(c => c.PublicId == dto.PublicId, ct);

            if (contact == null) return null;

            contact.FullName = dto.FullName;
            contact.JobTitle = dto.JobTitle;
            contact.ContactType = dto.ContactType;
            contact.Phone = dto.Phone;
            contact.Mobile = dto.Mobile;
            contact.Email = dto.Email;
            contact.Messenger = dto.Messenger;
            contact.Description = dto.Description;
            contact.IsPrimary = dto.IsPrimary;
            contact.IsActive = dto.IsActive;
            contact.TblUserGrpIdLastEdit = userId;

            await _context.SaveChangesAsync(ct);

            return MapCustomerContactToDto(contact, contact.Customer!);
        }

        public async Task<bool> DeleteCustomerContactAsync(Guid publicId, CancellationToken ct = default)
        {
            var contact = await _context.tblCustomerContacts.FirstOrDefaultAsync(c => c.PublicId == publicId, ct);
            if (contact == null) return false;

            _context.tblCustomerContacts.Remove(contact);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        // ============================================
        // اشتراک مشتری (CustomerSoftware)
        // ============================================

        public async Task<List<CustomerSoftwareDto>> GetCustomerSubscriptionsAsync(Guid customerPublicId, CancellationToken ct = default)
        {
            var subscriptions = await _context.tblCustomerSoftwares
                .Include(cs => cs.Customer)
                .Include(cs => cs.Software)
                .Include(cs => cs.Plan)
                .Where(cs => cs.Customer!.PublicId == customerPublicId)
                .ToListAsync(ct);

            return subscriptions.Select(cs => MapCustomerSoftwareToDto(cs)).ToList();
        }

        public async Task<CustomerSoftwareDto?> GetSubscriptionByPublicIdAsync(Guid publicId, CancellationToken ct = default)
        {
            var subscription = await _context.tblCustomerSoftwares
                .Include(cs => cs.Customer)
                .Include(cs => cs.Software)
                .Include(cs => cs.Plan)
                .FirstOrDefaultAsync(cs => cs.PublicId == publicId, ct);

            if (subscription == null) return null;

            return MapCustomerSoftwareToDto(subscription);
        }

        public async Task<CustomerSoftwareDto?> GetSubscriptionByLicenseKeyAsync(string licenseKey, CancellationToken ct = default)
        {
            var subscription = await _context.tblCustomerSoftwares
                .Include(cs => cs.Customer)
                .Include(cs => cs.Software)
                .Include(cs => cs.Plan)
                .FirstOrDefaultAsync(cs => cs.LicenseKey == licenseKey, ct);

            if (subscription == null) return null;

            return MapCustomerSoftwareToDto(subscription);
        }

        public async Task<CustomerSoftwareDto> CreateSubscriptionAsync(CreateCustomerSoftwareDto dto, long userId, CancellationToken ct = default)
        {
            var customer = await _context.tblCustomers.FirstOrDefaultAsync(c => c.PublicId == dto.CustomerPublicId, ct);
            if (customer == null)
                throw new ArgumentException("مشتری یافت نشد");

            var software = await _context.tblSoftwares.FirstOrDefaultAsync(s => s.PublicId == dto.SoftwarePublicId, ct);
            if (software == null)
                throw new ArgumentException("نرم‌افزار یافت نشد");

            var plan = await _context.tblPlans.FirstOrDefaultAsync(p => p.PublicId == dto.PlanPublicId, ct);
            if (plan == null)
                throw new ArgumentException("پلن یافت نشد");

            // تولید LicenseKey اگر ارسال نشده
            var licenseKey = dto.LicenseKey ?? GenerateLicenseKey(software.Code, customer.CustomerCode);

            var subscription = new tblCustomerSoftware
            {
                tblCustomerId = customer.Id,
                tblSoftwareId = software.Id,
                tblPlanId = plan.Id,
                LicenseKey = licenseKey,
                LicenseCount = dto.LicenseCount,
                StartDate = dto.StartDate ?? BaseEntity.GetNowPersian(),
                EndDate = dto.EndDate,
                SubscriptionType = dto.SubscriptionType,
                Status = dto.Status,
                MaxActivations = dto.MaxActivations,
                Notes = dto.Notes,
                PaidAmount = dto.PaidAmount,
                DiscountPercent = dto.DiscountPercent,
                TblUserGrpIdInsert = userId
            };

            _context.tblCustomerSoftwares.Add(subscription);

            // ایجاد دیتابیس اگر درخواست شده
            if (dto.Database != null)
            {
                var db = new tblDb
                {
                    tblCustomerId = customer.Id,
                    tblSoftwareId = software.Id,
                    Name = dto.Database.Name,
                    DbCode = dto.Database.DbCode,
                    ServerName = dto.Database.ServerName,
                    Port = dto.Database.Port,
                    DatabaseName = dto.Database.DatabaseName,
                    Username = dto.Database.Username,
                    EncryptedPassword = !string.IsNullOrEmpty(dto.Database.Password) ? _databaseConnectionService.EncryptPassword(dto.Database.Password) : null,
                    DbType = dto.Database.DbType,
                    Environment = dto.Database.Environment,
                    IsPrimary = true,
                    Status = 1,
                    TblUserGrpIdInsert = userId
                };

                _context.tblDbs.Add(db);
                await _context.SaveChangesAsync(ct);

                // ارتباط دیتابیس با اشتراک
                var csDb = new tblCustomerSoftwareDb
                {
                    tblCustomerSoftwareId = subscription.Id,
                    tblDbId = db.Id,
                    IsPrimary = true,
                    UsageType = 1,
                    ConnectedDate = BaseEntity.GetNowPersian(),
                    IsActive = true,
                    TblUserGrpIdInsert = userId
                };

                _context.tblCustomerSoftwareDbs.Add(csDb);
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("اشتراک جدید ایجاد شد: {LicenseKey} برای مشتری {Customer}", licenseKey, customer.Name);

            return (await GetSubscriptionByPublicIdAsync(subscription.PublicId, ct))!;
        }

        public async Task<CustomerSoftwareDto?> UpdateSubscriptionAsync(UpdateCustomerSoftwareDto dto, long userId, CancellationToken ct = default)
        {
            var subscription = await _context.tblCustomerSoftwares.FirstOrDefaultAsync(cs => cs.PublicId == dto.PublicId, ct);
            if (subscription == null) return null;

            var plan = await _context.tblPlans.FirstOrDefaultAsync(p => p.PublicId == dto.PlanPublicId, ct);
            if (plan == null)
                throw new ArgumentException("پلن یافت نشد");

            subscription.tblPlanId = plan.Id;
            subscription.LicenseCount = dto.LicenseCount;
            subscription.StartDate = dto.StartDate;
            subscription.EndDate = dto.EndDate;
            subscription.SubscriptionType = dto.SubscriptionType;
            subscription.Status = dto.Status;
            subscription.MaxActivations = dto.MaxActivations;
            subscription.Notes = dto.Notes;
            subscription.PaidAmount = dto.PaidAmount;
            subscription.DiscountPercent = dto.DiscountPercent;
            subscription.TblUserGrpIdLastEdit = userId;

            await _context.SaveChangesAsync(ct);

            return await GetSubscriptionByPublicIdAsync(dto.PublicId, ct);
        }

        public async Task<bool> DeleteSubscriptionAsync(Guid publicId, CancellationToken ct = default)
        {
            var subscription = await _context.tblCustomerSoftwares.FirstOrDefaultAsync(cs => cs.PublicId == publicId, ct);
            if (subscription == null) return false;

            _context.tblCustomerSoftwares.Remove(subscription);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<ActivationResultDto> ActivateLicenseAsync(ActivateLicenseDto dto, CancellationToken ct = default)
        {
            var subscription = await _context.tblCustomerSoftwares
                .Include(cs => cs.Customer)
                .Include(cs => cs.Software)
                .Include(cs => cs.Plan)
                .Include(cs => cs.CustomerSoftwareDbs)
                    .ThenInclude(csdb => csdb.Db)
                .FirstOrDefaultAsync(cs => cs.LicenseKey == dto.LicenseKey, ct);

            if (subscription == null)
            {
                return new ActivationResultDto
                {
                    IsSuccess = false,
                    Message = "لایسنس نامعتبر است"
                };
            }

            // بررسی وضعیت اشتراک
            if (subscription.Status != 1)
            {
                return new ActivationResultDto
                {
                    IsSuccess = false,
                    Message = "اشتراک غیرفعال است"
                };
            }

            // بررسی تاریخ انقضا
            if (!string.IsNullOrEmpty(subscription.EndDate))
            {
                var endDate = BaseEntity.ToGregorianDateTime(subscription.EndDate);
                if (endDate < DateTime.Now)
                {
                    return new ActivationResultDto
                    {
                        IsSuccess = false,
                        Message = "اشتراک منقضی شده است"
                    };
                }
            }

            // بررسی تعداد فعال‌سازی
            if (subscription.MaxActivations.HasValue && subscription.ActivationCount >= subscription.MaxActivations.Value)
            {
                return new ActivationResultDto
                {
                    IsSuccess = false,
                    Message = "تعداد فعال‌سازی به حداکثر رسیده است"
                };
            }

            // به‌روزرسانی اطلاعات فعال‌سازی
            subscription.ActivationCount++;
            subscription.LastActivationDate = BaseEntity.GetNowPersian();
            subscription.LastActivationIp = dto.IpAddress;
            subscription.InstalledVersion = dto.Version;

            await _context.SaveChangesAsync(ct);

            // دریافت Connection String
            string? connectionString = null;
            var primaryDb = subscription.CustomerSoftwareDbs.FirstOrDefault(csdb => csdb.IsPrimary && csdb.IsActive);
            if (primaryDb?.Db != null)
            {
                connectionString = await GetDecryptedConnectionStringAsync(primaryDb.Db.PublicId, ct);
            }

            // دریافت Features
            List<string>? features = null;
            if (!string.IsNullOrEmpty(subscription.Plan?.FeaturesJson))
            {
                features = JsonSerializer.Deserialize<List<string>>(subscription.Plan.FeaturesJson);
            }

            _logger.LogInformation("لایسنس فعال شد: {LicenseKey} از IP {IP}", dto.LicenseKey, dto.IpAddress);

            return new ActivationResultDto
            {
                IsSuccess = true,
                Message = "فعال‌سازی با موفقیت انجام شد",
                Subscription = MapCustomerSoftwareToDto(subscription),
                ConnectionString = connectionString,
                MaxMemberCount = subscription.Plan?.MaxMemberCount,
                MaxUserCount = subscription.Plan?.MaxUserCount,
                MaxBranchCount = subscription.Plan?.MaxBranchCount,
                Features = features
            };
        }

        public async Task<bool> UpdateUsedCountAsync(Guid subscriptionPublicId, int usedCount, CancellationToken ct = default)
        {
            var subscription = await _context.tblCustomerSoftwares.FirstOrDefaultAsync(cs => cs.PublicId == subscriptionPublicId, ct);
            if (subscription == null) return false;

            subscription.UsedCount = usedCount;
            await _context.SaveChangesAsync(ct);

            return true;
        }

        // ============================================
        // دیتابیس‌ها (Db)
        // ============================================

        public async Task<List<DbDto>> GetDatabasesAsync(Guid? softwarePublicId = null, Guid? customerPublicId = null, int? status = null, CancellationToken ct = default)
        {
            var query = _context.tblDbs
                .Include(d => d.Software)
                .Include(d => d.Customer)
                .AsQueryable();

            if (softwarePublicId.HasValue)
                query = query.Where(d => d.Software!.PublicId == softwarePublicId.Value);

            if (customerPublicId.HasValue)
                query = query.Where(d => d.Customer!.PublicId == customerPublicId.Value);

            if (status.HasValue)
                query = query.Where(d => d.Status == status.Value);

            var dbs = await query
                .OrderBy(d => d.DisplayOrder)
                .ThenBy(d => d.Name)
                .ToListAsync(ct);

            return dbs.Select(d => MapDbToDto(d)).ToList();
        }

        public async Task<DbDto?> GetDatabaseByPublicIdAsync(Guid publicId, CancellationToken ct = default)
        {
            var db = await _context.tblDbs
                .Include(d => d.Software)
                .Include(d => d.Customer)
                .FirstOrDefaultAsync(d => d.PublicId == publicId, ct);

            if (db == null) return null;

            return MapDbToDto(db);
        }

        public async Task<DbDto?> GetDatabaseByCodeAsync(string dbCode, CancellationToken ct = default)
        {
            var db = await _context.tblDbs
                .Include(d => d.Software)
                .Include(d => d.Customer)
                .FirstOrDefaultAsync(d => d.DbCode == dbCode, ct);

            if (db == null) return null;

            return MapDbToDto(db);
        }

        public async Task<DbDto> CreateDatabaseAsync(CreateDbDto dto, long userId, CancellationToken ct = default)
        {
            var software = await _context.tblSoftwares.FirstOrDefaultAsync(s => s.PublicId == dto.SoftwarePublicId, ct);
            if (software == null)
                throw new ArgumentException("نرم‌افزار یافت نشد");

            long? customerId = null;
            if (dto.CustomerPublicId.HasValue)
            {
                var customer = await _context.tblCustomers.FirstOrDefaultAsync(c => c.PublicId == dto.CustomerPublicId.Value, ct);
                if (customer == null)
                    throw new ArgumentException("مشتری یافت نشد");
                customerId = customer.Id;
            }

            var db = new tblDb
            {
                tblCustomerId = customerId,
                tblSoftwareId = software.Id,
                Name = dto.Name,
                DbCode = dto.DbCode,
                ServerName = dto.ServerName,
                Port = dto.Port,
                DatabaseName = dto.DatabaseName,
                Username = dto.Username,
                EncryptedPassword = !string.IsNullOrEmpty(dto.Password) ? _databaseConnectionService.EncryptPassword(dto.Password) : null,
                EncryptedConnectionString = !string.IsNullOrEmpty(dto.ConnectionString) ? _databaseConnectionService.EncryptConnectionString(dto.ConnectionString) : null,
                DbType = dto.DbType,
                Environment = dto.Environment,
                IsShared = dto.IsShared,
                TenantId = dto.TenantId,
                IsPrimary = dto.IsPrimary,
                IsReadOnly = dto.IsReadOnly,
                MaxSizeMB = dto.MaxSizeMB,
                Description = dto.Description,
                Status = dto.Status,
                DisplayOrder = dto.DisplayOrder,
                TblUserGrpIdInsert = userId
            };

            _context.tblDbs.Add(db);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("دیتابیس جدید ایجاد شد: {DbCode} - {Name}", dto.DbCode, dto.Name);

            return (await GetDatabaseByPublicIdAsync(db.PublicId, ct))!;
        }

        public async Task<DbDto?> UpdateDatabaseAsync(UpdateDbDto dto, long userId, CancellationToken ct = default)
        {
            var db = await _context.tblDbs.FirstOrDefaultAsync(d => d.PublicId == dto.PublicId, ct);
            if (db == null) return null;

            db.Name = dto.Name;
            db.DbCode = dto.DbCode;
            db.ServerName = dto.ServerName;
            db.Port = dto.Port;
            db.DatabaseName = dto.DatabaseName;
            db.Username = dto.Username;

            if (!string.IsNullOrEmpty(dto.Password))
                db.EncryptedPassword = _databaseConnectionService.EncryptPassword(dto.Password);

            if (!string.IsNullOrEmpty(dto.ConnectionString))
                db.EncryptedConnectionString = _databaseConnectionService.EncryptConnectionString(dto.ConnectionString);

            db.DbType = dto.DbType;
            db.Environment = dto.Environment;
            db.IsShared = dto.IsShared;
            db.TenantId = dto.TenantId;
            db.IsPrimary = dto.IsPrimary;
            db.IsReadOnly = dto.IsReadOnly;
            db.MaxSizeMB = dto.MaxSizeMB;
            db.Description = dto.Description;
            db.Status = dto.Status;
            db.DisplayOrder = dto.DisplayOrder;
            db.TblUserGrpIdLastEdit = userId;

            await _context.SaveChangesAsync(ct);

            return await GetDatabaseByPublicIdAsync(dto.PublicId, ct);
        }

        public async Task<bool> DeleteDatabaseAsync(Guid publicId, CancellationToken ct = default)
        {
            var db = await _context.tblDbs.FirstOrDefaultAsync(d => d.PublicId == publicId, ct);
            if (db == null) return false;

            _context.tblDbs.Remove(db);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<DbConnectionTestResultDto> TestDatabaseConnectionAsync(TestDbConnectionDto dto, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                var connectionString = BuildConnectionString(dto.ServerName, dto.Port, dto.DatabaseName, dto.Username, dto.Password, dto.DbType);

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(ct);

                var serverVersion = connection.ServerVersion;
                sw.Stop();

                return new DbConnectionTestResultDto
                {
                    IsSuccess = true,
                    Message = "اتصال موفق",
                    ResponseTimeMs = (int)sw.ElapsedMilliseconds,
                    ServerVersion = serverVersion
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogWarning(ex, "خطا در تست اتصال دیتابیس");

                return new DbConnectionTestResultDto
                {
                    IsSuccess = false,
                    Message = $"خطا در اتصال: {ex.Message}",
                    ResponseTimeMs = (int)sw.ElapsedMilliseconds
                };
            }
        }

        public async Task<DbConnectionTestResultDto> TestExistingDatabaseConnectionAsync(Guid publicId, CancellationToken ct = default)
        {
            var db = await _context.tblDbs.FirstOrDefaultAsync(d => d.PublicId == publicId, ct);
            if (db == null)
            {
                return new DbConnectionTestResultDto
                {
                    IsSuccess = false,
                    Message = "دیتابیس یافت نشد"
                };
            }

            var password = !string.IsNullOrEmpty(db.EncryptedPassword) ? _databaseConnectionService.DecryptPassword(db.EncryptedPassword) : null;

            var result = await TestDatabaseConnectionAsync(new TestDbConnectionDto
            {
                ServerName = db.ServerName,
                Port = db.Port,
                DatabaseName = db.DatabaseName,
                Username = db.Username,
                Password = password,
                DbType = db.DbType
            }, ct);

            // به‌روزرسانی نتیجه تست
            db.LastConnectionTestDate = BaseEntity.GetNowPersian();
            db.LastConnectionTestResult = result.IsSuccess;
            await _context.SaveChangesAsync(ct);

            return result;
        }

        public async Task<string?> GetDecryptedConnectionStringAsync(Guid dbPublicId, CancellationToken ct = default)
        {
            var db = await _context.tblDbs.FirstOrDefaultAsync(d => d.PublicId == dbPublicId, ct);
            if (db == null) return null;

            // اگر Connection String ذخیره شده، آن را برگردان
            if (!string.IsNullOrEmpty(db.EncryptedConnectionString))
            {
                return _databaseConnectionService.DecryptConnectionString(db.EncryptedConnectionString);
            }

            // در غیر این صورت، بساز
            var password = !string.IsNullOrEmpty(db.EncryptedPassword) ? _databaseConnectionService.DecryptPassword(db.EncryptedPassword) : null;
            return BuildConnectionString(db.ServerName, db.Port, db.DatabaseName, db.Username, password, db.DbType);
        }

        // ============================================
        // آمار و گزارش
        // ============================================

        public async Task<ManagementDashboardDto> GetDashboardStatsAsync(CancellationToken ct = default)
        {
            var softwares = await _context.tblSoftwares.ToListAsync(ct);
            var plans = await _context.tblPlans.CountAsync(ct);
            var customers = await _context.tblCustomers.ToListAsync(ct);
            var subscriptions = await _context.tblCustomerSoftwares.ToListAsync(ct);
            var databases = await _context.tblDbs.ToListAsync(ct);

            var softwareStats = await _context.tblSoftwares
                .Select(s => new SoftwareStatsDto
                {
                    PublicId = s.PublicId,
                    Name = s.Name,
                    CustomersCount = s.CustomerSoftwares.Select(cs => cs.tblCustomerId).Distinct().Count(),
                    ActiveSubscriptions = s.CustomerSoftwares.Count(cs => cs.Status == 1),
                    TotalLicenses = s.CustomerSoftwares.Sum(cs => cs.LicenseCount)
                })
                .ToListAsync(ct);

            return new ManagementDashboardDto
            {
                TotalSoftwares = softwares.Count,
                ActiveSoftwares = softwares.Count(s => s.IsActive),
                TotalPlans = plans,
                TotalCustomers = customers.Count,
                ActiveCustomers = customers.Count(c => c.Status == 1),
                TotalSubscriptions = subscriptions.Count,
                ActiveSubscriptions = subscriptions.Count(s => s.Status == 1),
                ExpiredSubscriptions = subscriptions.Count(s => s.Status == 4),
                TotalDatabases = databases.Count,
                ActiveDatabases = databases.Count(d => d.Status == 1),
                SoftwareStats = softwareStats
            };
        }

        // ============================================
        // Helper Methods
        // ============================================

        private static string GenerateLicenseKey(string softwareCode, string customerCode)
        {
            var timestamp = DateTime.Now.ToString("yyMMddHHmm");
            var random = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            return $"LIC-{softwareCode}-{customerCode}-{timestamp}-{random}";
        }

        private string BuildConnectionString(string server, int? port, string database, string? username, string? password, int dbType)
        {
            if (dbType == 1) // SQL Server
            {
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = port.HasValue ? $"{server},{port}" : server,
                    InitialCatalog = database,
                    TrustServerCertificate = true
                };

                if (!string.IsNullOrEmpty(username))
                {
                    builder.UserID = username;
                    builder.Password = password;
                }
                else
                {
                    builder.IntegratedSecurity = true;
                }

                return builder.ConnectionString;
            }

            throw new NotSupportedException($"نوع دیتابیس {dbType} پشتیبانی نمی‌شود");
        }

        // ============================================
        // Mapping Methods
        // ============================================

        private static PlanDto MapPlanToDto(tblPlan plan, tblSoftware software)
        {
            List<string>? features = null;
            if (!string.IsNullOrEmpty(plan.FeaturesJson))
            {
                try
                {
                    features = JsonSerializer.Deserialize<List<string>>(plan.FeaturesJson);
                }
                catch { }
            }

            return new PlanDto
            {
                PublicId = plan.PublicId,
                SoftwarePublicId = software.PublicId,
                SoftwareName = software.Name,
                Name = plan.Name,
                Code = plan.Code,
                Description = plan.Description,
                MaxMemberCount = plan.MaxMemberCount,
                MaxUserCount = plan.MaxUserCount,
                MaxBranchCount = plan.MaxBranchCount,
                MaxDbSizeMB = plan.MaxDbSizeMB,
                MaxDailyTransactions = plan.MaxDailyTransactions,
                Features = features,
                BasePrice = plan.BasePrice,
                MonthlyPrice = plan.MonthlyPrice,
                YearlyPrice = plan.YearlyPrice,
                PlanType = plan.PlanType,
                PlanTypeName = GetPlanTypeName(plan.PlanType),
                IsActive = plan.IsActive,
                IsDefault = plan.IsDefault,
                DisplayOrder = plan.DisplayOrder,
                ZamanInsert = plan.ZamanInsert
            };
        }

        private static CustomerDto MapCustomerToDto(tblCustomer customer)
        {
            return new CustomerDto
            {
                PublicId = customer.PublicId,
                Name = customer.Name,
                CustomerCode = customer.CustomerCode,
                CustomerType = customer.CustomerType,
                CustomerTypeName = GetCustomerTypeName(customer.CustomerType),
                NationalId = customer.NationalId,
                RegistrationNumber = customer.RegistrationNumber,
                CompanyNationalId = customer.CompanyNationalId,
                EconomicCode = customer.EconomicCode,
                ManagerName = customer.ManagerName,
                Phone = customer.Phone,
                Mobile = customer.Mobile,
                Fax = customer.Fax,
                Email = customer.Email,
                Website = customer.Website,
                Address = customer.Address,
                PostalCode = customer.PostalCode,
                Province = customer.Province,
                City = customer.City,
                LogoPath = customer.LogoPath,
                Description = customer.Description,
                MembershipDate = customer.MembershipDate,
                Status = customer.Status,
                StatusName = GetCustomerStatusName(customer.Status),
                LoyaltyPoints = customer.LoyaltyPoints,
                CustomerLevel = customer.CustomerLevel,
                CustomerLevelName = GetCustomerLevelName(customer.CustomerLevel),
                ZamanInsert = customer.ZamanInsert,
                SoftwaresCount = customer.CustomerSoftwares?.Count ?? 0,
                ContactsCount = customer.Contacts?.Count ?? 0
            };
        }

        private static CustomerContactDto MapCustomerContactToDto(tblCustomerContact contact, tblCustomer customer)
        {
            return new CustomerContactDto
            {
                PublicId = contact.PublicId,
                CustomerPublicId = customer.PublicId,
                CustomerName = customer.Name,
                FullName = contact.FullName,
                JobTitle = contact.JobTitle,
                ContactType = contact.ContactType,
                ContactTypeName = GetContactTypeName(contact.ContactType),
                Phone = contact.Phone,
                Mobile = contact.Mobile,
                Email = contact.Email,
                Messenger = contact.Messenger,
                Description = contact.Description,
                IsPrimary = contact.IsPrimary,
                IsActive = contact.IsActive,
                ZamanInsert = contact.ZamanInsert
            };
        }

        private static CustomerSoftwareDto MapCustomerSoftwareToDto(tblCustomerSoftware cs)
        {
            return new CustomerSoftwareDto
            {
                PublicId = cs.PublicId,
                CustomerPublicId = cs.Customer!.PublicId,
                CustomerName = cs.Customer.Name,
                CustomerCode = cs.Customer.CustomerCode,
                SoftwarePublicId = cs.Software!.PublicId,
                SoftwareName = cs.Software.Name,
                SoftwareCode = cs.Software.Code,
                PlanPublicId = cs.Plan!.PublicId,
                PlanName = cs.Plan.Name,
                PlanCode = cs.Plan.Code,
                LicenseKey = cs.LicenseKey,
                LicenseCount = cs.LicenseCount,
                UsedCount = cs.UsedCount,
                StartDate = cs.StartDate,
                EndDate = cs.EndDate,
                SubscriptionType = cs.SubscriptionType,
                SubscriptionTypeName = GetSubscriptionTypeName(cs.SubscriptionType),
                Status = cs.Status,
                StatusName = GetSubscriptionStatusName(cs.Status),
                InstalledVersion = cs.InstalledVersion,
                LastActivationDate = cs.LastActivationDate,
                LastActivationIp = cs.LastActivationIp,
                ActivationCount = cs.ActivationCount,
                MaxActivations = cs.MaxActivations,
                Notes = cs.Notes,
                PaidAmount = cs.PaidAmount,
                DiscountPercent = cs.DiscountPercent,
                ZamanInsert = cs.ZamanInsert,
                MaxMemberCount = cs.Plan.MaxMemberCount,
                MaxUserCount = cs.Plan.MaxUserCount,
                MaxBranchCount = cs.Plan.MaxBranchCount
            };
        }

        private static DbDto MapDbToDto(tblDb db)
        {
            return new DbDto
            {
                PublicId = db.PublicId,
                CustomerPublicId = db.Customer?.PublicId,
                CustomerName = db.Customer?.Name,
                SoftwarePublicId = db.Software!.PublicId,
                SoftwareName = db.Software.Name,
                Name = db.Name,
                DbCode = db.DbCode,
                ServerName = db.ServerName,
                Port = db.Port,
                DatabaseName = db.DatabaseName,
                Username = db.Username,
                DbType = db.DbType,
                DbTypeName = GetDbTypeName(db.DbType),
                Environment = db.Environment,
                EnvironmentName = GetEnvironmentName(db.Environment),
                IsShared = db.IsShared,
                TenantId = db.TenantId,
                IsPrimary = db.IsPrimary,
                IsReadOnly = db.IsReadOnly,
                MaxSizeMB = db.MaxSizeMB,
                CurrentSizeMB = db.CurrentSizeMB,
                LastBackupDate = db.LastBackupDate,
                LastConnectionTestDate = db.LastConnectionTestDate,
                LastConnectionTestResult = db.LastConnectionTestResult,
                Description = db.Description,
                Status = db.Status,
                StatusName = GetDbStatusName(db.Status),
                DisplayOrder = db.DisplayOrder,
                ZamanInsert = db.ZamanInsert,
                SubscriptionsCount = db.CustomerSoftwareDbs?.Count ?? 0
            };
        }

        // ============================================
        // Name Helpers
        // ============================================

        private static string GetPlanTypeName(int planType) => planType switch
        {
            1 => "تک‌کاربره",
            2 => "چندکاربره",
            3 => "سازمانی",
            _ => "نامشخص"
        };

        private static string GetCustomerTypeName(int customerType) => customerType switch
        {
            1 => "حقیقی",
            2 => "حقوقی",
            _ => "نامشخص"
        };

        private static string GetCustomerStatusName(int status) => status switch
        {
            1 => "فعال",
            2 => "غیرفعال",
            3 => "تعلیق",
            4 => "منقضی",
            _ => "نامشخص"
        };

        private static string GetCustomerLevelName(int level) => level switch
        {
            1 => "عادی",
            2 => "نقره‌ای",
            3 => "طلایی",
            4 => "الماسی",
            _ => "نامشخص"
        };

        private static string GetContactTypeName(int contactType) => contactType switch
        {
            1 => "اصلی",
            2 => "فنی",
            3 => "مالی",
            4 => "مدیریتی",
            _ => "نامشخص"
        };

        private static string GetSubscriptionTypeName(int subscriptionType) => subscriptionType switch
        {
            1 => "دائمی",
            2 => "ماهانه",
            3 => "سالانه",
            _ => "نامشخص"
        };

        private static string GetSubscriptionStatusName(int status) => status switch
        {
            1 => "فعال",
            2 => "غیرفعال",
            3 => "تعلیق",
            4 => "منقضی",
            _ => "نامشخص"
        };

        private static string GetDbTypeName(int dbType) => dbType switch
        {
            1 => "SQL Server",
            2 => "MySQL",
            3 => "PostgreSQL",
            4 => "Oracle",
            5 => "SQLite",
            _ => "نامشخص"
        };

        private static string GetEnvironmentName(int env) => env switch
        {
            1 => "Development",
            2 => "Test",
            3 => "Staging",
            4 => "Production",
            _ => "نامشخص"
        };

        private static string GetDbStatusName(int status) => status switch
        {
            1 => "فعال",
            2 => "غیرفعال",
            3 => "در حال نگهداری",
            _ => "نامشخص"
        };
    }
}

