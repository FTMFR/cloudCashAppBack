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
    /// کنترلر مدیریت سرفصل‌های حسابداری
    /// ============================================
    /// عملیات CRUD برای تعریف و مدیریت سرفصل‌ها - پیاده‌سازی FAU_GEN
    /// ============================================
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SarfaslController : AuditControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IDataExportService _dataExportService;

        public SarfaslController(
            IMediator mediator,
            IAuditLogService auditLogService,
            IDataExportService dataExportService)
            : base(auditLogService)
        {
            _mediator = mediator;
            _dataExportService = dataExportService;
        }

        /// <summary>
        /// دریافت لیست تمام سرفصل‌ها
        /// </summary>
        /// <param name="tblShobeId">فیلتر بر اساس شعبه (اختیاری)</param>
        /// <param name="sarfaslProtocolPublicId">فیلتر بر اساس پروتکل (اختیاری)</param>
        /// <param name="sarfaslTypePublicId">فیلتر بر اساس نوع سرفصل (اختیاری)</param>
        /// <param name="onlyWithJoze">فقط سرفصل‌های با جزء تفصیلی (اختیاری)</param>
        [HttpGet]
        [RequirePermission("Sarfasl.Read")]
        public async Task<ActionResult<List<SarfaslDto>>> GetAll(
            [FromQuery] long? tblShobeId = null,
            [FromQuery] Guid? sarfaslProtocolPublicId = null,
            [FromQuery] Guid? sarfaslTypePublicId = null,
            [FromQuery] bool? onlyWithJoze = null)
        {
            try
            {
                var query = new GetAllSarfaslsQuery
                {
                    TblShobeId = tblShobeId,
                    SarfaslProtocolPublicId = sarfaslProtocolPublicId,
                    SarfaslTypePublicId = sarfaslTypePublicId,
                    OnlyWithJoze = onlyWithJoze
                };
                var result = await _mediator.Send(query);
                var protectedResult = await ProtectReadPayloadAsync(result, "SarfaslList");
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت یک سرفصل با شناسه عمومی
        /// </summary>
        [HttpGet("{publicId}")]
        [RequirePermission("Sarfasl.Read")]
        public async Task<ActionResult<SarfaslDto>> GetById(Guid publicId)
        {
            try
            {
            var query = new GetSarfaslByIdQuery { PublicId = publicId };
            var result = await _mediator.Send(query);

            if (result == null)
                return NotFound(new { success = false, message = "سرفصل یافت نشد" });

                var protectedResult = await ProtectReadPayloadAsync(result, "Sarfasl", publicId.ToString());
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت سرفصل‌ها به صورت درختی
        /// </summary>
        /// <param name="tblShobeId">فیلتر بر اساس شعبه (اختیاری)</param>
        /// <param name="sarfaslProtocolPublicId">فیلتر بر اساس پروتکل (اختیاری)</param>
        [HttpGet("tree")]
        [RequirePermission("Sarfasl.Read")]
        public async Task<ActionResult<List<SarfaslDto>>> GetTree(
            [FromQuery] long? tblShobeId = null,
            [FromQuery] Guid? sarfaslProtocolPublicId = null)
        {
            try
            {
                var query = new GetSarfaslTreeQuery
                {
                    TblShobeId = tblShobeId,
                    SarfaslProtocolPublicId = sarfaslProtocolPublicId
                };
                var result = await _mediator.Send(query);
                var protectedResult = await ProtectReadPayloadAsync(result, "SarfaslTree");
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت زیرمجموعه‌های یک سرفصل
        /// </summary>
        [HttpGet("{parentPublicId}/children")]
        [RequirePermission("Sarfasl.Read")]
        public async Task<ActionResult<List<SarfaslDto>>> GetChildren(Guid parentPublicId)
        {
            try
            {
                var query = new GetSarfaslChildrenQuery { ParentPublicId = parentPublicId };
                var result = await _mediator.Send(query);
                var protectedResult = await ProtectReadPayloadAsync(result, "SarfaslChildren", parentPublicId.ToString());
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// جستجوی سرفصل بر اساس کد
        /// </summary>
        [HttpGet("by-code/{codeSarfasl}")]
        [RequirePermission("Sarfasl.Read")]
        public async Task<ActionResult<SarfaslDto>> GetByCode(
            string codeSarfasl,
            [FromQuery] long? tblShobeId = null)
        {
            try
            {
            var query = new GetSarfaslByCodeQuery
            {
                CodeSarfasl = codeSarfasl,
                TblShobeId = tblShobeId
            };
            var result = await _mediator.Send(query);

            if (result == null)
                return NotFound(new { success = false, message = "سرفصل یافت نشد" });

                var protectedResult = await ProtectReadPayloadAsync(result, "SarfaslByCode", codeSarfasl);
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// ایجاد سرفصل جدید
        /// </summary>
        [HttpPost]
        [RequirePermission("Sarfasl.Create")]
        public async Task<ActionResult<Guid>> Create([FromBody] CreateSarfaslDto dto)
        {
            var tblUserGrpIdInsert = User.GetTblUserGrpIdInsert();
            if (tblUserGrpIdInsert == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new CreateSarfaslCommand
            {
                TblShobeId = dto.TblShobeId,
                ParentPublicId = dto.ParentPublicId,
                SarfaslTypePublicId = dto.SarfaslTypePublicId,
                SarfaslProtocolPublicId = dto.SarfaslProtocolPublicId,
                CodeSarfasl = dto.CodeSarfasl,
                Title = dto.Title,
                Description = dto.Description,
                WithJoze = dto.WithJoze,
                TblComboIdVazeiatZirGrp = dto.TblComboIdVazeiatZirGrp,
                TedadArghamZirGrp = dto.TedadArghamZirGrp,
                MizanEtebarBedehkar = dto.MizanEtebarBedehkar,
                MizanEtebarBestankar = dto.MizanEtebarBestankar,
                TblComboIdControlAmaliat = dto.TblComboIdControlAmaliat,
                NotShowInTaraz = dto.NotShowInTaraz,
                TblUserGrpIdInsert = tblUserGrpIdInsert.Value
            };

            var result = await _mediator.Send(command);
            await LogAuditEventAsync("Create", "Sarfasl", result.ToString(), true);
            return CreatedAtAction(nameof(GetById), new { publicId = result }, result);
        }

        /// <summary>
        /// ویرایش سرفصل
        /// </summary>
        [HttpPut("{publicId}")]
        [RequirePermission("Sarfasl.Update")]
        public async Task<IActionResult> Update(Guid publicId, [FromBody] UpdateSarfaslDto dto)
        {
            var tblUserGrpIdLastEdit = User.GetTblUserGrpIdLastEdit();
            if (tblUserGrpIdLastEdit == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new UpdateSarfaslCommand
            {
                PublicId = publicId,
                TblShobeId = dto.TblShobeId,
                ParentPublicId = dto.ParentPublicId,
                SarfaslTypePublicId = dto.SarfaslTypePublicId,
                SarfaslProtocolPublicId = dto.SarfaslProtocolPublicId,
                CodeSarfasl = dto.CodeSarfasl,
                Title = dto.Title,
                Description = dto.Description,
                WithJoze = dto.WithJoze,
                TblComboIdVazeiatZirGrp = dto.TblComboIdVazeiatZirGrp,
                TedadArghamZirGrp = dto.TedadArghamZirGrp,
                MizanEtebarBedehkar = dto.MizanEtebarBedehkar,
                MizanEtebarBestankar = dto.MizanEtebarBestankar,
                TblComboIdControlAmaliat = dto.TblComboIdControlAmaliat,
                NotShowInTaraz = dto.NotShowInTaraz,
                TblUserGrpIdLastEdit = tblUserGrpIdLastEdit.Value,
                AuditUserId = User.GetUserId()
            };

            var result = await _mediator.Send(command);

            if (!result)
            {
                await LogAuditEventAsync("Update", "Sarfasl", publicId.ToString(), false, "خطا در انجام عملیات");
                return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
            }

            return Ok(new ResultDto(true, "عملیات با موفقیت انجام شد"));
        }

        /// <summary>
        /// حذف سرفصل
        /// </summary>
        [HttpDelete("{publicId}")]
        [RequirePermission("Sarfasl.Delete")]
        public async Task<IActionResult> Delete(Guid publicId)
        {
            var tblUserGrpIdLastEdit = User.GetTblUserGrpIdLastEdit();
            if (tblUserGrpIdLastEdit == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new DeleteSarfaslCommand
            {
                PublicId = publicId,
                TblUserGrpIdLastEdit = tblUserGrpIdLastEdit.Value
            };

            var result = await _mediator.Send(command);

            if (!result)
            {
                await LogAuditEventAsync("Delete", "Sarfasl", publicId.ToString(), false, "خطا در انجام عملیات");
                return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
            }

            await LogAuditEventAsync("Delete", "Sarfasl", publicId.ToString(), true);
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
