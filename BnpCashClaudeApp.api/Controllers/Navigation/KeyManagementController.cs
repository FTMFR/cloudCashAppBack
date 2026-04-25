// using BnpCashClaudeApp.Application.Interfaces;
// using BnpCashClaudeApp.api.Attributes;
// using BnpCashClaudeApp.api.Helpers;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using System;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;

// namespace BnpCashClaudeApp.api.Controllers.Navigation
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     [Authorize]
//     public class KeyManagementController : ControllerBase
//     {
//         private readonly IKeyGenerationService _keyGenerationService;
//         private readonly IKeyManagementService _keyManagementService;
//         private readonly IAuditLogService _auditLogService;
//         private readonly IDataExportService _dataExportService;

//         public KeyManagementController(
//             IKeyGenerationService keyGenerationService,
//             IKeyManagementService keyManagementService,
//             IAuditLogService auditLogService,
//             IDataExportService dataExportService)
//         {
//             _keyGenerationService = keyGenerationService;
//             _keyManagementService = keyManagementService;
//             _auditLogService = auditLogService;
//             _dataExportService = dataExportService;
//         }

//         [HttpGet("statistics")]
//         [RequirePermission("Security.Read")]
//         public async Task<IActionResult> GetStatistics(CancellationToken ct)
//         {
//             try
//             {
//                 var statistics = await _keyManagementService.GetKeyStatisticsAsync(ct);
//                 var protectedStatistics = await ProtectReadPayloadAsync(
//                     statistics,
//                     "CryptographicKeyStatistics",
//                     ct: ct);

//                 return Ok(protectedStatistics);
//             }
//             catch (InvalidOperationException ex)
//             {
//                 return StatusCode(403, new { success = false, error = ex.Message });
//             }
//         }

//         [HttpGet("by-purpose/{purpose}")]
//         [RequirePermission("Security.Read")]
//         public async Task<IActionResult> GetKeysByPurpose(
//             string purpose,
//             [FromQuery] bool includeExpired = false,
//             CancellationToken ct = default)
//         {
//             try
//             {
//                 var keys = await _keyManagementService.GetKeysByPurposeAsync(purpose, includeExpired, ct);

//                 var result = keys.Select(k => new
//                 {
//                     k.KeyId,
//                     k.Purpose,
//                     k.Status,
//                     k.CreatedAt,
//                     k.ExpiresAt,
//                     k.LastUsedAt,
//                     k.KeyLengthBits
//                 }).ToList();

//                 var protectedResult = await ProtectReadPayloadAsync(result, "CryptographicKey", purpose, ct);
//                 return Ok(protectedResult);
//             }
//             catch (InvalidOperationException ex)
//             {
//                 return StatusCode(403, new { success = false, error = ex.Message });
//             }
//         }

//         [HttpPost("create")]
//         [RequirePermission("Security.Write")]
//         public async Task<IActionResult> CreateKey(
//             [FromBody] CreateKeyRequest request,
//             CancellationToken ct)
//         {
//             if (string.IsNullOrEmpty(request.Purpose))
//             {
//                 return BadRequest(new { message = "Purpose is required" });
//             }

//             byte[] keyValue = _keyGenerationService.GenerateSymmetricKey(request.KeyLengthBits);

//             try
//             {
//                 var keyId = await _keyManagementService.StoreKeyAsync(
//                     request.Purpose,
//                     keyValue,
//                     request.ExpiresAt,
//                     ct);

//                 await _auditLogService.LogEventAsync(
//                     eventType: "KeyCreatedByAdmin",
//                     entityType: "CryptographicKey",
//                     entityId: keyId.ToString(),
//                     isSuccess: true,
//                     description: $"Administrator created new key for purpose: {request.Purpose}, Length: {request.KeyLengthBits} bits");

//                 return Ok(new { keyId, message = "Key created successfully" });
//             }
//             finally
//             {
//                 Array.Clear(keyValue, 0, keyValue.Length);
//             }
//         }

//         [HttpPost("rotate/{purpose}")]
//         [RequirePermission("Security.Write")]
//         public async Task<IActionResult> RotateKey(
//             string purpose,
//             [FromBody] RotateKeyRequest request,
//             CancellationToken ct)
//         {
//             var newKeyId = await _keyManagementService.AutoRotateKeyAsync(
//                 purpose,
//                 request.KeyLengthBits,
//                 request.GracePeriodMinutes,
//                 ct);

//             await _auditLogService.LogEventAsync(
//                 eventType: "KeyRotatedByAdmin",
//                 entityType: "CryptographicKey",
//                 entityId: newKeyId.ToString(),
//                 isSuccess: true,
//                 description: $"Administrator rotated key for purpose: {purpose}, Grace period: {request.GracePeriodMinutes} min");

//             return Ok(new { newKeyId, message = "Key rotated successfully" });
//         }

//         [HttpGet("needs-rotation/{purpose}")]
//         [RequirePermission("Security.Read")]
//         public async Task<IActionResult> CheckNeedsRotation(
//             string purpose,
//             [FromQuery] int maxAgeDays = 90,
//             CancellationToken ct = default)
//         {
//             try
//             {
//                 var needsRotation = await _keyManagementService.NeedsRotationAsync(purpose, maxAgeDays, ct);
//                 var response = new { purpose, needsRotation, maxAgeDays };
//                 var protectedResponse = await ProtectReadPayloadAsync(response, "CryptographicKeyRotation", purpose, ct);

//                 return Ok(protectedResponse);
//             }
//             catch (InvalidOperationException ex)
//             {
//                 return StatusCode(403, new { success = false, error = ex.Message });
//             }
//         }

//         [HttpPost("deactivate/{keyId}")]
//         [RequirePermission("Security.Write")]
//         public async Task<IActionResult> DeactivateKey(
//             Guid keyId,
//             [FromBody] DeactivateKeyRequest request,
//             CancellationToken ct)
//         {
//             await _keyManagementService.DeactivateKeyAsync(keyId, request.Reason, ct);

//             await _auditLogService.LogEventAsync(
//                 eventType: "KeyDeactivatedByAdmin",
//                 entityType: "CryptographicKey",
//                 entityId: keyId.ToString(),
//                 isSuccess: true,
//                 description: $"Administrator deactivated key: {keyId}, Reason: {request.Reason}");

//             return Ok(new { message = "Key deactivated successfully" });
//         }

//         [HttpDelete("destroy/{keyId}")]
//         [RequirePermission("Security.Delete")]
//         public async Task<IActionResult> DestroyKey(
//             Guid keyId,
//             [FromBody] DestroyKeyRequest request,
//             CancellationToken ct)
//         {
//             await _keyManagementService.DestroyKeyAsync(keyId, request.Reason, ct);

//             await _auditLogService.LogEventAsync(
//                 eventType: "KeyDestroyedByAdmin",
//                 entityType: "CryptographicKey",
//                 entityId: keyId.ToString(),
//                 isSuccess: true,
//                 description: $"Administrator destroyed key: {keyId}, Reason: {request.Reason}");

//             return Ok(new { message = "Key destroyed successfully" });
//         }

//         [HttpDelete("destroy-expired")]
//         [RequirePermission("Security.Delete")]
//         public async Task<IActionResult> DestroyExpiredKeys(CancellationToken ct)
//         {
//             var count = await _keyManagementService.DestroyExpiredKeysAsync(ct);

//             await _auditLogService.LogEventAsync(
//                 eventType: "ExpiredKeysDestroyedByAdmin",
//                 entityType: "CryptographicKey",
//                 isSuccess: true,
//                 description: $"Administrator destroyed {count} expired keys");

//             return Ok(new { destroyedCount = count, message = $"{count} expired keys destroyed" });
//         }

//         [HttpPost("validate-strength")]
//         [RequirePermission("Security.Read")]
//         public IActionResult ValidateKeyStrength([FromBody] ValidateKeyRequest request)
//         {
//             var result = _keyGenerationService.ValidateKeyStrengthBase64(
//                 request.KeyBase64,
//                 request.MinimumBits);

//             return Ok(result);
//         }

//         private async Task<T> ProtectReadPayloadAsync<T>(
//             T data,
//             string entityType,
//             string? entityId = null,
//             CancellationToken ct = default) where T : class
//         {
//             var context = new ExportContext
//             {
//                 EntityType = entityType,
//                 EntityId = entityId,
//                 UserId = GetUserId(),
//                 UserName = User.Identity?.Name ?? "Unknown",
//                 IpAddress = HttpContextHelper.GetIpAddress(HttpContext),
//                 UserAgent = HttpContextHelper.GetUserAgent(HttpContext),
//                 RequestPath = HttpContext.Request.Path,
//                 RequestedFormat = "JSON"
//             };

//             var secured = await _dataExportService.WrapWithSecurityAttributesAsync(data, context, ct);
//             return secured.Data;
//         }

//         private long GetUserId()
//         {
//             var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
//             return long.TryParse(userIdClaim, out var userId) ? userId : 0;
//         }
//     }

//     #region Request DTOs

//     public class CreateKeyRequest
//     {
//         public string Purpose { get; set; } = string.Empty;
//         public int KeyLengthBits { get; set; } = 256;
//         public DateTime? ExpiresAt { get; set; }
//     }

//     public class RotateKeyRequest
//     {
//         public int KeyLengthBits { get; set; } = 256;
//         public int GracePeriodMinutes { get; set; } = 60;
//     }

//     public class DeactivateKeyRequest
//     {
//         public string Reason { get; set; } = string.Empty;
//     }

//     public class DestroyKeyRequest
//     {
//         public string Reason { get; set; } = string.Empty;
//     }

//     public class ValidateKeyRequest
//     {
//         public string KeyBase64 { get; set; } = string.Empty;
//         public int MinimumBits { get; set; } = 256;
//     }

//     #endregion
// }
