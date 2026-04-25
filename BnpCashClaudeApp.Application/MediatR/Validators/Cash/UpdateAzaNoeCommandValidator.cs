using BnpCashClaudeApp.Application.MediatR.Commands.Cash;
using FluentValidation;

namespace BnpCashClaudeApp.Application.MediatR.Validators.Cash
{
    /// <summary>
    /// اعتبارسنجی فرمان ویرایش حوزه (نوع عضو)
    /// </summary>
    public class UpdateAzaNoeCommandValidator : AbstractValidator<UpdateAzaNoeCommand>
    {
        public UpdateAzaNoeCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("شناسه حوزه الزامی است");

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
