using BnpCashClaudeApp.Application.MediatR.Commands;
using FluentValidation;

namespace BnpCashClaudeApp.Application.MediatR.Validators
{
    /// <summary>
    /// اعتبارسنجی فرمان ویرایش تنظیمات شعبه
    /// </summary>
    public class UpdateShobeSettingCommandValidator : AbstractValidator<UpdateShobeSettingCommand>
    {
        public UpdateShobeSettingCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("شناسه تنظیمات الزامی است");

            RuleFor(x => x.SettingName)
                .NotEmpty().WithMessage("نام تنظیمات الزامی است")
                .MaximumLength(200).WithMessage("نام تنظیمات نمی‌تواند بیشتر از 200 کاراکتر باشد");

            RuleFor(x => x.SettingValue)
                .NotEmpty().WithMessage("مقدار تنظیمات الزامی است");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).WithMessage("ترتیب نمایش نمی‌تواند منفی باشد");
        }
    }
}
