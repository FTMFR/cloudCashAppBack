using BnpCashClaudeApp.Application.MediatR.Commands;
using FluentValidation;

namespace BnpCashClaudeApp.Application.MediatR.Validators
{
    /// <summary>
    /// اعتبارسنجی فرمان ایجاد تنظیمات شعبه
    /// </summary>
    public class CreateShobeSettingCommandValidator : AbstractValidator<CreateShobeSettingCommand>
    {
        public CreateShobeSettingCommandValidator()
        {
            RuleFor(x => x.SettingKey)
                .NotEmpty().WithMessage("کلید تنظیمات الزامی است")
                .MaximumLength(100).WithMessage("کلید تنظیمات نمی‌تواند بیشتر از 100 کاراکتر باشد");

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
