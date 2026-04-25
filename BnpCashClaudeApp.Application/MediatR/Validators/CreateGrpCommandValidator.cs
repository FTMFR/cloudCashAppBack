using BnpCashClaudeApp.Application.MediatR.Commands;
using FluentValidation;

namespace BnpCashClaudeApp.Application.MediatR.Validators
{
    /// <summary>
    /// اعتبارسنجی فرمان ایجاد گروه
    /// </summary>
    public class CreateGrpCommandValidator : AbstractValidator<CreateGrpCommand>
    {
        public CreateGrpCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("عنوان گروه الزامی است")
                .MaximumLength(200).WithMessage("عنوان گروه نمی‌تواند بیشتر از 200 کاراکتر باشد");
        }
    }
}
