using BnpCashClaudeApp.api.Attributes;
using BnpCashClaudeApp.api.Helpers;
using BnpCashClaudeApp.Application.DTOs.ManagementDtos;
using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BnpCashClaudeApp.api.Controllers.Management
{
    /// <summary>
    /// کنترلر راهبری سیستم
    /// مدیریت نرم‌افزارها، پلن‌ها، مشتریان و دیتابیس‌ها
    /// این کنترلر برای همه نرم‌افزارهای تولیدی مشترک است
    /// </summary>
    //[ApiController]
    //[Route("api/[controller]")]
    //[EnableRateLimiting("ApiPolicy")]
    //[Authorize]
    //public class ManagementController : ControllerBase
    //{
    //    private readonly IManagementService _managementService;
    //    private readonly IAuditLogService _auditLogService;
    //    private readonly ILogger<ManagementController> _logger;

    //    public ManagementController(
    //        IManagementService managementService,
    //        IAuditLogService auditLogService,
    //        ILogger<ManagementController> logger)
    //    {
    //        _managementService = managementService;
    //        _auditLogService = auditLogService;
    //        _logger = logger;
    //    }

    //    private long GetUserId() => long.Parse(User.FindFirst("UserId")?.Value ?? "0");
    //    private string GetUserName() => User.Identity?.Name ?? "Unknown";

    //    // ============================================
    //    // داشبورد و آمار
    //    // ============================================

    //    /// <summary>
    //    /// دریافت آمار کلی داشبورد راهبری
    //    /// </summary>
    //    [HttpGet("dashboard")]
    //    [RequirePermission("Management.Dashboard.Read")]
    //    [ProducesResponseType(typeof(ManagementDashboardDto), 200)]
    //    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.GetDashboardStatsAsync(ct);
    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در دریافت داشبورد راهبری");
    //            return StatusCode(500, new { error = "خطا در دریافت اطلاعات داشبورد" });
    //        }
    //    }

    //    // ============================================
    //    // نرم‌افزارها (Software)
    //    // ============================================

    //    /// <summary>
    //    /// دریافت لیست نرم‌افزارها
    //    /// </summary>
    //    [HttpGet("softwares")]
    //    [RequirePermission("Management.Software.Read")]
    //    [ProducesResponseType(typeof(List<SoftwareDto>), 200)]
    //    public async Task<IActionResult> GetSoftwares([FromQuery] bool? isActive, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.GetSoftwaresAsync(isActive, ct);
    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در دریافت لیست نرم‌افزارها");
    //            return StatusCode(500, new { error = "خطا در دریافت لیست نرم‌افزارها" });
    //        }
    //    }

    //    /// <summary>
    //    /// دریافت نرم‌افزار با شناسه
    //    /// </summary>
    //    [HttpGet("softwares/{publicId:guid}")]
    //    [RequirePermission("Management.Software.Read")]
    //    [ProducesResponseType(typeof(SoftwareDto), 200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> GetSoftware(Guid publicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.GetSoftwareByPublicIdAsync(publicId, ct);
    //            if (result == null)
    //                return NotFound(new { error = "نرم‌افزار یافت نشد" });

    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در دریافت نرم‌افزار");
    //            return StatusCode(500, new { error = "خطا در دریافت نرم‌افزار" });
    //        }
    //    }

    //    /// <summary>
    //    /// دریافت نرم‌افزار با پلن‌ها
    //    /// </summary>
    //    [HttpGet("softwares/{publicId:guid}/with-plans")]
    //    [RequirePermission("Management.Software.Read")]
    //    [ProducesResponseType(typeof(SoftwareWithPlansDto), 200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> GetSoftwareWithPlans(Guid publicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.GetSoftwareWithPlansAsync(publicId, ct);
    //            if (result == null)
    //                return NotFound(new { error = "نرم‌افزار یافت نشد" });

    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در دریافت نرم‌افزار با پلن‌ها");
    //            return StatusCode(500, new { error = "خطا در دریافت نرم‌افزار" });
    //        }
    //    }

    //    /// <summary>
    //    /// ایجاد نرم‌افزار جدید
    //    /// </summary>
    //    [HttpPost("softwares")]
    //    [RequirePermission("Management.Software.Create")]
    //    [ProducesResponseType(typeof(SoftwareDto), 201)]
    //    [ProducesResponseType(400)]
    //    public async Task<IActionResult> CreateSoftware([FromBody] CreateSoftwareDto dto, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.CreateSoftwareAsync(dto, GetUserId(), ct);

    //            await LogAuditAsync("SoftwareCreated", "Software", result.PublicId.ToString(),
    //                $"نرم‌افزار جدید ایجاد شد: {dto.Name}");

    //            return CreatedAtAction(nameof(GetSoftware), new { publicId = result.PublicId }, result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در ایجاد نرم‌افزار");
    //            return StatusCode(500, new { error = "خطا در ایجاد نرم‌افزار" });
    //        }
    //    }

    //    /// <summary>
    //    /// ویرایش نرم‌افزار
    //    /// </summary>
    //    [HttpPut("softwares")]
    //    [RequirePermission("Management.Software.Update")]
    //    [ProducesResponseType(typeof(SoftwareDto), 200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> UpdateSoftware([FromBody] UpdateSoftwareDto dto, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.UpdateSoftwareAsync(dto, GetUserId(), ct);
    //            if (result == null)
    //                return NotFound(new { error = "نرم‌افزار یافت نشد" });

    //            await LogAuditAsync("SoftwareUpdated", "Software", dto.PublicId.ToString(),
    //                $"نرم‌افزار ویرایش شد: {dto.Name}");

    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در ویرایش نرم‌افزار");
    //            return StatusCode(500, new { error = "خطا در ویرایش نرم‌افزار" });
    //        }
    //    }

    //    /// <summary>
    //    /// حذف نرم‌افزار
    //    /// </summary>
    //    [HttpDelete("softwares/{publicId:guid}")]
    //    [RequirePermission("Management.Software.Delete")]
    //    [ProducesResponseType(200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> DeleteSoftware(Guid publicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var success = await _managementService.DeleteSoftwareAsync(publicId, ct);
    //            if (!success)
    //                return NotFound(new { error = "نرم‌افزار یافت نشد" });

    //            await LogAuditAsync("SoftwareDeleted", "Software", publicId.ToString(),
    //                "نرم‌افزار حذف شد");

    //            return Ok(new { message = "نرم‌افزار با موفقیت حذف شد" });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در حذف نرم‌افزار");
    //            return StatusCode(500, new { error = "خطا در حذف نرم‌افزار" });
    //        }
    //    }

    //    // ============================================
    //    // پلن‌ها (Plan)
    //    // ============================================

    //    /// <summary>
    //    /// دریافت لیست پلن‌ها
    //    /// </summary>
    //    [HttpGet("plans")]
    //    [RequirePermission("Management.Plan.Read")]
    //    [ProducesResponseType(typeof(List<PlanDto>), 200)]
    //    public async Task<IActionResult> GetPlans([FromQuery] Guid? softwarePublicId, [FromQuery] bool? isActive, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.GetPlansAsync(softwarePublicId, isActive, ct);
    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در دریافت لیست پلن‌ها");
    //            return StatusCode(500, new { error = "خطا در دریافت لیست پلن‌ها" });
    //        }
    //    }

    //    /// <summary>
    //    /// دریافت پلن با شناسه
    //    /// </summary>
    //    [HttpGet("plans/{publicId:guid}")]
    //    [RequirePermission("Management.Plan.Read")]
    //    [ProducesResponseType(typeof(PlanDto), 200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> GetPlan(Guid publicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.GetPlanByPublicIdAsync(publicId, ct);
    //            if (result == null)
    //                return NotFound(new { error = "پلن یافت نشد" });

    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در دریافت پلن");
    //            return StatusCode(500, new { error = "خطا در دریافت پلن" });
    //        }
    //    }

    //    /// <summary>
    //    /// ایجاد پلن جدید
    //    /// </summary>
    //    [HttpPost("plans")]
    //    [RequirePermission("Management.Plan.Create")]
    //    [ProducesResponseType(typeof(PlanDto), 201)]
    //    [ProducesResponseType(400)]
    //    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanDto dto, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.CreatePlanAsync(dto, GetUserId(), ct);

    //            await LogAuditAsync("PlanCreated", "Plan", result.PublicId.ToString(),
    //                $"پلن جدید ایجاد شد: {dto.Name}");

    //            return CreatedAtAction(nameof(GetPlan), new { publicId = result.PublicId }, result);
    //        }
    //        catch (ArgumentException ex)
    //        {
    //            return BadRequest(new { error = ex.Message });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در ایجاد پلن");
    //            return StatusCode(500, new { error = "خطا در ایجاد پلن" });
    //        }
    //    }

    //    /// <summary>
    //    /// ویرایش پلن
    //    /// </summary>
    //    [HttpPut("plans")]
    //    [RequirePermission("Management.Plan.Update")]
    //    [ProducesResponseType(typeof(PlanDto), 200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> UpdatePlan([FromBody] UpdatePlanDto dto, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.UpdatePlanAsync(dto, GetUserId(), ct);
    //            if (result == null)
    //                return NotFound(new { error = "پلن یافت نشد" });

    //            await LogAuditAsync("PlanUpdated", "Plan", dto.PublicId.ToString(),
    //                $"پلن ویرایش شد: {dto.Name}");

    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در ویرایش پلن");
    //            return StatusCode(500, new { error = "خطا در ویرایش پلن" });
    //        }
    //    }

    //    /// <summary>
    //    /// حذف پلن
    //    /// </summary>
    //    [HttpDelete("plans/{publicId:guid}")]
    //    [RequirePermission("Management.Plan.Delete")]
    //    [ProducesResponseType(200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> DeletePlan(Guid publicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var success = await _managementService.DeletePlanAsync(publicId, ct);
    //            if (!success)
    //                return NotFound(new { error = "پلن یافت نشد" });

    //            await LogAuditAsync("PlanDeleted", "Plan", publicId.ToString(),
    //                "پلن حذف شد");

    //            return Ok(new { message = "پلن با موفقیت حذف شد" });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در حذف پلن");
    //            return StatusCode(500, new { error = "خطا در حذف پلن" });
    //        }
    //    }

    //    // ============================================
    //    // مشتریان (Customer)
    //    // ============================================

    //    /// <summary>
    //    /// دریافت لیست مشتریان
    //    /// </summary>
    //    [HttpGet("customers")]
    //    [RequirePermission("Management.Customer.Read")]
    //    [ProducesResponseType(typeof(List<CustomerDto>), 200)]
    //    public async Task<IActionResult> GetCustomers([FromQuery] int? status, [FromQuery] string? searchTerm, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.GetCustomersAsync(status, searchTerm, ct);
    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در دریافت لیست مشتریان");
    //            return StatusCode(500, new { error = "خطا در دریافت لیست مشتریان" });
    //        }
    //    }

    //    /// <summary>
    //    /// دریافت مشتری با شناسه
    //    /// </summary>
    //    [HttpGet("customers/{publicId:guid}")]
    //    [RequirePermission("Management.Customer.Read")]
    //    [ProducesResponseType(typeof(CustomerDto), 200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> GetCustomer(Guid publicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.GetCustomerByPublicIdAsync(publicId, ct);
    //            if (result == null)
    //                return NotFound(new { error = "مشتری یافت نشد" });

    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در دریافت مشتری");
    //            return StatusCode(500, new { error = "خطا در دریافت مشتری" });
    //        }
    //    }

    //    /// <summary>
    //    /// دریافت مشتری با اشتراک‌ها و مخاطبین
    //    /// </summary>
    //    [HttpGet("customers/{publicId:guid}/full")]
    //    [RequirePermission("Management.Customer.Read")]
    //    [ProducesResponseType(typeof(CustomerWithSubscriptionsDto), 200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> GetCustomerFull(Guid publicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.GetCustomerWithSubscriptionsAsync(publicId, ct);
    //            if (result == null)
    //                return NotFound(new { error = "مشتری یافت نشد" });

    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در دریافت اطلاعات کامل مشتری");
    //            return StatusCode(500, new { error = "خطا در دریافت اطلاعات مشتری" });
    //        }
    //    }

    //    /// <summary>
    //    /// ایجاد مشتری جدید
    //    /// </summary>
    //    [HttpPost("customers")]
    //    [RequirePermission("Management.Customer.Create")]
    //    [ProducesResponseType(typeof(CustomerDto), 201)]
    //    [ProducesResponseType(400)]
    //    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.CreateCustomerAsync(dto, GetUserId(), ct);

    //            await LogAuditAsync("CustomerCreated", "Customer", result.PublicId.ToString(),
    //                $"مشتری جدید ایجاد شد: {dto.Name}");

    //            return CreatedAtAction(nameof(GetCustomer), new { publicId = result.PublicId }, result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در ایجاد مشتری");
    //            return StatusCode(500, new { error = "خطا در ایجاد مشتری" });
    //        }
    //    }

    //    /// <summary>
    //    /// ویرایش مشتری
    //    /// </summary>
    //    [HttpPut("customers")]
    //    [RequirePermission("Management.Customer.Update")]
    //    [ProducesResponseType(typeof(CustomerDto), 200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> UpdateCustomer([FromBody] UpdateCustomerDto dto, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.UpdateCustomerAsync(dto, GetUserId(), ct);
    //            if (result == null)
    //                return NotFound(new { error = "مشتری یافت نشد" });

    //            await LogAuditAsync("CustomerUpdated", "Customer", dto.PublicId.ToString(),
    //                $"مشتری ویرایش شد: {dto.Name}");

    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در ویرایش مشتری");
    //            return StatusCode(500, new { error = "خطا در ویرایش مشتری" });
    //        }
    //    }

    //    /// <summary>
    //    /// حذف مشتری
    //    /// </summary>
    //    [HttpDelete("customers/{publicId:guid}")]
    //    [RequirePermission("Management.Customer.Delete")]
    //    [ProducesResponseType(200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> DeleteCustomer(Guid publicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var success = await _managementService.DeleteCustomerAsync(publicId, ct);
    //            if (!success)
    //                return NotFound(new { error = "مشتری یافت نشد" });

    //            await LogAuditAsync("CustomerDeleted", "Customer", publicId.ToString(),
    //                "مشتری حذف شد");

    //            return Ok(new { message = "مشتری با موفقیت حذف شد" });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در حذف مشتری");
    //            return StatusCode(500, new { error = "خطا در حذف مشتری" });
    //        }
    //    }

    //    // ============================================
    //    // مخاطبین مشتری (CustomerContact)
    //    // ============================================

    //    /// <summary>
    //    /// دریافت مخاطبین مشتری
    //    /// </summary>
    //    [HttpGet("customers/{customerPublicId:guid}/contacts")]
    //    [RequirePermission("Management.Customer.Read")]
    //    [ProducesResponseType(typeof(List<CustomerContactDto>), 200)]
    //    public async Task<IActionResult> GetCustomerContacts(Guid customerPublicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.GetCustomerContactsAsync(customerPublicId, ct);
    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در دریافت مخاطبین مشتری");
    //            return StatusCode(500, new { error = "خطا در دریافت مخاطبین" });
    //        }
    //    }

    //    /// <summary>
    //    /// ایجاد مخاطب جدید
    //    /// </summary>
    //    [HttpPost("customer-contacts")]
    //    [RequirePermission("Management.Customer.Update")]
    //    [ProducesResponseType(typeof(CustomerContactDto), 201)]
    //    [ProducesResponseType(400)]
    //    public async Task<IActionResult> CreateCustomerContact([FromBody] CreateCustomerContactDto dto, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.CreateCustomerContactAsync(dto, GetUserId(), ct);

    //            await LogAuditAsync("CustomerContactCreated", "CustomerContact", result.PublicId.ToString(),
    //                $"مخاطب جدید ایجاد شد: {dto.FullName}");

    //            return Created("", result);
    //        }
    //        catch (ArgumentException ex)
    //        {
    //            return BadRequest(new { error = ex.Message });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در ایجاد مخاطب");
    //            return StatusCode(500, new { error = "خطا در ایجاد مخاطب" });
    //        }
    //    }

    //    /// <summary>
    //    /// ویرایش مخاطب
    //    /// </summary>
    //    [HttpPut("customer-contacts")]
    //    [RequirePermission("Management.Customer.Update")]
    //    [ProducesResponseType(typeof(CustomerContactDto), 200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> UpdateCustomerContact([FromBody] UpdateCustomerContactDto dto, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.UpdateCustomerContactAsync(dto, GetUserId(), ct);
    //            if (result == null)
    //                return NotFound(new { error = "مخاطب یافت نشد" });

    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در ویرایش مخاطب");
    //            return StatusCode(500, new { error = "خطا در ویرایش مخاطب" });
    //        }
    //    }

    //    /// <summary>
    //    /// حذف مخاطب
    //    /// </summary>
    //    [HttpDelete("customer-contacts/{publicId:guid}")]
    //    [RequirePermission("Management.Customer.Update")]
    //    [ProducesResponseType(200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> DeleteCustomerContact(Guid publicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var success = await _managementService.DeleteCustomerContactAsync(publicId, ct);
    //            if (!success)
    //                return NotFound(new { error = "مخاطب یافت نشد" });

    //            return Ok(new { message = "مخاطب با موفقیت حذف شد" });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در حذف مخاطب");
    //            return StatusCode(500, new { error = "خطا در حذف مخاطب" });
    //        }
    //    }

    //    // ============================================
    //    // اشتراک‌ها (Subscription)
    //    // ============================================

    //    /// <summary>
    //    /// دریافت اشتراک‌های مشتری
    //    /// </summary>
    //    [HttpGet("customers/{customerPublicId:guid}/subscriptions")]
    //    [RequirePermission("Management.Subscription.Read")]
    //    [ProducesResponseType(typeof(List<CustomerSoftwareDto>), 200)]
    //    public async Task<IActionResult> GetCustomerSubscriptions(Guid customerPublicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.GetCustomerSubscriptionsAsync(customerPublicId, ct);
    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در دریافت اشتراک‌های مشتری");
    //            return StatusCode(500, new { error = "خطا در دریافت اشتراک‌ها" });
    //        }
    //    }

    //    /// <summary>
    //    /// دریافت اشتراک با شناسه
    //    /// </summary>
    //    [HttpGet("subscriptions/{publicId:guid}")]
    //    [RequirePermission("Management.Subscription.Read")]
    //    [ProducesResponseType(typeof(CustomerSoftwareDto), 200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> GetSubscription(Guid publicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.GetSubscriptionByPublicIdAsync(publicId, ct);
    //            if (result == null)
    //                return NotFound(new { error = "اشتراک یافت نشد" });

    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در دریافت اشتراک");
    //            return StatusCode(500, new { error = "خطا در دریافت اشتراک" });
    //        }
    //    }

    //    /// <summary>
    //    /// ایجاد اشتراک جدید
    //    /// </summary>
    //    [HttpPost("subscriptions")]
    //    [RequirePermission("Management.Subscription.Create")]
    //    [ProducesResponseType(typeof(CustomerSoftwareDto), 201)]
    //    [ProducesResponseType(400)]
    //    public async Task<IActionResult> CreateSubscription([FromBody] CreateCustomerSoftwareDto dto, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.CreateSubscriptionAsync(dto, GetUserId(), ct);

    //            await LogAuditAsync("SubscriptionCreated", "Subscription", result.PublicId.ToString(),
    //                $"اشتراک جدید ایجاد شد: {result.LicenseKey}");

    //            return CreatedAtAction(nameof(GetSubscription), new { publicId = result.PublicId }, result);
    //        }
    //        catch (ArgumentException ex)
    //        {
    //            return BadRequest(new { error = ex.Message });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در ایجاد اشتراک");
    //            return StatusCode(500, new { error = "خطا در ایجاد اشتراک" });
    //        }
    //    }

    //    /// <summary>
    //    /// ویرایش اشتراک
    //    /// </summary>
    //    [HttpPut("subscriptions")]
    //    [RequirePermission("Management.Subscription.Update")]
    //    [ProducesResponseType(typeof(CustomerSoftwareDto), 200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> UpdateSubscription([FromBody] UpdateCustomerSoftwareDto dto, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.UpdateSubscriptionAsync(dto, GetUserId(), ct);
    //            if (result == null)
    //                return NotFound(new { error = "اشتراک یافت نشد" });

    //            await LogAuditAsync("SubscriptionUpdated", "Subscription", dto.PublicId.ToString(),
    //                "اشتراک ویرایش شد");

    //            return Ok(result);
    //        }
    //        catch (ArgumentException ex)
    //        {
    //            return BadRequest(new { error = ex.Message });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در ویرایش اشتراک");
    //            return StatusCode(500, new { error = "خطا در ویرایش اشتراک" });
    //        }
    //    }

    //    /// <summary>
    //    /// حذف اشتراک
    //    /// </summary>
    //    [HttpDelete("subscriptions/{publicId:guid}")]
    //    [RequirePermission("Management.Subscription.Delete")]
    //    [ProducesResponseType(200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> DeleteSubscription(Guid publicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var success = await _managementService.DeleteSubscriptionAsync(publicId, ct);
    //            if (!success)
    //                return NotFound(new { error = "اشتراک یافت نشد" });

    //            await LogAuditAsync("SubscriptionDeleted", "Subscription", publicId.ToString(),
    //                "اشتراک حذف شد");

    //            return Ok(new { message = "اشتراک با موفقیت حذف شد" });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در حذف اشتراک");
    //            return StatusCode(500, new { error = "خطا در حذف اشتراک" });
    //        }
    //    }

    //    /// <summary>
    //    /// فعال‌سازی لایسنس
    //    /// این endpoint برای نرم‌افزارهای کلاینت استفاده می‌شود
    //    /// </summary>
    //    [HttpPost("subscriptions/activate")]
    //    [AllowAnonymous]
    //    [ProducesResponseType(typeof(ActivationResultDto), 200)]
    //    [ProducesResponseType(400)]
    //    public async Task<IActionResult> ActivateLicense([FromBody] ActivateLicenseDto dto, CancellationToken ct)
    //    {
    //        try
    //        {
    //            // افزودن IP به درخواست
    //            dto.IpAddress = HttpContextHelper.GetIpAddress(HttpContext);

    //            var result = await _managementService.ActivateLicenseAsync(dto, ct);

    //            if (result.IsSuccess)
    //            {
    //                await LogAuditAsync("LicenseActivated", "License", dto.LicenseKey,
    //                    $"لایسنس فعال شد از IP: {dto.IpAddress}", isAuthenticated: false);
    //            }
    //            else
    //            {
    //                await LogAuditAsync("LicenseActivationFailed", "License", dto.LicenseKey,
    //                    $"فعال‌سازی ناموفق: {result.Message}", isSuccess: false, isAuthenticated: false);
    //            }

    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در فعال‌سازی لایسنس");
    //            return StatusCode(500, new { error = "خطا در فعال‌سازی لایسنس" });
    //        }
    //    }

    //    // ============================================
    //    // دیتابیس‌ها (Database)
    //    // ============================================

    //    /// <summary>
    //    /// دریافت لیست دیتابیس‌ها
    //    /// </summary>
    //    [HttpGet("databases")]
    //    [RequirePermission("Management.Database.Read")]
    //    [ProducesResponseType(typeof(List<DbDto>), 200)]
    //    public async Task<IActionResult> GetDatabases(
    //        [FromQuery] Guid? softwarePublicId,
    //        [FromQuery] Guid? customerPublicId,
    //        [FromQuery] int? status,
    //        CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.GetDatabasesAsync(softwarePublicId, customerPublicId, status, ct);
    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در دریافت لیست دیتابیس‌ها");
    //            return StatusCode(500, new { error = "خطا در دریافت لیست دیتابیس‌ها" });
    //        }
    //    }

    //    /// <summary>
    //    /// دریافت دیتابیس با شناسه
    //    /// </summary>
    //    [HttpGet("databases/{publicId:guid}")]
    //    [RequirePermission("Management.Database.Read")]
    //    [ProducesResponseType(typeof(DbDto), 200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> GetDatabase(Guid publicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.GetDatabaseByPublicIdAsync(publicId, ct);
    //            if (result == null)
    //                return NotFound(new { error = "دیتابیس یافت نشد" });

    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در دریافت دیتابیس");
    //            return StatusCode(500, new { error = "خطا در دریافت دیتابیس" });
    //        }
    //    }

    //    /// <summary>
    //    /// ایجاد دیتابیس جدید
    //    /// </summary>
    //    [HttpPost("databases")]
    //    [RequirePermission("Management.Database.Create")]
    //    [ProducesResponseType(typeof(DbDto), 201)]
    //    [ProducesResponseType(400)]
    //    public async Task<IActionResult> CreateDatabase([FromBody] CreateDbDto dto, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.CreateDatabaseAsync(dto, GetUserId(), ct);

    //            await LogAuditAsync("DatabaseCreated", "Database", result.PublicId.ToString(),
    //                $"دیتابیس جدید ایجاد شد: {dto.Name}");

    //            return CreatedAtAction(nameof(GetDatabase), new { publicId = result.PublicId }, result);
    //        }
    //        catch (ArgumentException ex)
    //        {
    //            return BadRequest(new { error = ex.Message });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در ایجاد دیتابیس");
    //            return StatusCode(500, new { error = "خطا در ایجاد دیتابیس" });
    //        }
    //    }

    //    /// <summary>
    //    /// ویرایش دیتابیس
    //    /// </summary>
    //    [HttpPut("databases")]
    //    [RequirePermission("Management.Database.Update")]
    //    [ProducesResponseType(typeof(DbDto), 200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> UpdateDatabase([FromBody] UpdateDbDto dto, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.UpdateDatabaseAsync(dto, GetUserId(), ct);
    //            if (result == null)
    //                return NotFound(new { error = "دیتابیس یافت نشد" });

    //            await LogAuditAsync("DatabaseUpdated", "Database", dto.PublicId.ToString(),
    //                $"دیتابیس ویرایش شد: {dto.Name}");

    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در ویرایش دیتابیس");
    //            return StatusCode(500, new { error = "خطا در ویرایش دیتابیس" });
    //        }
    //    }

    //    /// <summary>
    //    /// حذف دیتابیس
    //    /// </summary>
    //    [HttpDelete("databases/{publicId:guid}")]
    //    [RequirePermission("Management.Database.Delete")]
    //    [ProducesResponseType(200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> DeleteDatabase(Guid publicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var success = await _managementService.DeleteDatabaseAsync(publicId, ct);
    //            if (!success)
    //                return NotFound(new { error = "دیتابیس یافت نشد" });

    //            await LogAuditAsync("DatabaseDeleted", "Database", publicId.ToString(),
    //                "دیتابیس حذف شد");

    //            return Ok(new { message = "دیتابیس با موفقیت حذف شد" });
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در حذف دیتابیس");
    //            return StatusCode(500, new { error = "خطا در حذف دیتابیس" });
    //        }
    //    }

    //    /// <summary>
    //    /// تست اتصال دیتابیس
    //    /// </summary>
    //    [HttpPost("databases/test-connection")]
    //    [RequirePermission("Management.Database.Read")]
    //    [ProducesResponseType(typeof(DbConnectionTestResultDto), 200)]
    //    public async Task<IActionResult> TestDatabaseConnection([FromBody] TestDbConnectionDto dto, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.TestDatabaseConnectionAsync(dto, ct);
    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در تست اتصال دیتابیس");
    //            return StatusCode(500, new { error = "خطا در تست اتصال" });
    //        }
    //    }

    //    /// <summary>
    //    /// تست اتصال دیتابیس موجود
    //    /// </summary>
    //    [HttpPost("databases/{publicId:guid}/test-connection")]
    //    [RequirePermission("Management.Database.Read")]
    //    [ProducesResponseType(typeof(DbConnectionTestResultDto), 200)]
    //    [ProducesResponseType(404)]
    //    public async Task<IActionResult> TestExistingDatabaseConnection(Guid publicId, CancellationToken ct)
    //    {
    //        try
    //        {
    //            var result = await _managementService.TestExistingDatabaseConnectionAsync(publicId, ct);
    //            return Ok(result);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "خطا در تست اتصال دیتابیس");
    //            return StatusCode(500, new { error = "خطا در تست اتصال" });
    //        }
    //    }

    //    // ============================================
    //    // Helper Methods
    //    // ============================================

    //    private async Task LogAuditAsync(string eventType, string entityType, string entityId, string description, bool isSuccess = true, bool isAuthenticated = true)
    //    {
    //        try
    //        {
    //            await _auditLogService.LogEventAsync(
    //                eventType: eventType,
    //                entityType: entityType,
    //                entityId: entityId,
    //                isSuccess: isSuccess,
    //                ipAddress: HttpContextHelper.GetIpAddress(HttpContext),
    //                userName: isAuthenticated ? GetUserName() : "Anonymous",
    //                userId: isAuthenticated ? GetUserId() : 0,
    //                userAgent: HttpContextHelper.GetUserAgent(HttpContext),
    //                description: description,
    //                ct: default);
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogWarning(ex, "خطا در ثبت Audit Log");
    //        }
    //    }
    //}
}
