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
    /// کنترلر مدیریت انواع حوزه (دسته‌بندی)
    /// ============================================
    /// عملیات CRUD برای تعریف انواع حوزه - پیاده‌سازی FAU_GEN
    /// ============================================
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AzaNoeController : AuditControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IDataExportService _dataExportService;

        public AzaNoeController(
            IMediator mediator,
            IAuditLogService auditLogService,
            IDataExportService dataExportService)
            : base(auditLogService)
        {
            _mediator = mediator;
            _dataExportService = dataExportService;
        }

        /// <summary>
        /// دریافت لیست تمام حوزه‌ها
        /// </summary>
        /// <param name="shobePublicId">فیلتر بر اساس شعبه (اختیاری)</param>
        /// <param name="tafsiliTypePublicId">فیلتر بر اساس نوع مشتری (اختیاری)</param>
        /// <param name="onlyActive">فقط موارد فعال (پیش‌فرض: true)</param>
        [HttpGet]
        [RequirePermission("AzaNoe.Read")]
        public async Task<ActionResult<List<AzaNoeDto>>> GetAll(
            [FromQuery] Guid? shobePublicId = null,
            [FromQuery] Guid? tafsiliTypePublicId = null,
            [FromQuery] bool onlyActive = true)
        {
            try
            {
                var query = new GetAllAzaNoesQuery
                {
                    ShobePublicId = shobePublicId,
                    TafsiliTypePublicId = tafsiliTypePublicId,
                    OnlyActive = onlyActive
                };
                var result = await _mediator.Send(query);
                var protectedResult = await ProtectReadPayloadAsync(result, "AzaNoeList");
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت یک حوزه با شناسه عمومی
        /// </summary>
        [HttpGet("{publicId}")]
        [RequirePermission("AzaNoe.Read")]
        public async Task<ActionResult<AzaNoeDto>> GetById(Guid publicId)
        {
            try
            {
            var query = new GetAzaNoeByIdQuery { PublicId = publicId };
            var result = await _mediator.Send(query);

            if (result == null)
                return NotFound(new { success = false, message = "حوزه یافت نشد" });

                var protectedResult = await ProtectReadPayloadAsync(result, "AzaNoe", publicId.ToString());
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت حوزه‌های یک نوع مشتری خاص
        /// </summary>
        [HttpGet("by-tafsili-type/{tafsiliTypePublicId}")]
        [RequirePermission("AzaNoe.Read")]
        public async Task<ActionResult<List<AzaNoeDto>>> GetByTafsiliType(
            Guid tafsiliTypePublicId,
            [FromQuery] bool onlyActive = true)
        {
            try
            {
            var query = new GetAzaNoesByTafsiliTypeQuery
            {
                TafsiliTypePublicId = tafsiliTypePublicId,
                OnlyActive = onlyActive
            };
            var result = await _mediator.Send(query);
                var protectedResult = await ProtectReadPayloadAsync(
                    result,
                    "AzaNoeListByTafsiliType",
                    tafsiliTypePublicId.ToString());
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// ایجاد حوزه جدید
        /// </summary>
        [HttpPost]
        [RequirePermission("AzaNoe.Create")]
        public async Task<ActionResult<Guid>> Create([FromBody] CreateAzaNoeDto dto)
        {
            var tblUserGrpIdInsert = User.GetTblUserGrpIdInsert();
            if (tblUserGrpIdInsert == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new CreateAzaNoeCommand
            {
                ShobePublicId = dto.ShobePublicId,
                Title = dto.Title,
                CodeHoze = dto.CodeHoze,
                PishFarz = dto.PishFarz,
                TafsiliTypePublicId = dto.TafsiliTypePublicId,
                TblUserGrpIdInsert = tblUserGrpIdInsert.Value
            };

            var result = await _mediator.Send(command);
            await LogAuditEventAsync("Create", "AzaNoe", result.ToString(), true);
            return CreatedAtAction(nameof(GetById), new { publicId = result }, result);
        }

        /// <summary>
        /// ویرایش حوزه
        /// </summary>
        [HttpPut("{publicId}")]
        [RequirePermission("AzaNoe.Update")]
        public async Task<IActionResult> Update(Guid publicId, [FromBody] UpdateAzaNoeDto dto)
        {
            var tblUserGrpIdLastEdit = User.GetTblUserGrpIdLastEdit();
            if (tblUserGrpIdLastEdit == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new UpdateAzaNoeCommand
            {
                PublicId = publicId,
                ShobePublicId = dto.ShobePublicId,
                Title = dto.Title,
                CodeHoze = dto.CodeHoze,
                PishFarz = dto.PishFarz,
                TafsiliTypePublicId = dto.TafsiliTypePublicId,
                IsActive = dto.IsActive,
                TblUserGrpIdLastEdit = tblUserGrpIdLastEdit.Value,
                AuditUserId = User.GetUserId()
            };

            var result = await _mediator.Send(command);

            if (!result)
            {
                await LogAuditEventAsync("Update", "AzaNoe", publicId.ToString(), false, "خطا در انجام عملیات");
                return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
            }

            return Ok(new ResultDto(true, "عملیات با موفقیت انجام شد"));
        }

        /// <summary>
        /// حذف حوزه (Soft Delete)
        /// </summary>
        [HttpDelete("{publicId}")]
        [RequirePermission("AzaNoe.Delete")]
        public async Task<IActionResult> Delete(Guid publicId)
        {
            var tblUserGrpIdLastEdit = User.GetTblUserGrpIdLastEdit();
            if (tblUserGrpIdLastEdit == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new DeleteAzaNoeCommand
            {
                PublicId = publicId,
                TblUserGrpIdLastEdit = tblUserGrpIdLastEdit.Value
            };

            var result = await _mediator.Send(command);

            if (!result)
            {
                await LogAuditEventAsync("Delete", "AzaNoe", publicId.ToString(), false, "خطا در انجام عملیات");
                return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
            }

            await LogAuditEventAsync("Delete", "AzaNoe", publicId.ToString(), true);
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
