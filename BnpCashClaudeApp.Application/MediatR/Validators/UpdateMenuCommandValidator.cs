using BnpCashClaudeApp.Application.MediatR.Commands;
using FluentValidation;

namespace BnpCashClaudeApp.Application.MediatR.Validators
{
    /// <summary>
    /// اعتبارسنجی فرمان ویرایش منو
    /// </summary>
    public class UpdateMenuCommandValidator : AbstractValidator<UpdateMenuCommand>
    {
        public UpdateMenuCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("شناسه منو الزامی است");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("عنوان منو الزامی است")
                .MaximumLength(200).WithMessage("عنوان منو نمی‌تواند بیشتر از 200 کاراکتر باشد");

            RuleFor(x => x.Path)
                .NotEmpty().WithMessage("مسیر منو الزامی است");

            // FK: اگر مقدار داده شده، باید بزرگتر از 0 باشد
            RuleFor(x => x.tblSoftwareId)
                .GreaterThan(0).WithMessage("شناسه نرم‌افزار باید بزرگتر از صفر باشد")
                .When(x => x.tblSoftwareId.HasValue);
        }
    }
}
