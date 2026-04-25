using BnpCashClaudeApp.api.Attributes;
using BnpCashClaudeApp.api.Controllers.Base;
using BnpCashClaudeApp.api.Extensions;
using BnpCashClaudeApp.api.Helpers;
using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Application.DTOs.Common;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.MediatR.Commands;
using BnpCashClaudeApp.Application.MediatR.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading;

namespace BnpCashClaudeApp.api.Controllers.Navigation
{
    /// <summary>
    /// کنترلر مدیریت شعب
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF و FAU_GEN از استاندارد ISO 15408
    /// ============================================
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("ApiPolicy")]
    public class ShobeController : AuditControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IDataExportService _dataExportService;

        public ShobeController(
            IMediator mediator,
            IDataExportService dataExportService,
            IAuditLogService auditLogService)
            : base(auditLogService)
        {
            _mediator = mediator;
            _dataExportService = dataExportService;
        }

        /// <summary>
        /// دریافت لیست تمام شعب
        /// </summary>
        [HttpGet]
        [RequirePermission("Shobes.Read")]
        public async Task<ActionResult<List<ShobeDto>>> GetAll()
        {
            try
            {
                var query = new GetAllShobesQuery();
                var result = await _mediator.Send(query);
                var protectedResult = await ProtectReadPayloadAsync(result, "Shobe");
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت یک شعبه با شناسه عمومی
        /// </summary>
        [HttpGet("{publicId}")]
        [RequirePermission("Shobes.Read")]
        public async Task<ActionResult<ShobeDto>> GetById(Guid publicId)
        {
            try
            {
                var query = new GetShobeByIdQuery { PublicId = publicId };
                var result = await _mediator.Send(query);

                if (result == null)
                    return NotFound(new { success = false, message = "شعبه یافت نشد" });

                var protectedResult = await ProtectReadPayloadAsync(result, "Shobe", publicId.ToString());
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// ایجاد شعبه جدید
        /// </summary>
        [HttpPost]
        [RequirePermission("Shobes.Create")]
        public async Task<ActionResult<Guid>> Create([FromBody] CreateShobeDto dto)
        {
            var tblUserGrpIdInsert = User.GetTblUserGrpIdInsert();
            if (tblUserGrpIdInsert == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new CreateShobeCommand
            {
                Title = dto.Title,
                ShobeCode = dto.ShobeCode,
                Address = dto.Address,
                Phone = dto.Phone,
                PostalCode = dto.PostalCode,
                ParentPublicId = dto.ParentPublicId,
                IsActive = dto.IsActive,
                Description = dto.Description,
                DisplayOrder = dto.DisplayOrder,
                TblUserGrpIdInsert = tblUserGrpIdInsert.Value
            };

            try
            {
                var result = await _mediator.Send(command);
                await LogAuditEventAsync("Create", "Shobe", result.ToString(), true);
                return CreatedAtAction(nameof(GetById), new { publicId = result }, result);
            }
            catch (ArgumentException ex)
            {
                await LogAuditEventAsync("Create", "Shobe", null, false, ex.Message);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// ویرایش شعبه
        /// </summary>
        [HttpPut("{publicId}")]
        [RequirePermission("Shobes.Update")]
        public async Task<IActionResult> Update(Guid publicId, [FromBody] UpdateShobeDto dto)
        {
            var tblUserGrpIdLastEdit = User.GetTblUserGrpIdLastEdit();
            if (tblUserGrpIdLastEdit == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new UpdateShobeCommand
            {
                PublicId = publicId,
                Title = dto.Title,
                ShobeCode = dto.ShobeCode,
                Address = dto.Address,
                Phone = dto.Phone,
                PostalCode = dto.PostalCode,
                ParentPublicId = dto.ParentPublicId,
                IsActive = dto.IsActive,
                Description = dto.Description,
                DisplayOrder = dto.DisplayOrder,
                TblUserGrpIdLastEdit = tblUserGrpIdLastEdit.Value,
                AuditUserId = User.GetUserId()
            };

            try
            {
                var result = await _mediator.Send(command);

                if (!result)
                {
                    await LogAuditEventAsync("Update", "Shobe", publicId.ToString(), false, "خطا در انجام عملیات");
                    return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
                }

                return Ok(new ResultDto(true, "عملیات با موفقیت انجام شد"));
            }
            catch (ArgumentException ex)
            {
                await LogAuditEventAsync("Update", "Shobe", publicId.ToString(), false, ex.Message);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// حذف شعبه
        /// </summary>
        [HttpDelete("{publicId}")]
        [RequirePermission("Shobes.Delete")]
        public async Task<IActionResult> Delete(Guid publicId)
        {
            var command = new DeleteShobeCommand { PublicId = publicId };

            try
            {
                var result = await _mediator.Send(command);

                if (!result)
                {
                    await LogAuditEventAsync("Delete", "Shobe", publicId.ToString(), false, "خطا در انجام عملیات");
                    return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
                }

                await LogAuditEventAsync("Delete", "Shobe", publicId.ToString(), true);
                return Ok(new ResultDto(true, "عملیات با موفقیت انجام شد"));
            }
            catch (InvalidOperationException ex)
            {
                await LogAuditEventAsync("Delete", "Shobe", publicId.ToString(), false, ex.Message);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============================================
        // تنظیمات شعبه
        // ============================================

        /// <summary>
        /// دریافت لیست تنظیمات شعبه
        /// </summary>
        /// <param name="shobePublicId">شناسه شعبه (اختیاری) - اگر null باشد، همه تنظیمات برگردانده می‌شود</param>
        [HttpGet("Settings")]
        [RequirePermission("Shobes.Settings")]
        public async Task<ActionResult<List<ShobeSettingDto>>> GetSettings([FromQuery] Guid? shobePublicId = null)
        {
            try
            {
                var query = new GetAllShobeSettingsQuery { ShobePublicId = shobePublicId };
                var result = await _mediator.Send(query);
                var protectedResult = await ProtectReadPayloadAsync(
                    result,
                    "ShobeSetting",
                    shobePublicId?.ToString());
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت یک تنظیمات با شناسه عمومی
        /// </summary>
        [HttpGet("Settings/{publicId}")]
        [RequirePermission("Shobes.Settings")]
        public async Task<ActionResult<ShobeSettingDto>> GetSettingById(Guid publicId)
        {
            try
            {
                var query = new GetShobeSettingByIdQuery { PublicId = publicId };
                var result = await _mediator.Send(query);

                if (result == null)
                    return NotFound(new { success = false, message = "تنظیمات یافت نشد" });

                var protectedResult = await ProtectReadPayloadAsync(result, "ShobeSetting", publicId.ToString());
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// ایجاد تنظیمات شعبه جدید
        /// </summary>
        [HttpPost("Settings")]
        [RequirePermission("Shobes.Settings")]
        public async Task<ActionResult<Guid>> CreateSetting([FromBody] CreateShobeSettingDto dto)
        {
            var tblUserGrpIdInsert = User.GetTblUserGrpIdInsert();
            if (tblUserGrpIdInsert == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new CreateShobeSettingCommand
            {
                ShobePublicId = dto.ShobePublicId,
                SettingKey = dto.SettingKey,
                SettingName = dto.SettingName,
                Description = dto.Description,
                SettingValue = dto.SettingValue,
                SettingType = dto.SettingType,
                IsActive = dto.IsActive,
                IsEditable = dto.IsEditable,
                DisplayOrder = dto.DisplayOrder,
                TblUserGrpIdInsert = tblUserGrpIdInsert.Value
            };

            try
            {
                var result = await _mediator.Send(command);
                await LogAuditEventAsync("Create", "ShobeSetting", result.ToString(), true);
                return CreatedAtAction(nameof(GetSettingById), new { publicId = result }, result);
            }
            catch (ArgumentException ex)
            {
                await LogAuditEventAsync("Create", "ShobeSetting", null, false, ex.Message);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// ویرایش تنظیمات شعبه
        /// </summary>
        //[HttpPut("Settings/{publicId}")]
        //[RequirePermission("Shobes.Settings")]
        //public async Task<IActionResult> UpdateSetting(Guid publicId, [FromBody] UpdateShobeSettingDto dto)
        //{
        //    var tblUserGrpIdLastEdit = User.GetTblUserGrpIdLastEdit();
        //    if (tblUserGrpIdLastEdit == null)
        //        return Problem(
        //            detail: "شناسه گروه کاربری در توکن یافت نشد.",
        //            statusCode: 403,
        //            title: "Forbidden");

        //    var command = new UpdateShobeSettingCommand
        //    {
        //        PublicId = publicId,
        //        SettingName = dto.SettingName,
        //        Description = dto.Description,
        //        SettingValue = dto.SettingValue,
        //        SettingType = dto.SettingType,
        //        IsActive = dto.IsActive,
        //        IsEditable = dto.IsEditable,
        //        DisplayOrder = dto.DisplayOrder,
        //        TblUserGrpIdLastEdit = tblUserGrpIdLastEdit.Value,
        //        AuditUserId = User.GetUserId()
        //    };

        //    try
        //    {
        //        var result = await _mediator.Send(command);

        //        if (!result)
        //        {
        //            await LogAuditEventAsync("Update", "ShobeSetting", publicId.ToString(), false, "خطا در انجام عملیات");
        //            return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
        //        }

        //        return Ok(new ResultDto(true, "عملیات با موفقیت انجام شد"));
        //    }
        //    catch (ArgumentException ex)
        //    {
        //        await LogAuditEventAsync("Update", "ShobeSetting", publicId.ToString(), false, ex.Message);
        //        return BadRequest(new { success = false, message = ex.Message });
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        await LogAuditEventAsync("Update", "ShobeSetting", publicId.ToString(), false, ex.Message);
        //        return BadRequest(new { success = false, message = ex.Message });
        //    }
        //}

        /// <summary>
        /// حذف تنظیمات شعبه
        /// </summary>
        //[HttpDelete("Settings/{publicId}")]
        //[RequirePermission("Shobes.Settings")]
        //public async Task<IActionResult> DeleteSetting(Guid publicId)
        //{
        //    var command = new DeleteShobeSettingCommand { PublicId = publicId };

        //    try
        //    {
        //        var result = await _mediator.Send(command);

        //        if (!result)
        //        {
        //            await LogAuditEventAsync("Delete", "ShobeSetting", publicId.ToString(), false, "خطا در انجام عملیات");
        //            return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
        //        }

        //        await LogAuditEventAsync("Delete", "ShobeSetting", publicId.ToString(), true);
        //        return Ok(new ResultDto(true, "عملیات با موفقیت انجام شد"));
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        await LogAuditEventAsync("Delete", "ShobeSetting", publicId.ToString(), false, ex.Message);
        //        return BadRequest(new { success = false, message = ex.Message });
        //    }
        //}

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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
