using BnpCashClaudeApp.Application.MediatR.Commands.Cash;
using FluentValidation;

namespace BnpCashClaudeApp.Application.MediatR.Validators.Cash
{
    /// <summary>
    /// اعتبارسنجی فرمان ایجاد نوع تفصیلی
    /// </summary>
    public class CreateTafsiliTypeCommandValidator : AbstractValidator<CreateTafsiliTypeCommand>
    {
        public CreateTafsiliTypeCommandValidator()
        {
            RuleFor(x => x.ShobePublicId)
                .NotEmpty().WithMessage("شناسه شعبه الزامی است");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("عنوان نوع تفصیلی الزامی است")
                .MaximumLength(200).WithMessage("عنوان نوع تفصیلی نمی‌تواند بیشتر از 200 کاراکتر باشد");
        }
    }
}
