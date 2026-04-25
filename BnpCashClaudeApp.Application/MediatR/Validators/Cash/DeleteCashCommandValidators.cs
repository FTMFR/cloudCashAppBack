using BnpCashClaudeApp.Application.MediatR.Commands.Cash;
using FluentValidation;

namespace BnpCashClaudeApp.Application.MediatR.Validators.Cash
{
    /// <summary>
    /// Validation rules for delete commands in cash domain.
    /// </summary>
    public class DeleteAzaNoeCommandValidator : AbstractValidator<DeleteAzaNoeCommand>
    {
        public DeleteAzaNoeCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("شناسه حوزه الزامی است");

            RuleFor(x => x.TblUserGrpIdLastEdit)
                .GreaterThan(0).WithMessage("شناسه کاربر حذف‌کننده نامعتبر است");
        }
    }

    public class DeleteSarfaslCommandValidator : AbstractValidator<DeleteSarfaslCommand>
    {
        public DeleteSarfaslCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("شناسه سرفصل الزامی است");

            RuleFor(x => x.TblUserGrpIdLastEdit)
                .GreaterThan(0).WithMessage("شناسه کاربر حذف‌کننده نامعتبر است");
        }
    }

    public class DeleteTafsiliTypeCommandValidator : AbstractValidator<DeleteTafsiliTypeCommand>
    {
        public DeleteTafsiliTypeCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("شناسه نوع مشتری الزامی است");

            RuleFor(x => x.TblUserGrpIdLastEdit)
                .GreaterThan(0).WithMessage("شناسه کاربر حذف‌کننده نامعتبر است");
        }
    }
}
