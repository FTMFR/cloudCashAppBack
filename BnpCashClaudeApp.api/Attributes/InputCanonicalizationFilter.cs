using BnpCashClaudeApp.Application.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.api.Attributes
{
    /// <summary>
    /// Skip global input canonicalization for exceptional endpoints.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class SkipInputCanonicalizationAttribute : Attribute
    {
    }

    /// <summary>
    /// Canonicalize API-bound body payloads for mutating endpoints (FDP_ITC.2.2).
    /// </summary>
    public class InputCanonicalizationFilter : IAsyncActionFilter
    {
        private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethods.Post,
            HttpMethods.Put,
            HttpMethods.Patch,
            HttpMethods.Delete
        };

        private readonly ILogger<InputCanonicalizationFilter> _logger;

        public InputCanonicalizationFilter(ILogger<InputCanonicalizationFilter> logger)
        {
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;

            if (!MutatingMethods.Contains(request.Method) || HasSkipMetadata(context))
            {
                await next();
                return;
            }

            foreach (var actionArg in context.ActionArguments.ToArray())
            {
                if (actionArg.Value == null)
                    continue;

                if (!IsFromBodyArgument(context, actionArg.Key))
                    continue;

                CanonicalizeArgument(context, actionArg.Key, actionArg.Value);
            }

            _logger.LogDebug(
                "[FDP_ITC.2.2] Input canonicalization applied. Path: {Path}, Method: {Method}",
                request.Path,
                request.Method);

            await next();
        }

        private static bool HasSkipMetadata(ActionExecutingContext context)
        {
            return context.ActionDescriptor.EndpointMetadata.OfType<SkipInputCanonicalizationAttribute>().Any();
        }

        private static bool IsFromBodyArgument(ActionExecutingContext context, string argumentName)
        {
            var parameterDescriptor = context.ActionDescriptor.Parameters
                .OfType<ControllerParameterDescriptor>()
                .FirstOrDefault(p => string.Equals(p.Name, argumentName, StringComparison.Ordinal));

            if (parameterDescriptor == null)
                return false;

            var bindingSource = parameterDescriptor.BindingInfo?.BindingSource;
            if (bindingSource == null)
                return true;

            return bindingSource.CanAcceptDataFrom(BindingSource.Body);
        }

        private static void CanonicalizeArgument(
            ActionExecutingContext context,
            string argumentName,
            object argumentValue)
        {
            if (argumentValue is IFormFile ||
                argumentValue is IFormFileCollection ||
                argumentValue is Stream)
            {
                return;
            }

            if (argumentValue.GetType() == typeof(byte[]))
                return;

            if (argumentValue is string stringValue)
            {
                context.ActionArguments[argumentName] =
                    InputCanonicalizationHelper.CanonicalizeString(stringValue, argumentName);
                return;
            }

            InputCanonicalizationHelper.CanonicalizeObjectGraph(argumentValue, argumentName);
        }
    }
}
