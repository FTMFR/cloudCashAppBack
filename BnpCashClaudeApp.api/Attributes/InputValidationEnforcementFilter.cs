using BnpCashClaudeApp.Application.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.api.Attributes
{
    /// <summary>
    /// Skip strict input-validator enforcement for exceptional endpoints.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class SkipInputValidationEnforcementAttribute : Attribute
    {
    }

    /// <summary>
    /// Enforces validator presence and execution for API-bound inputs (FDP_ITC.2.1).
    /// </summary>
    public class InputValidationEnforcementFilter : IAsyncActionFilter
    {
        private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethods.Post,
            HttpMethods.Put,
            HttpMethods.Patch,
            HttpMethods.Delete
        };

        private static readonly HashSet<Type> StrictExternalBodyModels = new()
        {
            typeof(DataExportSettings),
            typeof(ContextAccessControlSettings)
        };

        private const int MaxQueryStringLength = 1024;
        private const int MaxRouteStringLength = 512;
        private const int MaxHeaderStringLength = 2048;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InputValidationEnforcementFilter> _logger;

        public InputValidationEnforcementFilter(
            IServiceProvider serviceProvider,
            ILogger<InputValidationEnforcementFilter> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;

            if (HasSkipMetadata(context))
            {
                await next();
                return;
            }

            var missingValidators = new HashSet<string>(StringComparer.Ordinal);
            var validationFailures = new List<ValidationFailure>();

            foreach (var actionArg in context.ActionArguments.ToArray())
            {
                if (actionArg.Value == null)
                    continue;

                var bindingLocation = ResolveBindingLocation(context, actionArg.Key, actionArg.Value);

                if (bindingLocation == InputBindingLocation.Unknown ||
                    bindingLocation == InputBindingLocation.Form)
                {
                    continue;
                }

                var model = actionArg.Value;
                var modelType = model.GetType();

                if (bindingLocation == InputBindingLocation.Body)
                {
                    if (!MutatingMethods.Contains(request.Method) || ShouldIgnoreArgument(model))
                        continue;

                    var bodyValidator = ResolveValidator(modelType);
                    if (bodyValidator == null)
                    {
                        if (RequiresStrictValidator(modelType))
                        {
                            missingValidators.Add($"{modelType.FullName ?? modelType.Name} [Body]");
                        }

                        continue;
                    }

                    var bodyValidationResult = await ValidateWithValidatorAsync(
                        bodyValidator,
                        model,
                        context.HttpContext.RequestAborted);

                    if (!bodyValidationResult.IsValid)
                        validationFailures.AddRange(bodyValidationResult.Errors.Where(f => f != null));

                    continue;
                }

                if (IsStringLikeInput(model, out var stringValue))
                {
                    var stringFailure = ValidateSimpleStringInput(
                        actionArg.Key,
                        stringValue,
                        bindingLocation);

                    if (stringFailure != null)
                        validationFailures.Add(stringFailure);

                    continue;
                }

                if (ShouldIgnoreArgument(model))
                    continue;

                var validator = ResolveValidator(modelType);
                if (validator == null)
                {
                    if (RequiresStrictValidator(modelType))
                    {
                        missingValidators.Add($"{modelType.FullName ?? modelType.Name} [{bindingLocation}]");
                    }

                    continue;
                }

                var validationResult = await ValidateWithValidatorAsync(
                    validator,
                    model,
                    context.HttpContext.RequestAborted);

                if (!validationResult.IsValid)
                    validationFailures.AddRange(validationResult.Errors.Where(f => f != null));
            }

            if (missingValidators.Count > 0)
            {
                _logger.LogError(
                    "[FDP_ITC.2.1] Missing validator for strict API-bound models. Path: {Path}, Method: {Method}, Models: {Models}",
                    request.Path,
                    request.Method,
                    string.Join(", ", missingValidators));

                context.Result = new ObjectResult(new
                {
                    success = false,
                    error = "InputValidatorMissing",
                    message = "Server input validation policy is not fully configured for one or more request models.",
                    requirement = "FDP_ITC.2.1",
                    models = missingValidators.OrderBy(x => x).ToArray()
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };

                return;
            }

            if (validationFailures.Count > 0)
            {
                _logger.LogWarning(
                    "[FDP_ITC.2.1] Request validation failed. Path: {Path}, Method: {Method}, Errors: {Count}",
                    request.Path,
                    request.Method,
                    validationFailures.Count);

                context.Result = new BadRequestObjectResult(new
                {
                    success = false,
                    error = "InputValidationFailed",
                    message = "One or more input validation rules were violated.",
                    requirement = "FDP_ITC.2.1",
                    validationErrors = validationFailures
                        .Select(f => new
                        {
                            field = string.IsNullOrWhiteSpace(f.PropertyName) ? "request" : f.PropertyName,
                            message = f.ErrorMessage
                        })
                        .ToArray()
                });

                return;
            }

            await next();
        }

        private static bool HasSkipMetadata(ActionExecutingContext context)
        {
            return context.ActionDescriptor.EndpointMetadata.OfType<SkipInputValidationEnforcementAttribute>().Any();
        }

        private static InputBindingLocation ResolveBindingLocation(
            ActionExecutingContext context,
            string argumentName,
            object value)
        {
            var request = context.HttpContext.Request;
            var parameterDescriptor = context.ActionDescriptor.Parameters
                .OfType<ControllerParameterDescriptor>()
                .FirstOrDefault(p => string.Equals(p.Name, argumentName, StringComparison.Ordinal));

            if (parameterDescriptor == null)
                return InputBindingLocation.Unknown;

            var bindingSource = parameterDescriptor.BindingInfo?.BindingSource;
            if (bindingSource != null)
            {
                if (bindingSource.CanAcceptDataFrom(BindingSource.Body))
                    return InputBindingLocation.Body;

                if (bindingSource.CanAcceptDataFrom(BindingSource.Query))
                    return InputBindingLocation.Query;

                if (bindingSource.CanAcceptDataFrom(BindingSource.Path))
                    return InputBindingLocation.Path;

                if (bindingSource.CanAcceptDataFrom(BindingSource.Header))
                    return InputBindingLocation.Header;

                if (bindingSource.CanAcceptDataFrom(BindingSource.Form))
                    return InputBindingLocation.Form;
            }

            if (context.RouteData.Values.ContainsKey(argumentName))
                return InputBindingLocation.Path;

            var parameterType = parameterDescriptor.ParameterType;
            if (IsFrameworkArgumentType(parameterType))
                return InputBindingLocation.Unknown;

            if (request.Query.ContainsKey(argumentName))
                return InputBindingLocation.Query;

            if (request.Headers.ContainsKey(argumentName))
                return InputBindingLocation.Header;

            if (!IsSimpleType(parameterType) && parameterType != typeof(string))
                return MutatingMethods.Contains(request.Method)
                    ? InputBindingLocation.Body
                    : InputBindingLocation.Query;

            return MutatingMethods.Contains(request.Method)
                ? InputBindingLocation.Body
                : InputBindingLocation.Query;
        }

        private static bool ShouldIgnoreArgument(object value)
        {
            var type = value.GetType();

            if (value is string)
                return true;

            if (value is IFormFile || value is IFormFileCollection || value is Stream)
                return true;

            if (type == typeof(byte[]))
                return true;

            if (value is CancellationToken)
                return true;

            return IsSimpleType(type);
        }

        private static bool IsSimpleType(Type type)
        {
            var actualType = Nullable.GetUnderlyingType(type) ?? type;

            if (actualType == typeof(string))
                return true;

            if (actualType == typeof(CancellationToken))
                return true;

            return actualType.IsPrimitive ||
                   actualType.IsEnum ||
                   actualType == typeof(decimal) ||
                   actualType == typeof(DateTime) ||
                   actualType == typeof(DateTimeOffset) ||
                   actualType == typeof(TimeSpan) ||
                   actualType == typeof(Guid);
        }

        private static bool IsStringLikeInput(object value, out string? stringValue)
        {
            switch (value)
            {
                case string single:
                    stringValue = single;
                    return true;
                case IEnumerable<string> list:
                    stringValue = string.Join(",", list);
                    return true;
                default:
                    stringValue = null;
                    return false;
            }
        }

        private static bool IsFrameworkArgumentType(Type type)
        {
            var actualType = Nullable.GetUnderlyingType(type) ?? type;
            if (actualType == typeof(CancellationToken))
                return true;

            return false;
        }

        private ValidationFailure? ValidateSimpleStringInput(
            string argumentName,
            string? value,
            InputBindingLocation bindingLocation)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var maxAllowedLength = bindingLocation switch
            {
                InputBindingLocation.Path => MaxRouteStringLength,
                InputBindingLocation.Header => MaxHeaderStringLength,
                _ => MaxQueryStringLength
            };

            if (value.Length > maxAllowedLength)
            {
                return new ValidationFailure(argumentName,
                    $"Input length for '{argumentName}' exceeds allowed maximum ({maxAllowedLength}) for {bindingLocation}.");
            }

            var hasUnsafeControlChars = value.Any(ch => char.IsControl(ch) && ch != '\r' && ch != '\n' && ch != '\t');
            if (hasUnsafeControlChars)
            {
                return new ValidationFailure(argumentName,
                    $"Input contains invalid control characters for '{argumentName}'.");
            }

            return null;
        }

        private static Task<ValidationResult> ValidateWithValidatorAsync(
            IValidator validator,
            object model,
            CancellationToken cancellationToken)
        {
            var validationContext = new ValidationContext<object>(model);
            return validator.ValidateAsync(validationContext, cancellationToken);
        }

        private bool RequiresStrictValidator(Type modelType)
        {
            if (modelType.Assembly == typeof(Program).Assembly)
                return true;

            return StrictExternalBodyModels.Contains(modelType);
        }

        private IValidator? ResolveValidator(Type modelType)
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(modelType);
            return _serviceProvider.GetService(validatorType) as IValidator;
        }

        private enum InputBindingLocation
        {
            Unknown = 0,
            Body = 1,
            Query = 2,
            Path = 3,
            Header = 4,
            Form = 5
        }
    }
}
