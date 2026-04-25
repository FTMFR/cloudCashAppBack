using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.api.Attributes
{
    /// <summary>
    /// Enforces context-based access checks for authenticated endpoints that do not
    /// use explicit permission attributes. This closes coverage gaps for Authorize-only actions.
    /// </summary>
    public class ContextAccessAuthorizeOnlyFilter : IAsyncAuthorizationFilter
    {
        private readonly IContextAccessControlService _contextAccessControlService;
        private readonly ILogger<ContextAccessAuthorizeOnlyFilter> _logger;

        public ContextAccessAuthorizeOnlyFilter(
            IContextAccessControlService contextAccessControlService,
            ILogger<ContextAccessAuthorizeOnlyFilter> logger)
        {
            _contextAccessControlService = contextAccessControlService;
            _logger = logger;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var endpointMetadata = context.ActionDescriptor.EndpointMetadata;

            // Anonymous endpoints are out of scope.
            if (endpointMetadata.OfType<IAllowAnonymous>().Any())
            {
                return;
            }

            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            // Endpoints already protected by explicit permission attributes are validated there.
            if (endpointMetadata.OfType<RequirePermissionAttribute>().Any() ||
                endpointMetadata.OfType<RequireAllPermissionsAttribute>().Any() ||
                endpointMetadata.OfType<RequireAnyPermissionAttribute>().Any())
            {
                return;
            }

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out var userId))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "شناسه کاربر نامعتبر است",
                    error = "Invalid user identifier"
                });
                return;
            }

            try
            {
                var accessContext = new AccessContext
                {
                    IpAddress = HttpContextHelper.GetIpAddress(context.HttpContext),
                    UserAgent = context.HttpContext.Request.Headers["User-Agent"].ToString(),
                    RequestPath = context.HttpContext.Request.Path.ToString(),
                    HttpMethod = context.HttpContext.Request.Method,
                    RequiredPermission = "AuthorizeOnly",
                    RequestTime = DateTime.UtcNow
                };

                var contextResult = await _contextAccessControlService.ValidateAccessAsync(accessContext, userId);
                if (!contextResult.IsAllowed)
                {
                    _logger.LogWarning(
                        "Context-based access denied for authorize-only endpoint. User {UserId}, Path {Path}, Reason: {Reason}",
                        userId, accessContext.RequestPath, contextResult.DenialReason);

                    context.Result = new ObjectResult(new
                    {
                        success = false,
                        message = contextResult.DenialReason ?? "دسترسی بر اساس شرایط Context رد شد",
                        error = "Context access denied",
                        denialCode = contextResult.DenialCode?.ToString()
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                }
            }
            catch (Exception ex)
            {
                // Fail-secure: deny when context verification fails.
                _logger.LogError(ex,
                    "FAIL-SECURE: Context access check failed for authorize-only endpoint. ACCESS DENIED.");

                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = "خطا در بررسی دسترسی Context",
                    error = "Context access check failed"
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}
