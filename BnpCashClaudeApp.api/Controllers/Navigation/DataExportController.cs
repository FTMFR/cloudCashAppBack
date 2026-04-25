using BnpCashClaudeApp.api.Attributes;
using BnpCashClaudeApp.api.Controllers.Base;
using BnpCashClaudeApp.api.Helpers;
using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BnpCashClaudeApp.api.Controllers.Navigation
{
    /// <summary>
    /// کنترلر مدیریت خروجی داده‌ها با ویژگی‌های امنیتی
    /// پیاده‌سازی الزامات FDP_ETC.2.1, FDP_ETC.2.2, FDP_ETC.2.4 و FAU_GEN از استاندارد ISO 15408
    /// 
    /// FDP_ETC.2.1: خروجی داده با ویژگی‌های امنیتی مرتبط
    /// FDP_ETC.2.2: ارتباط بدون ابهام ویژگی‌های امنیتی با داده‌ها
    /// FDP_ETC.2.4: قوانین کنترل خروجی اضافی
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("ApiPolicy")]
    public class DataExportController : AuditControllerBase
    {
        private readonly IDataExportService _dataExportService;
        private readonly ILogger<DataExportController> _logger;

        public DataExportController(
            IDataExportService dataExportService,
            IAuditLogService auditLogService,
            ILogger<DataExportController> logger)
            : base(auditLogService)
        {
            _dataExportService = dataExportService;
            _logger = logger;
        }

        #region Settings Management

        /// <summary>
        /// دریافت تنظیمات خروجی داده‌ها
        /// FDP_ETC.2: مشاهده تنظیمات
        /// </summary>
        [HttpGet("settings")]
        [RequirePermission("DataExport.Settings")]
        [ProducesResponseType(typeof(DataExportSettings), 200)]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var settings = await _dataExportService.GetSettingsAsync();
                var secured = await ProtectReadPayloadAsync(settings, "DataExportSettings");

                await LogAuditEventAsync("DataExportSettingsViewed", "Settings", "View", true, description: "مشاهده تنظیمات خروجی داده‌ها");

                return Ok(new
                {
                    success = true,
                    data = secured.Data,
                    securityContext = secured.SecurityContext,
                    metadata = secured.Metadata,
                    signature = secured.Signature,
                    appliedRules = secured.AppliedRules
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Data export policy denied for settings read");
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data export settings");
                return StatusCode(500, new { success = false, error = "خطا در دریافت تنظیمات" });
            }
        }

        /// <summary>
        /// به‌روزرسانی تنظیمات خروجی داده‌ها
        /// FDP_ETC.2: مدیریت تنظیمات
        /// </summary>
        [HttpPut("settings")]
        [RequirePermission("DataExport.Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateSettings([FromBody] DataExportSettings settings)
        {
            try
            {
                var userId = GetUserId();
                var result = await _dataExportService.UpdateSettingsAsync(settings, userId);

                if (result)
                {
                    await LogAuditEventAsync("DataExportSettingsUpdated", "Settings", "Update", true,
                        description: "تنظیمات خروجی داده‌ها به‌روزرسانی شد");

                    return Ok(new
                    {
                        success = true,
                        message = "تنظیمات با موفقیت به‌روزرسانی شد"
                    });
                }

                return BadRequest(new { success = false, error = "خطا در به‌روزرسانی تنظیمات" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating data export settings");
                await LogAuditEventAsync("DataExportSettingsUpdated", "Settings", "Update", false, ex.Message, ex.Message);
                return StatusCode(500, new { success = false, error = "خطا در به‌روزرسانی تنظیمات" });
            }
        }


        /// <summary>
        /// دریافت آمار خروجی داده‌ها
        /// </summary>
        [HttpGet("statistics")]
        [RequirePermission("DataExport.Read")]
        [ProducesResponseType(typeof(ExportStatistics), 200)]
        public async Task<IActionResult> GetExportStatistics()
        {
            try
            {
                var statistics = await _dataExportService.GetExportStatisticsAsync();
                var secured = await ProtectReadPayloadAsync(statistics, "DataExportStatistics");

                return Ok(new
                {
                    success = true,
                    data = secured.Data,
                    securityContext = secured.SecurityContext,
                    metadata = secured.Metadata,
                    signature = secured.Signature,
                    appliedRules = secured.AppliedRules
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Data export policy denied for statistics read");
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting export statistics");
                return StatusCode(500, new { success = false, error = "خطا در دریافت آمار" });
            }
        }

        #endregion

        #region Export Rules Management

        /// <summary>
        /// دریافت لیست انواع قانون خروجی (برای فرانت - مثلاً در dropdown)
        /// FDP_ETC.2.4: قوانین کنترل خروجی
        /// </summary>
        [HttpGet("rule-types")]
        [RequirePermission("DataExport.Rules")]
        [ProducesResponseType(typeof(IEnumerable<ExportRuleTypeItem>), 200)]
        public IActionResult GetExportRuleTypes()
        {
            var items = Enum.GetValues<ExportRuleType>()
                .Select(e => new ExportRuleTypeItem
                {
                    Value = (int)e,
                    Name = e.ToString(),
                    Label = GetExportRuleTypeLabel(e)
                })
                .ToList();
            return Ok(new { success = true, data = items });
        }

        /// <summary>
        /// دریافت لیست قوانین خروجی
        /// FDP_ETC.2.4: قوانین کنترل خروجی
        /// </summary>
        [HttpGet("rules")]
        [RequirePermission("DataExport.Rules")]
        [ProducesResponseType(typeof(IEnumerable<ExportRule>), 200)]
        public async Task<IActionResult> GetExportRules()
        {
            try
            {
                var rules = await _dataExportService.GetExportRulesAsync();
                var rulesList = rules.ToList();
                var secured = await ProtectReadPayloadAsync(rulesList, "ExportRule");

                return Ok(new
                {
                    success = true,
                    count = rulesList.Count,
                    data = secured.Data,
                    securityContext = secured.SecurityContext,
                    metadata = secured.Metadata,
                    signature = secured.Signature,
                    appliedRules = secured.AppliedRules
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Data export policy denied for export rules read");
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting export rules");
                return StatusCode(500, new { success = false, error = "خطا در دریافت قوانین" });
            }
        }

        /// <summary>
        /// ایجاد قانون خروجی جدید
        /// FDP_ETC.2.4: قوانین کنترل خروجی
        /// </summary>
        [HttpPost("rules")]
        [RequirePermission("DataExport.Admin")]
        [ProducesResponseType(typeof(ExportRule), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateExportRule([FromBody] CreateExportRuleRequest request)
        {
            try
            {
                var userId = GetUserId();
                var rule = new ExportRule
                {
                    Name = request.Name,
                    Description = request.Description,
                    RuleType = request.RuleType,
                    EntityType = request.EntityType,
                    Condition = request.Condition,
                    Action = request.Action,
                    Priority = request.Priority,
                    IsActive = request.IsActive
                };

                var created = await _dataExportService.CreateExportRuleAsync(rule, userId);
                var secured = await ProtectReadPayloadAsync(created, "ExportRule", created.Id.ToString());

                await LogAuditEventAsync("ExportRuleCreated", "ExportRule", created.Id.ToString(), true,
                    description: $"قانون خروجی '{request.Name}' ایجاد شد");

                return CreatedAtAction(nameof(GetExportRules), new { id = created.Id }, new
                {
                    success = true,
                    data = secured.Data,
                    securityContext = secured.SecurityContext,
                    metadata = secured.Metadata,
                    signature = secured.Signature,
                    appliedRules = secured.AppliedRules
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Data export policy denied for export rule create response");
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating export rule");
                return StatusCode(500, new { success = false, error = "خطا در ایجاد قانون" });
            }
        }

        /// <summary>
        /// به‌روزرسانی قانون خروجی
        /// </summary>
        [HttpPut("rules/{id}")]
        [RequirePermission("DataExport.Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateExportRule(Guid id, [FromBody] UpdateExportRuleRequest request)
        {
            try
            {
                var userId = GetUserId();
                var rule = new ExportRule
                {
                    Id = id,
                    Name = request.Name,
                    Description = request.Description,
                    RuleType = request.RuleType,
                    EntityType = request.EntityType,
                    Condition = request.Condition,
                    Action = request.Action,
                    Priority = request.Priority,
                    IsActive = request.IsActive
                };

                var result = await _dataExportService.UpdateExportRuleAsync(rule, userId);

                if (result)
                {
                    await LogAuditEventAsync("ExportRuleUpdated", "ExportRule", id.ToString(), true);

                    return Ok(new
                    {
                        success = true,
                        message = "قانون با موفقیت به‌روزرسانی شد"
                    });
                }

                return NotFound(new { success = false, error = "قانون یافت نشد" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating export rule");
                return StatusCode(500, new { success = false, error = "خطا در به‌روزرسانی قانون" });
            }
        }

        #endregion

        #region Masking Rules Management

        /// <summary>
        /// دریافت لیست قوانین ماسک داده‌ها
        /// FDP_ETC.2.4: ماسک داده‌های حساس
        /// </summary>
        [HttpGet("masking")]
        [RequirePermission("DataExport.Masking")]
        [ProducesResponseType(typeof(IEnumerable<DataMaskingRule>), 200)]
        public async Task<IActionResult> GetMaskingRules()
        {
            try
            {
                var rules = await _dataExportService.GetMaskingRulesAsync();
                var rulesList = rules.ToList();
                var secured = await ProtectReadPayloadAsync(rulesList, "DataMaskingRule");

                return Ok(new
                {
                    success = true,
                    count = rulesList.Count,
                    data = secured.Data,
                    securityContext = secured.SecurityContext,
                    metadata = secured.Metadata,
                    signature = secured.Signature,
                    appliedRules = secured.AppliedRules
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Data export policy denied for masking rules read");
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting masking rules");
                return StatusCode(500, new { success = false, error = "خطا در دریافت قوانین ماسک" });
            }
        }

        /// <summary>
        /// ایجاد قانون ماسک جدید
        /// </summary>
        [HttpPost("masking")]
        [RequirePermission("DataExport.Admin")]
        [ProducesResponseType(typeof(DataMaskingRule), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateMaskingRule([FromBody] CreateMaskingRuleRequest request)
        {
            try
            {
                var userId = GetUserId();
                var rule = new DataMaskingRule
                {
                    Name = request.Name,
                    Description = request.Description,
                    EntityType = request.EntityType,
                    FieldName = request.FieldName,
                    MaskingType = request.MaskingType,
                    MaskPattern = request.MaskPattern,
                    VisibleCharsStart = request.VisibleCharsStart,
                    VisibleCharsEnd = request.VisibleCharsEnd,
                    ExcludePermissions = request.ExcludePermissions,
                    IsActive = request.IsActive
                };

                var created = await _dataExportService.CreateMaskingRuleAsync(rule, userId);
                var secured = await ProtectReadPayloadAsync(created, "DataMaskingRule", created.Id.ToString());

                await LogAuditEventAsync("MaskingRuleCreated", "MaskingRule", created.Id.ToString(), true,
                    description: $"قانون ماسک '{request.Name}' ایجاد شد");

                return CreatedAtAction(nameof(GetMaskingRules), new { id = created.Id }, new
                {
                    success = true,
                    data = secured.Data,
                    securityContext = secured.SecurityContext,
                    metadata = secured.Metadata,
                    signature = secured.Signature,
                    appliedRules = secured.AppliedRules
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Data export policy denied for masking rule create response");
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating masking rule");
                return StatusCode(500, new { success = false, error = "خطا در ایجاد قانون ماسک" });
            }
        }

        /// <summary>
        /// به‌روزرسانی قانون ماسک
        /// </summary>
        [HttpPut("masking/{id}")]
        [RequirePermission("DataExport.Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateMaskingRule(Guid id, [FromBody] UpdateMaskingRuleRequest request)
        {
            try
            {
                var userId = GetUserId();
                var rule = new DataMaskingRule
                {
                    Id = id,
                    Name = request.Name,
                    Description = request.Description,
                    EntityType = request.EntityType,
                    FieldName = request.FieldName,
                    MaskingType = request.MaskingType,
                    MaskPattern = request.MaskPattern,
                    VisibleCharsStart = request.VisibleCharsStart,
                    VisibleCharsEnd = request.VisibleCharsEnd,
                    ExcludePermissions = request.ExcludePermissions,
                    IsActive = request.IsActive
                };

                var result = await _dataExportService.UpdateMaskingRuleAsync(rule, userId);

                if (result)
                {
                    await LogAuditEventAsync("MaskingRuleUpdated", "MaskingRule", id.ToString(), true);

                    return Ok(new
                    {
                        success = true,
                        message = "قانون ماسک با موفقیت به‌روزرسانی شد"
                    });
                }

                return NotFound(new { success = false, error = "قانون ماسک یافت نشد" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating masking rule");
                return StatusCode(500, new { success = false, error = "خطا در به‌روزرسانی قانون ماسک" });
            }
        }

        #endregion

        #region Sensitivity Levels

        /// <summary>
        /// دریافت سطوح حساسیت داده‌ها
        /// FDP_ETC.2.1: سطوح حساسیت
        /// </summary>
        [HttpGet("sensitivity-levels")]
        [RequirePermission("DataExport.SensitivityLevel")]
        [ProducesResponseType(typeof(IEnumerable<SensitivityLevel>), 200)]
        public async Task<IActionResult> GetSensitivityLevels()
        {
            try
            {
                var levels = await _dataExportService.GetSensitivityLevelsAsync();
                var levelsList = levels.ToList();
                var secured = await ProtectReadPayloadAsync(levelsList, "SensitivityLevel");

                return Ok(new
                {
                    success = true,
                    count = levelsList.Count,
                    data = secured.Data,
                    securityContext = secured.SecurityContext,
                    metadata = secured.Metadata,
                    signature = secured.Signature,
                    appliedRules = secured.AppliedRules
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Data export policy denied for sensitivity levels read");
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sensitivity levels");
                return StatusCode(500, new { success = false, error = "خطا در دریافت سطوح حساسیت" });
            }
        }

        /// <summary>
        /// دریافت سطح حساسیت یک Entity خاص
        /// </summary>
        [HttpGet("sensitivity-levels/{entityType}/{entityId}")]
        [RequirePermission("DataExport.Read")]
        [ProducesResponseType(typeof(SensitivityLevel), 200)]
        public async Task<IActionResult> GetEntitySensitivityLevel(string entityType, string entityId)
        {
            try
            {
                var level = await _dataExportService.GetEntitySensitivityLevelAsync(entityType, entityId);
                var secured = await ProtectReadPayloadAsync(level, "SensitivityLevel", $"{entityType}:{entityId}");

                return Ok(new
                {
                    success = true,
                    entityType,
                    entityId,
                    sensitivityLevel = secured.Data,
                    securityContext = secured.SecurityContext,
                    metadata = secured.Metadata,
                    signature = secured.Signature,
                    appliedRules = secured.AppliedRules
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Data export policy denied for entity sensitivity read. EntityType: {EntityType}, EntityId: {EntityId}", entityType, entityId);
                return StatusCode(403, new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entity sensitivity level");
                return StatusCode(500, new { success = false, error = "خطا در دریافت سطح حساسیت" });
            }
        }

        #endregion

       



        #region Helper Methods

        private async Task<SecureExportResponse<T>> ProtectReadPayloadAsync<T>(
            T data,
            string entityType,
            string? entityId = null) where T : class
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

            return await _dataExportService.WrapWithSecurityAttributesAsync(data, context);
        }

        private long GetUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private static string GetExportRuleTypeLabel(ExportRuleType type)
        {
            return type switch
            {
                ExportRuleType.VolumeLimit => "محدودیت حجم",
                ExportRuleType.RecordLimit => "محدودیت تعداد",
                ExportRuleType.TimeRestriction => "محدودیت زمانی",
                ExportRuleType.RoleBasedFilter => "فیلتر بر اساس نقش",
                ExportRuleType.SensitivityFilter => "فیلتر بر اساس سطح حساسیت",
                ExportRuleType.DataMasking => "ماسک داده",
                ExportRuleType.FieldRemoval => "حذف فیلد",
                ExportRuleType.RecipientVerification => "تایید گیرنده",
                _ => type.ToString()
            };
        }

        #endregion
    }

    #region Request DTOs

    /// <summary>
    /// آیتم نوع قانون خروجی برای فرانت (dropdown و غیره)
    /// </summary>
    public class ExportRuleTypeItem
    {
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class CreateExportRuleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ExportRuleType RuleType { get; set; }
        public string EntityType { get; set; } = "*";
        public string Condition { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int Priority { get; set; } = 100;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateExportRuleRequest : CreateExportRuleRequest { }

    public class CreateMaskingRuleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EntityType { get; set; } = "*";
        public string FieldName { get; set; } = string.Empty;
        public MaskingType MaskingType { get; set; }
        public string MaskPattern { get; set; } = "****";
        public int VisibleCharsStart { get; set; } = 0;
        public int VisibleCharsEnd { get; set; } = 4;
        public string? ExcludePermissions { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateMaskingRuleRequest : CreateMaskingRuleRequest { }

    public class TestExportRequest
    {
        public string EntityType { get; set; } = "Test";
        public string? EntityId { get; set; }
        public string Format { get; set; } = "JSON";
    }

    #endregion
}

