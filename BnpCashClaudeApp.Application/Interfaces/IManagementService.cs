using BnpCashClaudeApp.Application.DTOs.ManagementDtos;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// اینترفیس سرویس راهبری سیستم
    /// مدیریت نرم‌افزارها، پلن‌ها، مشتریان و دیتابیس‌ها
    /// </summary>
    public interface IManagementService
    {
        // ============================================
        // نرم‌افزارها (Software)
        // ============================================

        /// <summary>
        /// دریافت لیست نرم‌افزارها
        /// </summary>
        Task<List<SoftwareDto>> GetSoftwaresAsync(bool? isActive = null, CancellationToken ct = default);

        /// <summary>
        /// دریافت نرم‌افزار با PublicId
        /// </summary>
        Task<SoftwareDto?> GetSoftwareByPublicIdAsync(Guid publicId, CancellationToken ct = default);

        /// <summary>
        /// دریافت نرم‌افزار با کد
        /// </summary>
        Task<SoftwareDto?> GetSoftwareByCodeAsync(string code, CancellationToken ct = default);

        /// <summary>
        /// دریافت نرم‌افزار با پلن‌ها
        /// </summary>
        Task<SoftwareWithPlansDto?> GetSoftwareWithPlansAsync(Guid publicId, CancellationToken ct = default);

        /// <summary>
        /// ایجاد نرم‌افزار جدید
        /// </summary>
        Task<SoftwareDto> CreateSoftwareAsync(CreateSoftwareDto dto, long userId, CancellationToken ct = default);

        /// <summary>
        /// ویرایش نرم‌افزار
        /// </summary>
        Task<SoftwareDto?> UpdateSoftwareAsync(UpdateSoftwareDto dto, long userId, CancellationToken ct = default);

        /// <summary>
        /// حذف نرم‌افزار
        /// </summary>
        Task<bool> DeleteSoftwareAsync(Guid publicId, CancellationToken ct = default);

        // ============================================
        // پلن‌ها (Plan)
        // ============================================

        /// <summary>
        /// دریافت لیست پلن‌ها
        /// </summary>
        Task<List<PlanDto>> GetPlansAsync(Guid? softwarePublicId = null, bool? isActive = null, CancellationToken ct = default);

        /// <summary>
        /// دریافت پلن با PublicId
        /// </summary>
        Task<PlanDto?> GetPlanByPublicIdAsync(Guid publicId, CancellationToken ct = default);

        /// <summary>
        /// ایجاد پلن جدید
        /// </summary>
        Task<PlanDto> CreatePlanAsync(CreatePlanDto dto, long userId, CancellationToken ct = default);

        /// <summary>
        /// ویرایش پلن
        /// </summary>
        Task<PlanDto?> UpdatePlanAsync(UpdatePlanDto dto, long userId, CancellationToken ct = default);

        /// <summary>
        /// حذف پلن
        /// </summary>
        Task<bool> DeletePlanAsync(Guid publicId, CancellationToken ct = default);

        // ============================================
        // مشتریان (Customer)
        // ============================================

        /// <summary>
        /// دریافت لیست مشتریان
        /// </summary>
        Task<List<CustomerDto>> GetCustomersAsync(int? status = null, string? searchTerm = null, CancellationToken ct = default);

        /// <summary>
        /// دریافت مشتری با PublicId
        /// </summary>
        Task<CustomerDto?> GetCustomerByPublicIdAsync(Guid publicId, CancellationToken ct = default);

        /// <summary>
        /// دریافت مشتری با کد
        /// </summary>
        Task<CustomerDto?> GetCustomerByCodeAsync(string customerCode, CancellationToken ct = default);

        /// <summary>
        /// دریافت مشتری با اشتراک‌ها
        /// </summary>
        Task<CustomerWithSubscriptionsDto?> GetCustomerWithSubscriptionsAsync(Guid publicId, CancellationToken ct = default);

        /// <summary>
        /// ایجاد مشتری جدید
        /// </summary>
        Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto, long userId, CancellationToken ct = default);

        /// <summary>
        /// ویرایش مشتری
        /// </summary>
        Task<CustomerDto?> UpdateCustomerAsync(UpdateCustomerDto dto, long userId, CancellationToken ct = default);

        /// <summary>
        /// حذف مشتری
        /// </summary>
        Task<bool> DeleteCustomerAsync(Guid publicId, CancellationToken ct = default);

        // ============================================
        // مخاطبین مشتری (CustomerContact)
        // ============================================

        /// <summary>
        /// دریافت مخاطبین مشتری
        /// </summary>
        Task<List<CustomerContactDto>> GetCustomerContactsAsync(Guid customerPublicId, CancellationToken ct = default);

        /// <summary>
        /// ایجاد مخاطب جدید
        /// </summary>
        Task<CustomerContactDto> CreateCustomerContactAsync(CreateCustomerContactDto dto, long userId, CancellationToken ct = default);

        /// <summary>
        /// ویرایش مخاطب
        /// </summary>
        Task<CustomerContactDto?> UpdateCustomerContactAsync(UpdateCustomerContactDto dto, long userId, CancellationToken ct = default);

        /// <summary>
        /// حذف مخاطب
        /// </summary>
        Task<bool> DeleteCustomerContactAsync(Guid publicId, CancellationToken ct = default);

        // ============================================
        // اشتراک مشتری (CustomerSoftware)
        // ============================================

        /// <summary>
        /// دریافت اشتراک‌های مشتری
        /// </summary>
        Task<List<CustomerSoftwareDto>> GetCustomerSubscriptionsAsync(Guid customerPublicId, CancellationToken ct = default);

        /// <summary>
        /// دریافت اشتراک با PublicId
        /// </summary>
        Task<CustomerSoftwareDto?> GetSubscriptionByPublicIdAsync(Guid publicId, CancellationToken ct = default);

        /// <summary>
        /// دریافت اشتراک با LicenseKey
        /// </summary>
        Task<CustomerSoftwareDto?> GetSubscriptionByLicenseKeyAsync(string licenseKey, CancellationToken ct = default);

        /// <summary>
        /// ایجاد اشتراک جدید
        /// </summary>
        Task<CustomerSoftwareDto> CreateSubscriptionAsync(CreateCustomerSoftwareDto dto, long userId, CancellationToken ct = default);

        /// <summary>
        /// ویرایش اشتراک
        /// </summary>
        Task<CustomerSoftwareDto?> UpdateSubscriptionAsync(UpdateCustomerSoftwareDto dto, long userId, CancellationToken ct = default);

        /// <summary>
        /// حذف اشتراک
        /// </summary>
        Task<bool> DeleteSubscriptionAsync(Guid publicId, CancellationToken ct = default);

        /// <summary>
        /// فعال‌سازی لایسنس
        /// </summary>
        Task<ActivationResultDto> ActivateLicenseAsync(ActivateLicenseDto dto, CancellationToken ct = default);

        /// <summary>
        /// به‌روزرسانی تعداد مصرف شده
        /// </summary>
        Task<bool> UpdateUsedCountAsync(Guid subscriptionPublicId, int usedCount, CancellationToken ct = default);

        // ============================================
        // دیتابیس‌ها (Db)
        // ============================================

        /// <summary>
        /// دریافت لیست دیتابیس‌ها
        /// </summary>
        Task<List<DbDto>> GetDatabasesAsync(Guid? softwarePublicId = null, Guid? customerPublicId = null, int? status = null, CancellationToken ct = default);

        /// <summary>
        /// دریافت دیتابیس با PublicId
        /// </summary>
        Task<DbDto?> GetDatabaseByPublicIdAsync(Guid publicId, CancellationToken ct = default);

        /// <summary>
        /// دریافت دیتابیس با کد
        /// </summary>
        Task<DbDto?> GetDatabaseByCodeAsync(string dbCode, CancellationToken ct = default);

        /// <summary>
        /// ایجاد دیتابیس جدید
        /// </summary>
        Task<DbDto> CreateDatabaseAsync(CreateDbDto dto, long userId, CancellationToken ct = default);

        /// <summary>
        /// ویرایش دیتابیس
        /// </summary>
        Task<DbDto?> UpdateDatabaseAsync(UpdateDbDto dto, long userId, CancellationToken ct = default);

        /// <summary>
        /// حذف دیتابیس
        /// </summary>
        Task<bool> DeleteDatabaseAsync(Guid publicId, CancellationToken ct = default);

        /// <summary>
        /// تست اتصال دیتابیس
        /// </summary>
        Task<DbConnectionTestResultDto> TestDatabaseConnectionAsync(TestDbConnectionDto dto, CancellationToken ct = default);

        /// <summary>
        /// تست اتصال دیتابیس موجود
        /// </summary>
        Task<DbConnectionTestResultDto> TestExistingDatabaseConnectionAsync(Guid publicId, CancellationToken ct = default);

        /// <summary>
        /// دریافت Connection String رمزگشایی شده
        /// </summary>
        Task<string?> GetDecryptedConnectionStringAsync(Guid dbPublicId, CancellationToken ct = default);

        // ============================================
        // آمار و گزارش
        // ============================================

        /// <summary>
        /// دریافت آمار کلی راهبری
        /// </summary>
        Task<ManagementDashboardDto> GetDashboardStatsAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// DTO برای داشبورد راهبری
    /// </summary>
    public class ManagementDashboardDto
    {
        public int TotalSoftwares { get; set; }
        public int ActiveSoftwares { get; set; }
        public int TotalPlans { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int ExpiredSubscriptions { get; set; }
        public int TotalDatabases { get; set; }
        public int ActiveDatabases { get; set; }
        public List<SoftwareStatsDto> SoftwareStats { get; set; } = new List<SoftwareStatsDto>();
    }

    /// <summary>
    /// آمار هر نرم‌افزار
    /// </summary>
    public class SoftwareStatsDto
    {
        public Guid PublicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CustomersCount { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int TotalLicenses { get; set; }
    }
}
