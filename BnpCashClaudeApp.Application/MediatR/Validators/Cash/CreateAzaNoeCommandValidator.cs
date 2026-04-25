using BnpCashClaudeApp.Application.MediatR.Commands.Cash;
using FluentValidation;

namespace BnpCashClaudeApp.Application.MediatR.Validators.Cash
{
    /// <summary>
    /// اعتبارسنجی فرمان ایجاد حوزه (نوع عضو)
    /// </summary>
    public class CreateAzaNoeCommandValidator : AbstractValidator<CreateAzaNoeCommand>
    {
        public CreateAzaNoeCommandValidator()
        {
            RuleFor(x => x.ShobePublicId)
                .NotEmpty().WithMessage("شناسه شعبه الزامی است");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("عنوان حوزه الزامی است")
                .MaximumLength(200).WithMessage("عنوان حوزه نمی‌تواند بیشتر از 200 کاراکتر باشد");

            RuleFor(x => x.CodeHoze)
                .GreaterThan(0).WithMessage("کد حوزه باید بزرگتر از صفر باشد");

            RuleFor(x => x.TafsiliTypePublicId)
                .NotEmpty().WithMessage("شناسه نوع تفصیلی الزامی است");
        }
    }
}
