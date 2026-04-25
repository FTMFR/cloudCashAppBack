using BnpCashClaudeApp.Application.MediatR.Commands;
using FluentValidation;

namespace BnpCashClaudeApp.Application.MediatR.Validators
{
    /// <summary>
    /// اعتبارسنجی فرمان ایجاد شعبه
    /// </summary>
    public class CreateShobeCommandValidator : AbstractValidator<CreateShobeCommand>
    {
        public CreateShobeCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("عنوان شعبه الزامی است")
                .MaximumLength(200).WithMessage("عنوان شعبه نمی‌تواند بیشتر از 200 کاراکتر باشد");

            RuleFor(x => x.ShobeCode)
                .GreaterThan(0).WithMessage("کد شعبه باید بزرگتر از صفر باشد");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).WithMessage("ترتیب نمایش نمی‌تواند منفی باشد");

            RuleFor(x => x.PostalCode)
                .Matches(@"^\d{10}$").WithMessage("کد پستی باید 10 رقم باشد")
                .When(x => !string.IsNullOrWhiteSpace(x.PostalCode));
        }
    }
}
