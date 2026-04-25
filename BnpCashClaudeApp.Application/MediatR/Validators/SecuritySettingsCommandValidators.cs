using BnpCashClaudeApp.Application.MediatR.Commands;
using FluentValidation;

namespace BnpCashClaudeApp.Application.MediatR.Validators
{
    /// <summary>
    /// Validation rules for security settings command inputs.
    /// </summary>
    public class UpdateAccountLockoutSettingsCommandValidator : AbstractValidator<UpdateAccountLockoutSettingsCommand>
    {
        public UpdateAccountLockoutSettingsCommandValidator()
        {
            RuleFor(x => x.MaxFailedAttempts)
                .InclusiveBetween(1, 20).WithMessage("حداکثر تلاش ناموفق باید بین 1 تا 20 باشد");

            RuleFor(x => x.LockoutDurationMinutes)
                .InclusiveBetween(1, 1440).WithMessage("مدت قفل حساب باید بین 1 تا 1440 دقیقه باشد");

            RuleFor(x => x.PermanentLockoutThreshold)
                .InclusiveBetween(1, 100).WithMessage("آستانه قفل دائمی باید بین 1 تا 100 باشد");

            RuleFor(x => x.FailedAttemptResetMinutes)
                .InclusiveBetween(1, 1440).WithMessage("زمان ریست تلاش ناموفق باید بین 1 تا 1440 دقیقه باشد");

            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("شناسه کاربر ویرایش‌کننده نامعتبر است");
        }
    }

    public class UpdatePasswordPolicySettingsCommandValidator : AbstractValidator<UpdatePasswordPolicySettingsCommand>
    {
        public UpdatePasswordPolicySettingsCommandValidator()
        {
            RuleFor(x => x.MinimumLength)
                .InclusiveBetween(6, 128).WithMessage("حداقل طول رمز عبور باید بین 6 تا 128 باشد");

            RuleFor(x => x.MaximumLength)
                .InclusiveBetween(8, 256).WithMessage("حداکثر طول رمز عبور باید بین 8 تا 256 باشد");

            RuleFor(x => x)
                .Must(x => x.MaximumLength >= x.MinimumLength)
                .WithMessage("حداکثر طول رمز عبور نمی‌تواند کمتر از حداقل طول باشد");

            RuleFor(x => x.PasswordHistoryCount)
                .InclusiveBetween(0, 24).WithMessage("تعداد تاریخچه رمز عبور باید بین 0 تا 24 باشد");

            RuleFor(x => x.PasswordExpirationDays)
                .InclusiveBetween(0, 3650).WithMessage("انقضای رمز عبور باید بین 0 تا 3650 روز باشد");

            RuleFor(x => x.SpecialCharacters)
                .NotEmpty().WithMessage("لیست کاراکترهای خاص نباید خالی باشد")
                .MaximumLength(100).WithMessage("لیست کاراکترهای خاص بیش از حد طولانی است")
                .When(x => x.RequireSpecialCharacter);

            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("شناسه کاربر ویرایش‌کننده نامعتبر است");
        }
    }
}
