using BnpCashClaudeApp.api.Attributes;
using BnpCashClaudeApp.api.Helpers;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.AuditLogSubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading;

namespace BnpCashClaudeApp.api.Controllers.Log
{
    /// <summary>
    /// کنترلر مدیریت Audit Log (لاگ‌های امنیتی)
    /// پیاده‌سازی الزام FAU (Security Audit) از استاندارد ISO 15408
    /// 
    /// الزامات پیاده‌سازی شده:
    /// - FAU_GEN: تولید داده‌های ممیزی امنیتی
    /// - FAU_SAR: بازبینی ممیزی امنیتی
    /// - FAU_SEL: انتخاب رویدادهای ممیزی
    /// - FAU_STG: ذخیره‌سازی رویدادهای ممیزی
    /// 
    /// دسترسی: فقط مدیران سیستم
    /// پیاده‌سازی الزام FDP_ACF - کنترل دسترسی دقیق
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("ApiPolicy")]
    public class AuditLogController : ControllerBase
    {
        private const int AccessHistoryDefaultRetentionDays = 90;

        private readonly IDbContextFactory<LogDbContext> _logDbContextFactory;
        private readonly IAuditLogService _auditLogService;
        private readonly IDataExportService _dataExportService;

        /// <summary>
        /// سازنده - استفاده از DbContextFactory برای جلوگیری از خطای concurrent access
        /// </summary>
        public AuditLogController(
            IDbContextFactory<LogDbContext> logDbContextFactory,
            IAuditLogService auditLogService,
            IDataExportService dataExportService)
        {
            _logDbContextFactory = logDbContextFactory;
            _auditLogService = auditLogService;
            _dataExportService = dataExportService;
        }

        #region DTOs

        /// <summary>
        /// DTO برای نمایش لاگ امنیتی
        /// ============================================
        /// تاریخ‌ها به صورت شمسی نمایش داده می‌شوند
        /// ============================================
        /// </summary>
        public class AuditLogDto
        {
            public long Id { get; set; }

            /// <summary>
            /// تاریخ و زمان رویداد (شمسی)
            /// مثال: 1403/09/26 12:30:00
            /// </summary>
            public string EventDateTime { get; set; } = string.Empty;

            public string EventType { get; set; } = string.Empty;
            public string? EntityId { get; set; }
            public string? EntityType { get; set; }
            public bool IsSuccess { get; set; }
            public string? ErrorMessage { get; set; }
            public string? IpAddress { get; set; }
            public string? UserName { get; set; }
            public long? UserId { get; set; }
            public string? OperatingSystem { get; set; }
            public string? UserAgent { get; set; }
            public string? Description { get; set; }
        }

        /// <summary>
        /// DTO برای جزئیات لاگ
        /// </summary>
        public class AuditLogDetailDto
        {
            public long Id { get; set; }
            public string FieldName { get; set; } = string.Empty;
            public string? OldValue { get; set; }
            public string? NewValue { get; set; }
            public string? DataType { get; set; }
            public string? Description { get; set; }
        }

        /// <summary>
        /// DTO برای لاگ با جزئیات کامل
        /// </summary>
        public class AuditLogWithDetailsDto : AuditLogDto
        {
            public List<AuditLogDetailDto> Details { get; set; } = new();
        }

        /// <summary>
        /// DTO برای فیلتر و جستجوی لاگ‌ها
        /// ============================================
        /// تاریخ‌ها به صورت شمسی دریافت می‌شوند
        /// ============================================
        /// </summary>
        public class AuditLogFilterDto
        {
            /// <summary>
            /// تاریخ شروع (شمسی - اختیاری)
            /// فرمت: 1403/09/26 یا 1403/09/26 12:30:00
            /// </summary>
            public string? FromDate { get; set; }

            /// <summary>
            /// تاریخ پایان (شمسی - اختیاری)
            /// فرمت: 1403/09/26 یا 1403/09/26 23:59:59
            /// </summary>
            public string? ToDate { get; set; }

            /// <summary>
            /// نوع رویداد (اختیاری)
            /// مثال: Authentication, Create, Update, Delete
            /// </summary>
            public string? EventType { get; set; }

            /// <summary>
            /// نام کاربری (اختیاری)
            /// </summary>
            public string? UserName { get; set; }

            /// <summary>
            /// شناسه کاربر (اختیاری)
            /// </summary>
            public long? UserId { get; set; }

            /// <summary>
            /// آدرس IP (اختیاری)
            /// </summary>
            public string? IpAddress { get; set; }

            /// <summary>
            /// فقط رویدادهای موفق/ناموفق (اختیاری)
            /// </summary>
            public bool? IsSuccess { get; set; }

            /// <summary>
            /// نوع موجودیت (اختیاری)
            /// مثال: User, Menu, Grp
            /// </summary>
            public string? EntityType { get; set; }

            /// <summary>
            /// شماره صفحه (پیش‌فرض: 1)
            /// </summary>
            public int PageNumber { get; set; } = 1;

            /// <summary>
            /// تعداد در هر صفحه (پیش‌فرض: 20، حداکثر: 100)
            /// </summary>
            public int PageSize { get; set; } = 20;
        }

        /// <summary>
        /// DTO برای نتیجه صفحه‌بندی شده
        /// </summary>
        public class PagedResultDto<T>
        {
            public List<T> Items { get; set; } = new();
            public int TotalCount { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalPages { get; set; }
            public bool HasPreviousPage { get; set; }
            public bool HasNextPage { get; set; }
        }

        /// <summary>
        /// DTO برای آمار امنیتی
        /// </summary>
        public class SecurityStatisticsDto
        {
            /// <summary>
            /// تعداد کل رویدادها
            /// </summary>
            public int TotalEvents { get; set; }

            /// <summary>
            /// تعداد رویدادهای موفق
            /// </summary>
            public int SuccessfulEvents { get; set; }

            /// <summary>
            /// تعداد رویدادهای ناموفق
            /// </summary>
            public int FailedEvents { get; set; }

            /// <summary>
            /// تعداد ورودهای ناموفق (امروز)
            /// </summary>
            public int FailedLoginsToday { get; set; }

            /// <summary>
            /// تعداد ورودهای موفق (امروز)
            /// </summary>
            public int SuccessfulLoginsToday { get; set; }

            /// <summary>
            /// کاربران فعال (امروز)
            /// </summary>
            public int ActiveUsersToday { get; set; }

            /// <summary>
            /// IPهای منحصر به فرد (امروز)
            /// </summary>
            public int UniqueIpsToday { get; set; }

            /// <summary>
            /// رویدادها بر اساس نوع
            /// </summary>
            public Dictionary<string, int> EventsByType { get; set; } = new();

            /// <summary>
            /// رویدادها در 7 روز گذشته
            /// </summary>
            public Dictionary<string, int> EventsLast7Days { get; set; } = new();
        }

        #endregion

        /// <summary>
        /// دریافت لیست لاگ‌ها با صفحه‌بندی و فیلتر
        /// الزام FAU_SAR.1: بازبینی ممیزی
        /// </summary>
        /// <param name="filter">فیلترهای جستجو</param>
        /// <returns>لیست صفحه‌بندی شده لاگ‌ها</returns>
        [HttpGet]
        [RequirePermission("AuditLog.Read")]
        [ProducesResponseType(typeof(PagedResultDto<AuditLogDto>), 200)]
        public async Task<ActionResult<PagedResultDto<AuditLogDto>>> GetAll([FromQuery] AuditLogFilterDto filter)
        {
            try
            {
            // ============================================
            // اعتبارسنجی پارامترها
            // ============================================
            if (filter.PageNumber < 1) filter.PageNumber = 1;
            if (filter.PageSize < 1) filter.PageSize = 20;
            if (filter.PageSize > 100) filter.PageSize = 100;

            // ============================================
            // ایجاد DbContext جداگانه برای جلوگیری از خطای concurrent access
            // ============================================
            await using var context = await _logDbContextFactory.CreateDbContextAsync();

            // ============================================
            // ساخت Query با فیلترها
            // AsNoTracking برای بهبود کارایی در عملیات فقط خواندنی
            // ============================================
            var query = context.AuditLogMasters.AsNoTracking().AsQueryable();

            // فیلتر تاریخ شروع (تبدیل شمسی به میلادی)
            if (!string.IsNullOrEmpty(filter.FromDate))
            {
                try
                {
                    var fromDateGregorian = BaseEntity.ToGregorianDateTime(filter.FromDate);
                    query = query.Where(x => x.EventDateTime >= fromDateGregorian);
                }
                catch (FormatException)
                {
                    return BadRequest(new { message = "فرمت تاریخ شروع نامعتبر است. فرمت صحیح: 1403/09/26" });
                }
            }

            // فیلتر تاریخ پایان (تبدیل شمسی به میلادی)
            if (!string.IsNullOrEmpty(filter.ToDate))
            {
                try
                {
                    var toDateGregorian = BaseEntity.ToGregorianDateTime(filter.ToDate);
                    query = query.Where(x => x.EventDateTime <= toDateGregorian);
                }
                catch (FormatException)
                {
                    return BadRequest(new { message = "فرمت تاریخ پایان نامعتبر است. فرمت صحیح: 1403/09/26" });
                }
            }

            // فیلتر نوع رویداد
            if (!string.IsNullOrEmpty(filter.EventType))
            {
                query = query.Where(x => x.EventType == filter.EventType);
            }

            // فیلتر نام کاربری
            if (!string.IsNullOrEmpty(filter.UserName))
            {
                query = query.Where(x => x.UserName != null && x.UserName.Contains(filter.UserName));
            }

            // فیلتر شناسه کاربر
            if (filter.UserId.HasValue)
            {
                query = query.Where(x => x.UserId == filter.UserId.Value);
            }

            // فیلتر آدرس IP
            if (!string.IsNullOrEmpty(filter.IpAddress))
            {
                query = query.Where(x => x.IpAddress != null && x.IpAddress.Contains(filter.IpAddress));
            }

            // فیلتر موفقیت/شکست
            if (filter.IsSuccess.HasValue)
            {
                query = query.Where(x => x.IsSuccess == filter.IsSuccess.Value);
            }

            // فیلتر نوع موجودیت
            if (!string.IsNullOrEmpty(filter.EntityType))
            {
                query = query.Where(x => x.EntityType == filter.EntityType);
            }

            // ============================================
            // شمارش کل رکوردها
            // ============================================
            var totalCount = await query.CountAsync();

            // ============================================
            // صفحه‌بندی و مرتب‌سازی
            // ============================================
            // ============================================
            // دریافت داده‌ها از دیتابیس
            // ============================================
            var rawItems = await query
                .OrderByDescending(x => x.EventDateTime)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // ============================================
            // تبدیل تاریخ میلادی به شمسی در خروجی
            // ============================================
            var items = rawItems.Select(x => new AuditLogDto
            {
                Id = x.Id,
                EventDateTime = BaseEntity.ToPersianDateTime(x.EventDateTime), // تبدیل به شمسی
                EventType = x.EventType,
                EntityId = x.EntityId,
                EntityType = x.EntityType,
                IsSuccess = x.IsSuccess,
                ErrorMessage = x.ErrorMessage,
                IpAddress = x.IpAddress,
                UserName = x.UserName,
                UserId = x.UserId,
                OperatingSystem = x.OperatingSystem,
                UserAgent = x.UserAgent,
                Description = x.Description
            }).ToList();

            // ============================================
            // ساخت نتیجه صفحه‌بندی شده
            // ============================================
            var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

                var response = new PagedResultDto<AuditLogDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = filter.PageNumber > 1,
                    HasNextPage = filter.PageNumber < totalPages
                };

                var protectedResponse = await ProtectReadPayloadAsync(response, "AuditLogPagedResult");
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت جزئیات یک لاگ امنیتی
        /// الزام FAU_SAR.1: بازبینی ممیزی
        /// </summary>
        /// <param name="id">شناسه لاگ</param>
        /// <returns>جزئیات کامل لاگ</returns>
        [HttpGet("{id}")]
        [RequirePermission("AuditLog.Read")]
        [ProducesResponseType(typeof(AuditLogWithDetailsDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<AuditLogWithDetailsDto>> GetById(int id)
        {
            try
            {
            // ============================================
            // ایجاد DbContext جداگانه برای جلوگیری از خطای concurrent access
            // ============================================
            await using var context = await _logDbContextFactory.CreateDbContextAsync();

            // ============================================
            // دریافت لاگ با جزئیات
            // AsNoTracking برای بهبود کارایی در عملیات فقط خواندنی
            // ============================================
            var log = await context.AuditLogMasters.AsNoTracking()
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (log == null)
            {
                return NotFound(new { message = "لاگ امنیتی یافت نشد" });
            }

            // ============================================
            // تبدیل به DTO با تاریخ شمسی
            // ============================================
            var result = new AuditLogWithDetailsDto
            {
                Id = log.Id,
                EventDateTime = BaseEntity.ToPersianDateTime(log.EventDateTime), // تبدیل به شمسی
                EventType = log.EventType,
                EntityId = log.EntityId,
                EntityType = log.EntityType,
                IsSuccess = log.IsSuccess,
                ErrorMessage = log.ErrorMessage,
                IpAddress = log.IpAddress,
                UserName = log.UserName,
                UserId = log.UserId,
                OperatingSystem = log.OperatingSystem,
                UserAgent = log.UserAgent,
                Description = log.Description,
                Details = log.Details.Select(d => new AuditLogDetailDto
                {
                    Id = d.Id,
                    FieldName = d.FieldName,
                    OldValue = d.OldValue,
                    NewValue = d.NewValue,
                    DataType = d.DataType,
                    Description = d.Description
                }).ToList()
            };

                var protectedResult = await ProtectReadPayloadAsync(result, "AuditLog", id.ToString());
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت لاگ‌های یک کاربر خاص
        /// الزام FAU_SAR.1: بازبینی ممیزی
        /// </summary>
        /// <param name="username">نام کاربری</param>
        /// <param name="pageNumber">شماره صفحه</param>
        /// <param name="pageSize">تعداد در هر صفحه</param>
        /// <returns>لیست لاگ‌های کاربر</returns>
        [HttpGet("user/{username}")]
        [RequirePermission("AuditLog.Read")]
        [ProducesResponseType(typeof(PagedResultDto<AuditLogDto>), 200)]
        public async Task<ActionResult<PagedResultDto<AuditLogDto>>> GetByUser(
            string username,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var filter = new AuditLogFilterDto
            {
                UserName = username,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetAll(filter);
        }

        /// <summary>
        /// دریافت ورودهای ناموفق
        /// الزام FAU_SAR.1: بازبینی ممیزی - شناسایی حملات
        /// </summary>
        /// <param name="pageNumber">شماره صفحه</param>
        /// <param name="pageSize">تعداد در هر صفحه</param>
        /// <returns>لیست ورودهای ناموفق</returns>
        [HttpGet("failed-logins")]
        [RequirePermission("AuditLog.Read")]
        [ProducesResponseType(typeof(PagedResultDto<AuditLogDto>), 200)]
        public async Task<ActionResult<PagedResultDto<AuditLogDto>>> GetFailedLogins(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var filter = new AuditLogFilterDto
            {
                EventType = "Authentication",
                IsSuccess = false,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetAll(filter);
        }

        /// <summary>
        /// دریافت رویدادهای امروز
        /// الزام FAU_SAR.1: بازبینی ممیزی
        /// </summary>
        /// <param name="pageNumber">شماره صفحه</param>
        /// <param name="pageSize">تعداد در هر صفحه</param>
        /// <returns>لیست رویدادهای امروز</returns>
        [HttpGet("today")]
        [RequirePermission("AuditLog.Read")]
        [ProducesResponseType(typeof(PagedResultDto<AuditLogDto>), 200)]
        public async Task<ActionResult<PagedResultDto<AuditLogDto>>> GetToday(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            // تبدیل تاریخ امروز به شمسی
            var todayPersian = BaseEntity.ToPersianDate(DateTime.UtcNow.Date);
            var tomorrowPersian = BaseEntity.ToPersianDate(DateTime.UtcNow.Date.AddDays(1));

            var filter = new AuditLogFilterDto
            {
                FromDate = todayPersian,
                ToDate = tomorrowPersian,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GetAll(filter);
        }

        /// <summary>
        /// تاریخچه دسترسی کاربر جاری
        /// الزام FTA_TAH.1.2 - تاریخچه دسترسی کاربر با سیاست نگهداشت و فیلتر
        /// </summary>
        [HttpGet("my-access-history")]
        [RequirePermission("Sessions.Read")]
        [ProducesResponseType(typeof(PagedResultDto<AuditLogDto>), 200)]
        public async Task<ActionResult<PagedResultDto<AuditLogDto>>> GetMyAccessHistory(
            [FromQuery] AuditLogFilterDto filter,
            CancellationToken ct = default)
        {
            try
            {
                var userId = GetUserId();
                if (userId <= 0)
                {
                    return Unauthorized(new { message = "کاربر احراز هویت نشده است" });
                }

                var response = await GetAccessHistoryReportInternalAsync(filter, userId, ct);
                var protectedResponse = await ProtectReadPayloadAsync(
                    response,
                    "UserAccessHistory",
                    userId.ToString(),
                    ct);

                return Ok(protectedResponse);
            }
            catch (FormatException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// گزارش دسترسی کاربران برای مدیر
        /// الزام FTA_TAH.1.3 - گزارش مدیریتی تاریخچه دسترسی
        /// </summary>
        [HttpGet("access-history")]
        [RequirePermission("AuditLog.Read")]
        [ProducesResponseType(typeof(PagedResultDto<AuditLogDto>), 200)]
        public async Task<ActionResult<PagedResultDto<AuditLogDto>>> GetAccessHistoryReport(
            [FromQuery] AuditLogFilterDto filter,
            CancellationToken ct = default)
        {
            try
            {
                var response = await GetAccessHistoryReportInternalAsync(filter, null, ct);
                var protectedResponse = await ProtectReadPayloadAsync(response, "AccessHistoryReport", null, ct);
                return Ok(protectedResponse);
            }
            catch (FormatException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// خروجی امن از گزارش دسترسی کاربران
        /// الزام FTA_TAH.1.3 - قابلیت خروجی برای گزارش مدیریتی
        /// </summary>
        [HttpGet("access-history/export")]
        [RequirePermission("AuditLog.Export")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> ExportAccessHistory(
            [FromQuery] AuditLogFilterDto filter,
            [FromQuery] int maxRecords = 2000,
            CancellationToken ct = default)
        {
            try
            {
                if (maxRecords < 1) maxRecords = 1;
                if (maxRecords > 10000) maxRecords = 10000;

                await using var context = await _logDbContextFactory.CreateDbContextAsync(ct);
                var query = BuildAccessHistoryQuery(context, filter, null);

                var rawItems = await query
                    .OrderByDescending(x => x.EventDateTime)
                    .Take(maxRecords)
                    .ToListAsync(ct);

                var items = rawItems.Select(MapToAuditLogDto).ToList();
                var payload = new
                {
                    GeneratedAt = BaseEntity.ToPersianDateTime(DateTime.UtcNow),
                    RetentionDays = AccessHistoryDefaultRetentionDays,
                    TotalCount = items.Count,
                    Items = items
                };

                var exportContext = new ExportContext
                {
                    EntityType = "AccessHistoryExport",
                    UserId = GetUserId(),
                    UserName = User.Identity?.Name ?? "Unknown",
                    IpAddress = HttpContextHelper.GetIpAddress(HttpContext),
                    UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                    RequestPath = HttpContext.Request.Path,
                    RequestedFormat = "JSON"
                };

                var secureExport = await _dataExportService.WrapWithSecurityAttributesAsync(payload, exportContext, ct);
                return Ok(secureExport);
            }
            catch (FormatException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت انواع رویدادهای موجود
        /// الزام FAU_SEL.1: انتخاب ممیزی انتخابی
        /// </summary>
        /// <returns>لیست انواع رویداد</returns>
        [HttpGet("event-types")]
        [RequirePermission("AuditLog.Read")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public async Task<ActionResult<List<string>>> GetEventTypes()
        {
            try
            {
            // ============================================
            // ایجاد DbContext جداگانه برای جلوگیری از خطای concurrent access
            // ============================================
            await using var context = await _logDbContextFactory.CreateDbContextAsync();

            // ============================================
            // دریافت لیست یکتای انواع رویداد
            // AsNoTracking برای بهبود کارایی در عملیات فقط خواندنی
            // ============================================
            var eventTypes = await context.AuditLogMasters.AsNoTracking()
                .Select(x => x.EventType)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

                var protectedEventTypes = await ProtectReadPayloadAsync(eventTypes, "AuditLogEventTypeList");
                return Ok(protectedEventTypes);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت آمار امنیتی
        /// الزام FAU_SAR.1: بازبینی ممیزی - داشبورد امنیتی
        /// </summary>
        /// <returns>آمار امنیتی</returns>
        [HttpGet("statistics")]
        [RequirePermission("AuditLog.Statistics")]
        [ProducesResponseType(typeof(SecurityStatisticsDto), 200)]
        public async Task<ActionResult<SecurityStatisticsDto>> GetStatistics()
        {
            try
            {
            // ============================================
            // ایجاد DbContext جداگانه برای جلوگیری از خطای concurrent access
            // ============================================
            await using var context = await _logDbContextFactory.CreateDbContextAsync();

            var today = DateTime.UtcNow.Date;
            var sevenDaysAgo = today.AddDays(-7);

            // ============================================
            // محاسبه آمارهای کلی - اجرای ترتیبی برای جلوگیری از خطای DbContext
            // ============================================
            var stats = new SecurityStatisticsDto();

            // آمار کلی - استفاده از AsNoTracking برای بهبود کارایی
            stats.TotalEvents = await context.AuditLogMasters.AsNoTracking().CountAsync();
            stats.SuccessfulEvents = await context.AuditLogMasters.AsNoTracking().CountAsync(x => x.IsSuccess);
            stats.FailedEvents = await context.AuditLogMasters.AsNoTracking().CountAsync(x => !x.IsSuccess);

            // ============================================
            // آمار امروز
            // ============================================
            stats.FailedLoginsToday = await context.AuditLogMasters.AsNoTracking()
                .CountAsync(x => x.EventDateTime >= today &&
                                x.EventType == "Authentication" &&
                                !x.IsSuccess);

            stats.SuccessfulLoginsToday = await context.AuditLogMasters.AsNoTracking()
                .CountAsync(x => x.EventDateTime >= today &&
                                x.EventType == "Authentication" &&
                                x.IsSuccess && x.Description != null && x.Description.Contains("ورود"));

            stats.ActiveUsersToday = await context.AuditLogMasters.AsNoTracking()
                .Where(x => x.EventDateTime >= today && x.UserName != null)
                .Select(x => x.UserName)
                .Distinct()
                .CountAsync();

            stats.UniqueIpsToday = await context.AuditLogMasters.AsNoTracking()
                .Where(x => x.EventDateTime >= today && x.IpAddress != null)
                .Select(x => x.IpAddress)
                .Distinct()
                .CountAsync();

            // ============================================
            // رویدادها بر اساس نوع
            // ============================================
            stats.EventsByType = await context.AuditLogMasters.AsNoTracking()
                .GroupBy(x => x.EventType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);

            // ============================================
            // رویدادها در 7 روز گذشته
            // ============================================
            var last7DaysData = await context.AuditLogMasters.AsNoTracking()
                .Where(x => x.EventDateTime >= sevenDaysAgo)
                .GroupBy(x => x.EventDateTime.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            // تبدیل تاریخ‌ها به شمسی
            stats.EventsLast7Days = last7DaysData
                .ToDictionary(x => BaseEntity.ToPersianDate(x.Date), x => x.Count);

                var protectedStats = await ProtectReadPayloadAsync(stats, "AuditLogStatistics");
                return Ok(protectedStats);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// جستجوی پیشرفته در لاگ‌ها
        /// الزام FAU_SAR.1: بازبینی ممیزی
        /// </summary>
        /// <param name="searchTerm">عبارت جستجو</param>
        /// <param name="pageNumber">شماره صفحه</param>
        /// <param name="pageSize">تعداد در هر صفحه</param>
        /// <returns>نتایج جستجو</returns>
        [HttpGet("search")]
        [RequirePermission("AuditLog.Search")]
        [ProducesResponseType(typeof(PagedResultDto<AuditLogDto>), 200)]
        public async Task<ActionResult<PagedResultDto<AuditLogDto>>> Search(
            [FromQuery] string searchTerm,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest(new { message = "عبارت جستجو الزامی است" });
            }

            // ============================================
            // اعتبارسنجی پارامترها
            // ============================================
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            // ============================================
            // ایجاد DbContext جداگانه برای جلوگیری از خطای concurrent access
            // ============================================
            await using var context = await _logDbContextFactory.CreateDbContextAsync();

            // ============================================
            // جستجو در چندین فیلد
            // AsNoTracking برای بهبود کارایی در عملیات فقط خواندنی
            // ============================================
            var query = context.AuditLogMasters.AsNoTracking()
                .Where(x =>
                    x.UserName != null && x.UserName.Contains(searchTerm) ||
                    x.IpAddress != null && x.IpAddress.Contains(searchTerm) ||
                    x.EventType != null && x.EventType.Contains(searchTerm) ||
                    x.Description != null && x.Description.Contains(searchTerm) ||
                    x.ErrorMessage != null && x.ErrorMessage.Contains(searchTerm) ||
                    x.EntityType != null && x.EntityType.Contains(searchTerm));

            var totalCount = await query.CountAsync();

            // دریافت داده‌ها از دیتابیس
            var rawSearchItems = await query
                .OrderByDescending(x => x.EventDateTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // تبدیل به DTO با تاریخ شمسی
            var items = rawSearchItems.Select(x => new AuditLogDto
            {
                Id = x.Id,
                EventDateTime = BaseEntity.ToPersianDateTime(x.EventDateTime), // تبدیل به شمسی
                EventType = x.EventType,
                EntityId = x.EntityId,
                EntityType = x.EntityType,
                IsSuccess = x.IsSuccess,
                ErrorMessage = x.ErrorMessage,
                IpAddress = x.IpAddress,
                UserName = x.UserName,
                UserId = x.UserId,
                OperatingSystem = x.OperatingSystem,
                UserAgent = x.UserAgent,
                Description = x.Description
            }).ToList();

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var response = new PagedResultDto<AuditLogDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = pageNumber > 1,
                    HasNextPage = pageNumber < totalPages
                };

                var protectedResponse = await ProtectReadPayloadAsync(response, "AuditLogSearchResult");
                return Ok(protectedResponse);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        private async Task<PagedResultDto<AuditLogDto>> GetAccessHistoryReportInternalAsync(
            AuditLogFilterDto filter,
            long? fixedUserId,
            CancellationToken ct)
        {
            if (filter.PageNumber < 1) filter.PageNumber = 1;
            if (filter.PageSize < 1) filter.PageSize = 20;
            if (filter.PageSize > 100) filter.PageSize = 100;

            await using var context = await _logDbContextFactory.CreateDbContextAsync(ct);
            var query = BuildAccessHistoryQuery(context, filter, fixedUserId);

            var totalCount = await query.CountAsync(ct);
            var rawItems = await query
                .OrderByDescending(x => x.EventDateTime)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(ct);

            var items = rawItems.Select(MapToAuditLogDto).ToList();
            var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

            return new PagedResultDto<AuditLogDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = totalPages,
                HasPreviousPage = filter.PageNumber > 1,
                HasNextPage = filter.PageNumber < totalPages
            };
        }

        private IQueryable<AuditLogMaster> BuildAccessHistoryQuery(
            LogDbContext context,
            AuditLogFilterDto filter,
            long? fixedUserId)
        {
            var query = context.AuditLogMasters.AsNoTracking().Where(x =>
                x.EventType == "Authentication" ||
                x.EventType.Contains("Login") ||
                x.EventType.Contains("Logout") ||
                x.EventType.Contains("Session") ||
                x.EventType.Contains("Access") ||
                (x.EntityType != null && (
                    x.EntityType.Contains("Session") ||
                    x.EntityType.Contains("Auth"))));

            if (fixedUserId.HasValue)
            {
                query = query.Where(x => x.UserId == fixedUserId.Value);
            }
            else if (filter.UserId.HasValue)
            {
                query = query.Where(x => x.UserId == filter.UserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.UserName))
            {
                query = query.Where(x => x.UserName != null && x.UserName.Contains(filter.UserName));
            }

            if (!string.IsNullOrWhiteSpace(filter.IpAddress))
            {
                query = query.Where(x => x.IpAddress != null && x.IpAddress.Contains(filter.IpAddress));
            }

            if (!string.IsNullOrWhiteSpace(filter.EventType))
            {
                query = query.Where(x => x.EventType == filter.EventType);
            }

            if (filter.IsSuccess.HasValue)
            {
                query = query.Where(x => x.IsSuccess == filter.IsSuccess.Value);
            }

            query = ApplyAccessHistoryDateRange(query, filter.FromDate, filter.ToDate);
            return query;
        }

        private IQueryable<AuditLogMaster> ApplyAccessHistoryDateRange(
            IQueryable<AuditLogMaster> query,
            string? fromDate,
            string? toDate)
        {
            if (string.IsNullOrWhiteSpace(fromDate))
            {
                var retentionFrom = DateTime.UtcNow.AddDays(-AccessHistoryDefaultRetentionDays);
                query = query.Where(x => x.EventDateTime >= retentionFrom);
            }
            else
            {
                try
                {
                    var fromDateGregorian = BaseEntity.ToGregorianDateTime(fromDate);
                    query = query.Where(x => x.EventDateTime >= fromDateGregorian);
                }
                catch (FormatException)
                {
                    throw new FormatException("فرمت تاریخ شروع نامعتبر است. فرمت صحیح: 1403/09/26");
                }
            }

            if (!string.IsNullOrWhiteSpace(toDate))
            {
                try
                {
                    var toDateGregorian = BaseEntity.ToGregorianDateTime(toDate);
                    query = query.Where(x => x.EventDateTime <= toDateGregorian);
                }
                catch (FormatException)
                {
                    throw new FormatException("فرمت تاریخ پایان نامعتبر است. فرمت صحیح: 1403/09/26");
                }
            }

            return query;
        }

        private static AuditLogDto MapToAuditLogDto(AuditLogMaster x)
        {
            return new AuditLogDto
            {
                Id = x.Id,
                EventDateTime = BaseEntity.ToPersianDateTime(x.EventDateTime),
                EventType = x.EventType,
                EntityId = x.EntityId,
                EntityType = x.EntityType,
                IsSuccess = x.IsSuccess,
                ErrorMessage = x.ErrorMessage,
                IpAddress = x.IpAddress,
                UserName = x.UserName,
                UserId = x.UserId,
                OperatingSystem = x.OperatingSystem,
                UserAgent = x.UserAgent,
                Description = x.Description
            };
        }

        private async Task<T> ProtectReadPayloadAsync<T>(
            T data,
            string entityType,
            string? entityId = null,
            CancellationToken ct = default) where T : class
        {
            var context = new ExportContext
            {
                EntityType = entityType,
                EntityId = entityId,
                UserId = GetUserId(),
                UserName = User.Identity?.Name ?? "Unknown",
                IpAddress = HttpContextHelper.GetIpAddress(HttpContext),
                UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
                RequestPath = HttpContext.Request.Path,
                RequestedFormat = "JSON"
            };

            var secured = await _dataExportService.WrapWithSecurityAttributesAsync(data, context, ct);
            return secured.Data;
        }

        private long GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("UserId")?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
