using BnpCashClaudeApp.Application.MediatR.Commands;
using FluentValidation;

namespace BnpCashClaudeApp.Application.MediatR.Validators
{
    /// <summary>
    /// Validation rules for delete commands in navigation domain.
    /// </summary>
    public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
    {
        public DeleteUserCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("شناسه کاربر الزامی است");
        }
    }

    public class DeleteGrpCommandValidator : AbstractValidator<DeleteGrpCommand>
    {
        public DeleteGrpCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("شناسه گروه الزامی است");
        }
    }

    public class DeleteMenuCommandValidator : AbstractValidator<DeleteMenuCommand>
    {
        public DeleteMenuCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("شناسه منو الزامی است");
        }
    }

    public class DeleteShobeCommandValidator : AbstractValidator<DeleteShobeCommand>
    {
        public DeleteShobeCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("شناسه شعبه الزامی است");
        }
    }

    public class DeleteShobeSettingCommandValidator : AbstractValidator<DeleteShobeSettingCommand>
    {
        public DeleteShobeSettingCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("شناسه تنظیمات شعبه الزامی است");
        }
    }
}
