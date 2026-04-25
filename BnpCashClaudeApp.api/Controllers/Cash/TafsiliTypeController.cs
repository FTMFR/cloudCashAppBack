using BnpCashClaudeApp.api.Attributes;
using BnpCashClaudeApp.api.Controllers.Base;
using BnpCashClaudeApp.api.Extensions;
using BnpCashClaudeApp.api.Helpers;
using BnpCashClaudeApp.Application.DTOs.CashDtos;
using BnpCashClaudeApp.Application.DTOs.Common;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.MediatR.Commands.Cash;
using BnpCashClaudeApp.Application.MediatR.Queries.Cash;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;

namespace BnpCashClaudeApp.api.Controllers.Cash
{
    /// <summary>
    /// کنترلر مدیریت انواع مشتری (انواع تفصیلی)
    /// ============================================
    /// عملیات CRUD برای تعریف انواع مشتری - پیاده‌سازی FAU_GEN
    /// ============================================
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TafsiliTypeController : AuditControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IDataExportService _dataExportService;

        public TafsiliTypeController(
            IMediator mediator,
            IAuditLogService auditLogService,
            IDataExportService dataExportService)
            : base(auditLogService)
        {
            _mediator = mediator;
            _dataExportService = dataExportService;
        }

        /// <summary>
        /// دریافت لیست تمام انواع مشتری
        /// </summary>
        /// <param name="shobePublicId">فیلتر بر اساس شعبه (اختیاری)</param>
        /// <param name="onlyActive">فقط موارد فعال (پیش‌فرض: true)</param>
        [HttpGet]
        [RequirePermission("TafsiliType.Read")]
        public async Task<ActionResult<List<TafsiliTypeDto>>> GetAll(
            [FromQuery] Guid? shobePublicId = null,
            [FromQuery] bool onlyActive = true)
        {
            try
            {
                var query = new GetAllTafsiliTypesQuery
                {
                    ShobePublicId = shobePublicId,
                    OnlyActive = onlyActive
                };
                var result = await _mediator.Send(query);
                var protectedResult = await ProtectReadPayloadAsync(result, "TafsiliTypeList");
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت یک نوع مشتری با شناسه عمومی
        /// </summary>
        [HttpGet("{publicId}")]
        [RequirePermission("TafsiliType.Read")]
        public async Task<ActionResult<TafsiliTypeDto>> GetById(Guid publicId)
        {
            try
            {
            var query = new GetTafsiliTypeByIdQuery { PublicId = publicId };
            var result = await _mediator.Send(query);

            if (result == null)
                return NotFound(new { success = false, message = "نوع مشتری یافت نشد" });

                var protectedResult = await ProtectReadPayloadAsync(result, "TafsiliType", publicId.ToString());
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت انواع مشتری به صورت درختی
        /// </summary>
        /// <param name="shobePublicId">فیلتر بر اساس شعبه (اختیاری)</param>
        /// <param name="onlyActive">فقط موارد فعال (پیش‌فرض: true)</param>
        [HttpGet("tree")]
        [RequirePermission("TafsiliType.Read")]
        public async Task<ActionResult<List<TafsiliTypeDto>>> GetTree(
            [FromQuery] Guid? shobePublicId = null,
            [FromQuery] bool onlyActive = true)
        {
            try
            {
                var query = new GetTafsiliTypeTreeQuery
                {
                    ShobePublicId = shobePublicId,
                    OnlyActive = onlyActive
                };
                var result = await _mediator.Send(query);
                var protectedResult = await ProtectReadPayloadAsync(result, "TafsiliTypeTree");
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// ایجاد نوع مشتری جدید
        /// </summary>
        [HttpPost]
        [RequirePermission("TafsiliType.Create")]
        public async Task<ActionResult<Guid>> Create([FromBody] CreateTafsiliTypeDto dto)
        {
            var tblUserGrpIdInsert = User.GetTblUserGrpIdInsert();
            if (tblUserGrpIdInsert == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new CreateTafsiliTypeCommand
            {
                ShobePublicId = dto.ShobePublicId,
                ParentPublicId = dto.ParentPublicId,
                Title = dto.Title,
                TblUserGrpIdInsert = tblUserGrpIdInsert.Value
            };

            var result = await _mediator.Send(command);
            await LogAuditEventAsync("Create", "TafsiliType", result.ToString(), true);
            return CreatedAtAction(nameof(GetById), new { publicId = result }, result);
        }

        /// <summary>
        /// ویرایش نوع مشتری
        /// </summary>
        [HttpPut("{publicId}")]
        [RequirePermission("TafsiliType.Update")]
        public async Task<IActionResult> Update(Guid publicId, [FromBody] UpdateTafsiliTypeDto dto)
        {
            var tblUserGrpIdLastEdit = User.GetTblUserGrpIdLastEdit();
            if (tblUserGrpIdLastEdit == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new UpdateTafsiliTypeCommand
            {
                PublicId = publicId,
                ShobePublicId = dto.ShobePublicId,
                ParentPublicId = dto.ParentPublicId,
                Title = dto.Title,
                IsActive = dto.IsActive,
                TblUserGrpIdLastEdit = tblUserGrpIdLastEdit.Value,
                AuditUserId = User.GetUserId()
            };

            var result = await _mediator.Send(command);

            if (!result)
            {
                await LogAuditEventAsync("Update", "TafsiliType", publicId.ToString(), false, "خطا در انجام عملیات");
                return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
            }

            return Ok(new ResultDto(true, "عملیات با موفقیت انجام شد"));
        }

        /// <summary>
        /// حذف نوع مشتری (Soft Delete)
        /// </summary>
        [HttpDelete("{publicId}")]
        [RequirePermission("TafsiliType.Delete")]
        public async Task<IActionResult> Delete(Guid publicId)
        {
            var tblUserGrpIdLastEdit = User.GetTblUserGrpIdLastEdit();
            if (tblUserGrpIdLastEdit == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new DeleteTafsiliTypeCommand
            {
                PublicId = publicId,
                TblUserGrpIdLastEdit = tblUserGrpIdLastEdit.Value
            };

            var result = await _mediator.Send(command);

            if (!result)
            {
                await LogAuditEventAsync("Delete", "TafsiliType", publicId.ToString(), false, "خطا در انجام عملیات");
                return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
            }

            await LogAuditEventAsync("Delete", "TafsiliType", publicId.ToString(), true);
            return Ok(new ResultDto(true, "عملیات با موفقیت انجام شد"));
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
