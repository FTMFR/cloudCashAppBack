using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.api.Helpers;
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
    /// Attribute برای بررسی Permission کاربر
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF از استاندارد ISO 15408
    /// پیاده‌سازی الزام FPT_FLS.1.1 (الزام 46) - Fail-Secure
    /// بررسی دسترسی کاربر به عملیات خاص
    /// ============================================
    /// استفاده:
    /// [RequirePermission("Users.Create")]
    /// [RequirePermission("Users", "Create")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequirePermissionAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// سازنده با نام کامل Permission
        /// </summary>
        /// <param name="permission">نام کامل Permission (مثال: Users.Create)</param>
        public RequirePermissionAttribute(string permission) 
            : base(typeof(RequirePermissionFilter))
        {
            // فقط permission را پاس می‌دهیم، alternativePermission به صورت optional در constructor است
            Arguments = new object[] { permission };
        }

        /// <summary>
        /// سازنده با Resource و Action جداگانه
        /// </summary>
        /// <param name="resource">نام Resource (مثال: Users)</param>
        /// <param name="action">نام Action (مثال: Create)</param>
        public RequirePermissionAttribute(string resource, string action) 
            : base(typeof(RequirePermissionFilter))
        {
            Arguments = new object[] { $"{resource}.{action}" };
        }
    }

    /// <summary>
    /// Filter برای بررسی Permission
    /// ============================================
    /// پیاده‌سازی الزام FPT_FLS.1.1 (الزام 46) - Fail-Secure
    /// پیاده‌سازی الزام FDP_ACF.1.4 (الزام 19) - Context-based Access Control
    /// در صورت خطا در بررسی دسترسی، دسترسی رد می‌شود
    /// ============================================
    /// </summary>
    public class RequirePermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string _permission;
        private readonly string? _alternativePermission;
        private readonly IPermissionService _permissionService;
        private readonly IResourceAuthorizationPolicyService _resourceAuthorizationPolicyService;
        private readonly IContextAccessControlService _contextAccessControlService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<RequirePermissionFilter> _logger;

        public RequirePermissionFilter(
            string permission, 
            IPermissionService permissionService,
            IResourceAuthorizationPolicyService resourceAuthorizationPolicyService,
            IContextAccessControlService contextAccessControlService,
            IAuditLogService auditLogService,
            ILogger<RequirePermissionFilter> logger,
            string? alternativePermission = null) // optional parameter در انتها
        {
            _permission = permission;
            _alternativePermission = alternativePermission;
            _permissionService = permissionService;
            _resourceAuthorizationPolicyService = resourceAuthorizationPolicyService;
            _contextAccessControlService = contextAccessControlService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            
            // بررسی احراز هویت
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "احراز هویت الزامی است",
                    error = "Unauthorized"
                });

                // ثبت در Audit Log
                await LogUnauthorizedAccess(context, null, "Not authenticated");
                return;
            }

            // دریافت UserId از Claims
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "شناسه کاربر نامعتبر است",
                    error = "Invalid user identifier"
                });

                await LogUnauthorizedAccess(context, null, "Invalid user ID");
                return;
            }

            var primaryPolicyResult = _resourceAuthorizationPolicyService.ValidatePermission(_permission);
            if (!primaryPolicyResult.IsAllowed)
            {
                _logger.LogWarning(
                    "Authorization policy denied permission {Permission} for user {UserId}. Reason: {Reason}",
                    _permission, userId, primaryPolicyResult.DenialReason);

                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = "سیاست دسترسی اجازه این عملیات را نمی‌دهد",
                    error = "Authorization policy denied",
                    requiredPermission = _permission,
                    reason = primaryPolicyResult.DenialReason
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };

                await LogAuthorizationPolicyDenied(context, userId, _permission, primaryPolicyResult.DenialReason);
                return;
            }

            var canUseAlternativePermission = false;
            if (!string.IsNullOrWhiteSpace(_alternativePermission))
            {
                var alternativePolicyResult = _resourceAuthorizationPolicyService.ValidatePermission(_alternativePermission);
                canUseAlternativePermission = alternativePolicyResult.IsAllowed;

                if (!alternativePolicyResult.IsAllowed)
                {
                    _logger.LogWarning(
                        "Alternative permission policy denied permission {Permission} for user {UserId}. Reason: {Reason}",
                        _alternativePermission, userId, alternativePolicyResult.DenialReason);
                }
            }

            // ============================================
            // FDP_ACF.1.4 (الزام 19): Context-based Access Control
            // بررسی دسترسی بر اساس IP، زمان، دستگاه و ...
            // ============================================
            try
            {
                var accessContext = new AccessContext
                {
                    IpAddress = HttpContextHelper.GetIpAddress(context.HttpContext),
                    UserAgent = context.HttpContext.Request.Headers["User-Agent"].ToString(),
                    RequestPath = context.HttpContext.Request.Path.ToString(),
                    HttpMethod = context.HttpContext.Request.Method,
                    RequiredPermission = _permission,
                    RequestTime = DateTime.UtcNow
                };

                var contextResult = await _contextAccessControlService.ValidateAccessAsync(accessContext, userId);

                if (!contextResult.IsAllowed)
                {
                    _logger.LogWarning(
                        "Context-based access denied for user {UserId}, permission {Permission}. Reason: {Reason}",
                        userId, _permission, contextResult.DenialReason);

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
                    return;
                }

                // اگر نیاز به MFA بود (ریسک بالا)
                if (contextResult.RequiresMfa)
                {
                    _logger.LogInformation(
                        "MFA required for user {UserId} due to high risk score: {RiskScore}",
                        userId, contextResult.RiskScore);
                    // اینجا می‌توان منطق MFA را اضافه کرد
                }
            }
            catch (Exception ex)
            {
                // ============================================
                // FPT_FLS.1.1: Fail-Secure - رد دسترسی در خطا
                // ============================================
                _logger.LogError(ex,
                    "FAIL-SECURE: Context access check failed for user {UserId}. ACCESS DENIED.",
                    userId);

                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = "خطا در بررسی دسترسی Context",
                    error = "Context access check failed"
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            // ============================================
            // FPT_FLS.1.1 (الزام 46): Fail-Secure در بررسی دسترسی
            // در صورت خطا، دسترسی رد می‌شود (Deny by Default)
            // ============================================
            bool hasPermission;
            try
            {
                // بررسی Permission اصلی
                hasPermission = await _permissionService.HasPermissionAsync(userId, _permission);
                
                // اگر Permission جایگزین داریم و دسترسی اصلی ندارد، جایگزین را چک کن
                if (!hasPermission && canUseAlternativePermission && !string.IsNullOrEmpty(_alternativePermission))
                {
                    hasPermission = await _permissionService.HasPermissionAsync(userId, _alternativePermission);
                }
            }
            catch (Exception ex)
            {
                // ============================================
                // FPT_FLS.1.1: Fail-Secure - رد دسترسی در خطا
                // ============================================
                _logger.LogError(ex,
                    "FAIL-SECURE: Permission check failed for user {UserId}, permission {Permission}. ACCESS DENIED.",
                    userId, _permission);

                // ثبت در Audit Log
                await LogFailSecureEvent(context, userId, ex);

                // Fail-Secure: رد دسترسی
                hasPermission = false;
            }

            if (!hasPermission)
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = "شما دسترسی لازم برای این عملیات را ندارید",
                    error = "Access denied",
                    requiredPermission = _permission
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };

                // ثبت در Audit Log
                await LogAccessDenied(context, userId, user.Identity?.Name);
            }
        }

        private async Task LogAuthorizationPolicyDenied(
            AuthorizationFilterContext context,
            int userId,
            string permission,
            string? reason)
        {
            try
            {
                var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
                var path = context.HttpContext.Request.Path;
                var method = context.HttpContext.Request.Method;

                await _auditLogService.LogEventAsync(
                    eventType: "AuthorizationPolicyDenied",
                    entityType: "PermissionPolicy",
                    entityId: permission,
                    isSuccess: false,
                    errorMessage: reason,
                    ipAddress: ipAddress,
                    userId: userId,
                    description: $"Authorization policy denied {method} {path}. Permission: {permission}. Reason: {reason}",
                    ct: default);
            }
            catch
            {
                // Best effort only.
            }
        }

        private async Task LogUnauthorizedAccess(AuthorizationFilterContext context, int? userId, string reason)
        {
            try
            {
                var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
                var path = context.HttpContext.Request.Path;
                var method = context.HttpContext.Request.Method;

                await _auditLogService.LogEventAsync(
                    eventType: "UnauthorizedAccess",
                    entityType: "Permission",
                    entityId: _permission,
                    isSuccess: false,
                    errorMessage: reason,
                    ipAddress: ipAddress,
                    userId: userId,
                    description: $"Unauthorized access attempt to {method} {path} - Permission required: {_permission}",
                    ct: default);
            }
            catch
            {
                // ثبت Audit Log نباید مانع از ادامه عملیات شود
            }
        }

        private async Task LogAccessDenied(AuthorizationFilterContext context, int userId, string? userName)
        {
            try
            {
                var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
                var path = context.HttpContext.Request.Path;
                var method = context.HttpContext.Request.Method;

                await _auditLogService.LogEventAsync(
                    eventType: "AccessDenied",
                    entityType: "Permission",
                    entityId: _permission,
                    isSuccess: false,
                    errorMessage: "Permission denied",
                    ipAddress: ipAddress,
                    userName: userName,
                    userId: userId,
                    description: $"Access denied to {method} {path} - Permission required: {_permission}",
                    ct: default);
            }
            catch
            {
                // ثبت Audit Log نباید مانع از ادامه عملیات شود
            }
        }

        /// <summary>
        /// ثبت رویداد Fail-Secure در Audit Log
        /// </summary>
        private async Task LogFailSecureEvent(AuthorizationFilterContext context, int userId, Exception ex)
        {
            try
            {
                var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
                var path = context.HttpContext.Request.Path;
                var method = context.HttpContext.Request.Method;

                await _auditLogService.LogEventAsync(
                    eventType: "FailSecureActivated",
                    entityType: "Permission",
                    entityId: _permission,
                    isSuccess: false,
                    errorMessage: $"FAIL-SECURE: Permission check failed - {ex.GetType().Name}",
                    ipAddress: ipAddress,
                    userId: userId,
                    description: $"FAIL-SECURE: Access denied to {method} {path} due to system failure. Permission: {_permission}",
                    ct: default);
            }
            catch
            {
                // ثبت Audit Log نباید مانع از Fail-Secure شود
            }
        }
    }

    /// <summary>
    /// Attribute برای بررسی چندین Permission (AND logic)
    /// کاربر باید به همه Permission ها دسترسی داشته باشد
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RequireAllPermissionsAttribute : TypeFilterAttribute
    {
        public RequireAllPermissionsAttribute(params string[] permissions) 
            : base(typeof(RequireAllPermissionsFilter))
        {
            Arguments = new object[] { permissions };
        }
    }

    /// <summary>
    /// Filter برای بررسی چندین Permission (AND logic)
    /// ============================================
    /// پیاده‌سازی الزام FPT_FLS.1.1 (الزام 46) - Fail-Secure
    /// ============================================
    /// </summary>
    public class RequireAllPermissionsFilter : IAsyncAuthorizationFilter
    {
        private readonly string[] _permissions;
        private readonly IPermissionService _permissionService;
        private readonly IContextAccessControlService _contextAccessControlService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<RequireAllPermissionsFilter> _logger;

        public RequireAllPermissionsFilter(
            string[] permissions,
            IPermissionService permissionService,
            IContextAccessControlService contextAccessControlService,
            IAuditLogService auditLogService,
            ILogger<RequireAllPermissionsFilter> logger)
        {
            _permissions = permissions;
            _permissionService = permissionService;
            _contextAccessControlService = contextAccessControlService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "احراز هویت الزامی است",
                    error = "Unauthorized"
                });
                return;
            }

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "شناسه کاربر نامعتبر است",
                    error = "Invalid user identifier"
                });
                return;
            }

            // ============================================
            // FDP_ACF.1.3: consistent context-based enforcement
            // ============================================
            try
            {
                var accessContext = new AccessContext
                {
                    IpAddress = HttpContextHelper.GetIpAddress(context.HttpContext),
                    UserAgent = context.HttpContext.Request.Headers["User-Agent"].ToString(),
                    RequestPath = context.HttpContext.Request.Path.ToString(),
                    HttpMethod = context.HttpContext.Request.Method,
                    RequiredPermission = string.Join(",", _permissions),
                    RequestTime = DateTime.UtcNow
                };

                var contextResult = await _contextAccessControlService.ValidateAccessAsync(accessContext, userId);
                if (!contextResult.IsAllowed)
                {
                    _logger.LogWarning(
                        "Context-based access denied for user {UserId}, permissions {Permissions}. Reason: {Reason}",
                        userId, string.Join(", ", _permissions), contextResult.DenialReason);

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
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "FAIL-SECURE: Context access check failed for user {UserId}. ACCESS DENIED.",
                    userId);

                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = "خطا در بررسی دسترسی Context",
                    error = "Context access check failed"
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            // ============================================
            // FPT_FLS.1.1 (الزام 46): Fail-Secure
            // ============================================
            bool hasAllPermissions;
            try
            {
                hasAllPermissions = await _permissionService.HasAllPermissionsAsync(userId, _permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "FAIL-SECURE: HasAllPermissions check failed for user {UserId}. ACCESS DENIED.",
                    userId);

                try
                {
                    var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _auditLogService.LogEventAsync(
                        eventType: "FailSecureActivated",
                        entityType: "Permission",
                        entityId: string.Join(", ", _permissions),
                        isSuccess: false,
                        errorMessage: $"FAIL-SECURE: HasAllPermissions check failed - {ex.GetType().Name}",
                        ipAddress: ipAddress,
                        userId: userId,
                        description: $"FAIL-SECURE: Access denied due to system failure",
                        ct: default);
                }
                catch { }

                hasAllPermissions = false;
            }

            if (!hasAllPermissions)
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = "شما دسترسی‌های لازم برای این عملیات را ندارید",
                    error = "Access denied",
                    requiredPermissions = _permissions
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };

                // ثبت در Audit Log
                try
                {
                    var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _auditLogService.LogEventAsync(
                        eventType: "AccessDenied",
                        entityType: "Permission",
                        entityId: string.Join(", ", _permissions),
                        isSuccess: false,
                        errorMessage: "Multiple permissions required",
                        ipAddress: ipAddress,
                        userName: user.Identity?.Name,
                        userId: userId,
                        description: $"Access denied - Required all permissions: {string.Join(", ", _permissions)}",
                        ct: default);
                }
                catch { }
            }
        }
    }

    /// <summary>
    /// Attribute برای بررسی چندین Permission (OR logic)
    /// کاربر باید به حداقل یکی از Permission ها دسترسی داشته باشد
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RequireAnyPermissionAttribute : TypeFilterAttribute
    {
        public RequireAnyPermissionAttribute(params string[] permissions) 
            : base(typeof(RequireAnyPermissionFilter))
        {
            Arguments = new object[] { permissions };
        }
    }

    /// <summary>
    /// Filter برای بررسی چندین Permission (OR logic)
    /// ============================================
    /// پیاده‌سازی الزام FPT_FLS.1.1 (الزام 46) - Fail-Secure
    /// ============================================
    /// </summary>
    public class RequireAnyPermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string[] _permissions;
        private readonly IPermissionService _permissionService;
        private readonly IContextAccessControlService _contextAccessControlService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<RequireAnyPermissionFilter> _logger;

        public RequireAnyPermissionFilter(
            string[] permissions,
            IPermissionService permissionService,
            IContextAccessControlService contextAccessControlService,
            IAuditLogService auditLogService,
            ILogger<RequireAnyPermissionFilter> logger)
        {
            _permissions = permissions;
            _permissionService = permissionService;
            _contextAccessControlService = contextAccessControlService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "احراز هویت الزامی است",
                    error = "Unauthorized"
                });
                return;
            }

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "شناسه کاربر نامعتبر است",
                    error = "Invalid user identifier"
                });
                return;
            }

            // ============================================
            // FDP_ACF.1.3: consistent context-based enforcement
            // ============================================
            try
            {
                var accessContext = new AccessContext
                {
                    IpAddress = HttpContextHelper.GetIpAddress(context.HttpContext),
                    UserAgent = context.HttpContext.Request.Headers["User-Agent"].ToString(),
                    RequestPath = context.HttpContext.Request.Path.ToString(),
                    HttpMethod = context.HttpContext.Request.Method,
                    RequiredPermission = string.Join(",", _permissions),
                    RequestTime = DateTime.UtcNow
                };

                var contextResult = await _contextAccessControlService.ValidateAccessAsync(accessContext, userId);
                if (!contextResult.IsAllowed)
                {
                    _logger.LogWarning(
                        "Context-based access denied for user {UserId}, permissions {Permissions}. Reason: {Reason}",
                        userId, string.Join(", ", _permissions), contextResult.DenialReason);

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
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "FAIL-SECURE: Context access check failed for user {UserId}. ACCESS DENIED.",
                    userId);

                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = "خطا در بررسی دسترسی Context",
                    error = "Context access check failed"
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            // ============================================
            // FPT_FLS.1.1 (الزام 46): Fail-Secure
            // ============================================
            bool hasAnyPermission;
            try
            {
                hasAnyPermission = await _permissionService.HasAnyPermissionAsync(userId, _permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "FAIL-SECURE: HasAnyPermission check failed for user {UserId}. ACCESS DENIED.",
                    userId);

                try
                {
                    var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _auditLogService.LogEventAsync(
                        eventType: "FailSecureActivated",
                        entityType: "Permission",
                        entityId: string.Join(", ", _permissions),
                        isSuccess: false,
                        errorMessage: $"FAIL-SECURE: HasAnyPermission check failed - {ex.GetType().Name}",
                        ipAddress: ipAddress,
                        userId: userId,
                        description: $"FAIL-SECURE: Access denied due to system failure",
                        ct: default);
                }
                catch { }

                hasAnyPermission = false;
            }

            if (!hasAnyPermission)
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = "شما دسترسی لازم برای این عملیات را ندارید",
                    error = "Access denied",
                    acceptablePermissions = _permissions
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };

                // ثبت در Audit Log
                try
                {
                    var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _auditLogService.LogEventAsync(
                        eventType: "AccessDenied",
                        entityType: "Permission",
                        entityId: string.Join(", ", _permissions),
                        isSuccess: false,
                        errorMessage: "At least one permission required",
                        ipAddress: ipAddress,
                        userName: user.Identity?.Name,
                        userId: userId,
                        description: $"Access denied - Required any of permissions: {string.Join(", ", _permissions)}",
                        ct: default);
                }
                catch { }
            }
        }
    }
}
