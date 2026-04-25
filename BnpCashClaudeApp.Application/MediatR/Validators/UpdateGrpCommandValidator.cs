using BnpCashClaudeApp.Application.MediatR.Commands;
using FluentValidation;

namespace BnpCashClaudeApp.Application.MediatR.Validators
{
    /// <summary>
    /// اعتبارسنجی فرمان ویرایش گروه
    /// </summary>
    public class UpdateGrpCommandValidator : AbstractValidator<UpdateGrpCommand>
    {
        public UpdateGrpCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("شناسه گروه الزامی است");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("عنوان گروه الزامی است")
                .MaximumLength(200).WithMessage("عنوان گروه نمی‌تواند بیشتر از 200 کاراکتر باشد");
        }
    }
}
