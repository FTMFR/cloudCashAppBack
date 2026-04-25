using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BnpCashClaudeApp.api.Attributes
{
    /// <summary>
    /// Security label levels for input ingestion controls (FDP_ITC.2.3).
    /// </summary>
    public enum InputSecurityLabel
    {
        Public = 0,
        Internal = 1,
        Confidential = 2,
        Secret = 3
    }

    /// <summary>
    /// Runtime options for input security-label enforcement.
    /// </summary>
    public class InputSecurityLabelOptions
    {
        public string HeaderName { get; set; } = "X-Data-Security-Label";
        public string DefaultAuthenticatedLabel { get; set; } = "Internal";
        public string DefaultAnonymousLabel { get; set; } = "Public";
        public bool EnforceExplicitLabelForAuthenticatedMutations { get; set; } = true;
        public bool EnforceExplicitLabelForAnonymousMutations { get; set; } = false;
    }

    /// <summary>
    /// Per-endpoint override for input security label policy.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class RequireInputSecurityLabelAttribute : Attribute
    {
        public RequireInputSecurityLabelAttribute(
            InputSecurityLabel minimumLabel = InputSecurityLabel.Internal,
            bool requireExplicitLabel = false)
        {
            MinimumLabel = minimumLabel;
            RequireExplicitLabel = requireExplicitLabel;
        }

        public InputSecurityLabel MinimumLabel { get; }
        public bool RequireExplicitLabel { get; }
    }

    /// <summary>
    /// Skip global input security-label check for exceptional endpoints.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class SkipInputSecurityLabelAttribute : Attribute
    {
    }

    /// <summary>
    /// Global ingestion guard for FDP_ITC.2.3.
    /// </summary>
    public class InputSecurityLabelFilter : IAsyncActionFilter
    {
        public const string InputSecurityLabelItemKey = "InputSecurityLabel";
        public const string InputSecurityLabelSourceItemKey = "InputSecurityLabelSource";

        private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethods.Post,
            HttpMethods.Put,
            HttpMethods.Patch,
            HttpMethods.Delete
        };

        private static readonly Dictionary<string, InputSecurityLabel> LabelMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["PUBLIC"] = InputSecurityLabel.Public,
            ["INTERNAL"] = InputSecurityLabel.Internal,
            ["CONFIDENTIAL"] = InputSecurityLabel.Confidential,
            ["SECRET"] = InputSecurityLabel.Secret
        };

        private readonly IOptionsMonitor<InputSecurityLabelOptions> _optionsMonitor;
        private readonly ILogger<InputSecurityLabelFilter> _logger;

        public InputSecurityLabelFilter(
            IOptionsMonitor<InputSecurityLabelOptions> optionsMonitor,
            ILogger<InputSecurityLabelFilter> logger)
        {
            _optionsMonitor = optionsMonitor;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var request = httpContext.Request;

            if (!MutatingMethods.Contains(request.Method))
            {
                await next();
                return;
            }

            if (HasSkipMetadata(context))
            {
                await next();
                return;
            }

            var options = _optionsMonitor.CurrentValue ?? new InputSecurityLabelOptions();
            var isAuthenticated = httpContext.User?.Identity?.IsAuthenticated ?? false;
            var isAllowAnonymousEndpoint = context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();

            var endpointPolicy = context.ActionDescriptor.EndpointMetadata
                .OfType<RequireInputSecurityLabelAttribute>()
                .FirstOrDefault();

            var minimumLabel = endpointPolicy?.MinimumLabel
                ?? ResolveDefaultLabel(options, isAuthenticated, isAllowAnonymousEndpoint);

            var requireExplicitLabel = endpointPolicy?.RequireExplicitLabel
                ?? ResolveExplicitRequirement(options, isAuthenticated, isAllowAnonymousEndpoint);

            var rawLabel = ResolveInputLabel(request, options.HeaderName);
            var source = "PolicyDefault";
            InputSecurityLabel effectiveLabel;

            if (string.IsNullOrWhiteSpace(rawLabel))
            {
                if (requireExplicitLabel)
                {
                    context.Result = BuildBadRequestResult(options.HeaderName);
                    return;
                }

                effectiveLabel = minimumLabel;
            }
            else if (!TryParseLabel(rawLabel, out effectiveLabel))
            {
                context.Result = BuildInvalidLabelResult(rawLabel);
                return;
            }
            else
            {
                source = "ClientProvided";
            }

            if (effectiveLabel < minimumLabel)
            {
                context.Result = BuildInsufficientLabelResult(effectiveLabel, minimumLabel);
                return;
            }

            httpContext.Items[InputSecurityLabelItemKey] = effectiveLabel.ToString();
            httpContext.Items[InputSecurityLabelSourceItemKey] = source;

            _logger.LogDebug(
                "[FDP_ITC.2.3] Input label accepted. Path: {Path}, Method: {Method}, Label: {Label}, Source: {Source}",
                request.Path,
                request.Method,
                effectiveLabel,
                source);

            await next();
        }

        private static bool HasSkipMetadata(ActionExecutingContext context)
        {
            return context.ActionDescriptor.EndpointMetadata.OfType<SkipInputSecurityLabelAttribute>().Any();
        }

        private static string? ResolveInputLabel(HttpRequest request, string headerName)
        {
            if (request.Headers.TryGetValue(headerName, out var headerValues))
            {
                var fromHeader = headerValues.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(fromHeader))
                    return fromHeader;
            }

            if (request.Query.TryGetValue("securityLabel", out var queryValues))
            {
                var fromQuery = queryValues.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(fromQuery))
                    return fromQuery;
            }

            return null;
        }

        private static bool TryParseLabel(string rawLabel, out InputSecurityLabel label)
        {
            var normalized = rawLabel.Trim().Replace("-", "").Replace("_", "");
            if (LabelMap.TryGetValue(normalized, out label))
            {
                return true;
            }

            label = InputSecurityLabel.Public;
            return false;
        }

        private static InputSecurityLabel ResolveDefaultLabel(
            InputSecurityLabelOptions options,
            bool isAuthenticated,
            bool isAllowAnonymousEndpoint)
        {
            var defaultLabel = (isAuthenticated || !isAllowAnonymousEndpoint)
                ? options.DefaultAuthenticatedLabel
                : options.DefaultAnonymousLabel;

            return TryParseLabel(defaultLabel, out var parsed)
                ? parsed
                : InputSecurityLabel.Internal;
        }

        private static bool ResolveExplicitRequirement(
            InputSecurityLabelOptions options,
            bool isAuthenticated,
            bool isAllowAnonymousEndpoint)
        {
            if (isAuthenticated || !isAllowAnonymousEndpoint)
                return options.EnforceExplicitLabelForAuthenticatedMutations;

            return options.EnforceExplicitLabelForAnonymousMutations;
        }

        private static IActionResult BuildBadRequestResult(string headerName)
        {
            return new BadRequestObjectResult(new
            {
                success = false,
                error = "InputSecurityLabelRequired",
                message = $"Input security label is required. Provide '{headerName}' header.",
                allowedLabels = new[] { "Public", "Internal", "Confidential", "Secret" },
                requirement = "FDP_ITC.2.3"
            });
        }

        private static IActionResult BuildInvalidLabelResult(string provided)
        {
            return new BadRequestObjectResult(new
            {
                success = false,
                error = "InvalidInputSecurityLabel",
                message = $"Unsupported input security label: '{provided}'.",
                allowedLabels = new[] { "Public", "Internal", "Confidential", "Secret" },
                requirement = "FDP_ITC.2.3"
            });
        }

        private IActionResult BuildInsufficientLabelResult(
            InputSecurityLabel provided,
            InputSecurityLabel minimumRequired)
        {
            _logger.LogWarning(
                "[FDP_ITC.2.3] Input label denied. Provided: {ProvidedLabel}, MinimumRequired: {MinimumRequired}",
                provided,
                minimumRequired);

            return new ObjectResult(new
            {
                success = false,
                error = "InsufficientInputSecurityLabel",
                message = $"Input security label '{provided}' is lower than minimum required '{minimumRequired}'.",
                requirement = "FDP_ITC.2.3"
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
