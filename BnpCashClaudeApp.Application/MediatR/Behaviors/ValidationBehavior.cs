using BnpCashClaudeApp.Application.Security;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Behaviors
{
    /// <summary>
    /// MediatR Pipeline Behavior برای اجرای خودکار FluentValidation
    /// ============================================
    /// این Behavior قبل از هر Handler اجرا می‌شود و
    /// اگر Validator ای برای Command/Query ثبت شده باشد،
    /// آن را اجرا کرده و در صورت خطا، Exception پرتاب می‌کند
    /// ============================================
    /// </summary>
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // FDP_ITC.2.2: canonicalize inbound payload before validation/handling.
            InputCanonicalizationHelper.CanonicalizeObjectGraph(request);

            // FDP_ITC.2.1 hardening: all command requests must have validators.
            if (IsCommandRequestWithoutValidator())
            {
                var missingValidatorFailures = new List<ValidationFailure>
                {
                    new ValidationFailure(
                        typeof(TRequest).Name,
                        $"Validator is required for command '{typeof(TRequest).Name}'.")
                };

                throw new ValidationException(missingValidatorFailures);
            }

            // اگر هیچ Validator ای ثبت نشده، مستقیم به Handler برو
            if (!_validators.Any())
            {
                return await next();
            }

            // اجرای تمام Validator ها
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            // جمع‌آوری خطاها
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            // اگر خطایی وجود داشت، Exception پرتاب کن
            if (failures.Any())
            {
                throw new ValidationException(failures);
            }

            return await next();
        }

        private bool IsCommandRequestWithoutValidator()
        {
            return typeof(TRequest).Name.EndsWith("Command", StringComparison.Ordinal) && !_validators.Any();
        }
    }
}
