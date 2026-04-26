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
using System.Security.Claims;
using System.Threading;

namespace BnpCashClaudeApp.api.Controllers.Navigation
{
    /// <summary>
    /// کنترلر مدیریت گروه‌ها
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF و FAU_GEN از استاندارد ISO 15408
    /// ============================================
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class GrpController : AuditControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IDataExportService _dataExportService;

        public GrpController(
            IMediator mediator,
            IDataExportService dataExportService,
            IAuditLogService auditLogService)
            : base(auditLogService)
        {
            _mediator = mediator;
            _dataExportService = dataExportService;
        }

        /// <summary>
        /// دریافت لیست تمام گروه‌ها
        /// </summary>
        [HttpGet]
        [RequirePermission("Groups.Read")]
        public async Task<ActionResult<List<GrpDto>>> GetAll()
        {
            try
            {
                var query = new GetAllGrpsQuery();
                var result = await _mediator.Send(query);
                var protectedResult = await ProtectReadPayloadAsync(result, "Group");
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// دریافت یک گروه با شناسه عمومی
        /// </summary>
        [HttpGet("{publicId}")]
        [RequirePermission("Groups.Read")]
        public async Task<ActionResult<GrpDto>> GetById(Guid publicId)
        {
            try
            {
                var query = new GetGrpByIdQuery { PublicId = publicId };
                var result = await _mediator.Send(query);

                if (result == null)
                    return NotFound(new { success = false, message = "گروه یافت نشد" });

                var protectedResult = await ProtectReadPayloadAsync(result, "Group", publicId.ToString());
                return Ok(protectedResult);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(403, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// ایجاد گروه جدید
        /// </summary>
        [HttpPost]
        [RequirePermission("Groups.Create")]
        public async Task<ActionResult<Guid>> Create([FromBody] CreateGrpDto dto)
        {
            var tblUserGrpIdInsert = User.GetTblUserGrpIdInsert();
            if (tblUserGrpIdInsert == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");

            var command = new CreateGrpCommand
            {
                Title = dto.Title,
                TblUserGrpIdInsert = tblUserGrpIdInsert.Value,
                ParentPublicId = dto.ParentPublicId,
                Description = dto.Title,
                ShobePublicId = dto.ShobePublicId
            };

            var result = await _mediator.Send(command);
            await LogAuditEventAsync("Create", "Grp", result.ToString(), true);
            //return CreatedAtAction(nameof(GetById), new { publicId = result }, result);
            return Ok(new ResultDto(true, "عملیات با موفقیت انجام شد"));
        }

        /// <summary>
        /// ویرایش گروه
        /// </summary>
        [HttpPut("{publicId}")]
        [RequirePermission("Groups.Update")]
        public async Task<IActionResult> Update(Guid publicId, [FromBody] UpdateGrpDto dto)
        {
            var tblUserGrpIdLastEdit = User.GetTblUserGrpIdLastEdit();
            if (tblUserGrpIdLastEdit == null)
                return Problem(
                    detail: "شناسه گروه کاربری در توکن یافت نشد.",
                    statusCode: 403,
                    title: "Forbidden");
            var command = new UpdateGrpCommand
            {
                PublicId = publicId,
                Title = dto.Title,
                TblUserGrpIdLastEdit = tblUserGrpIdLastEdit.Value,
                ParentPublicId = dto.ParentPublicId,
                Description = dto.Title,
                ShobePublicId = dto.ShobePublicId,
                AuditUserId = User.GetUserId()
            };

            var result = await _mediator.Send(command);

            if (!result)
            {
                await LogAuditEventAsync("Update", "Grp", publicId.ToString(), false, "خطا در انجام عملیات");
                return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
            }

            return Ok(new ResultDto(true, "عملیات با موفقیت انجام شد"));
        }

        /// <summary>
        /// حذف گروه
        /// </summary>
        [HttpDelete("{publicId}")]
        [RequirePermission("Groups.Delete")]
        public async Task<IActionResult> Delete(Guid publicId)
        {
            try
            {
                var command = new DeleteGrpCommand { PublicId = publicId };

                var result = await _mediator.Send(command);

                if (!result)
                {
                    await LogAuditEventAsync("Delete", "Grp", publicId.ToString(), false, "خطا در انجام عملیات");
                    return Ok(new ResultDto(false, "خطا در انجام عملیات!"));
                }

                await LogAuditEventAsync("Delete", "Grp", publicId.ToString(), true);
                return Ok(new ResultDto(true, "عملیات با موفقیت انجام شد"));
            }
            catch (Exception)
            {
                return Ok(new ResultDto(false, "خطا در انجام عملیات ، اماکان داشتن زیر گروه یا وابسته بودن به کاربر"));
            }

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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
