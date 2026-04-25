using BnpCashClaudeApp.Application.MediatR.Commands.Cash;
using FluentValidation;

namespace BnpCashClaudeApp.Application.MediatR.Validators.Cash
{
    /// <summary>
    /// اعتبارسنجی فرمان ویرایش سرفصل
    /// </summary>
    public class UpdateSarfaslCommandValidator : AbstractValidator<UpdateSarfaslCommand>
    {
        public UpdateSarfaslCommandValidator()
        {
            RuleFor(x => x.PublicId)
                .NotEmpty().WithMessage("شناسه سرفصل الزامی است");

            RuleFor(x => x.TblShobeId)
                .GreaterThan(0).WithMessage("شناسه شعبه باید بزرگتر از صفر باشد");

            RuleFor(x => x.CodeSarfasl)
                .NotEmpty().WithMessage("کد سرفصل الزامی است")
                .MaximumLength(50).WithMessage("کد سرفصل نمی‌تواند بیشتر از 50 کاراکتر باشد");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("عنوان سرفصل الزامی است")
                .MaximumLength(200).WithMessage("عنوان سرفصل نمی‌تواند بیشتر از 200 کاراکتر باشد");

            RuleFor(x => x.MizanEtebarBedehkar)
                .GreaterThanOrEqualTo(0).WithMessage("میزان اعتبار بدهکار نمی‌تواند منفی باشد");

            RuleFor(x => x.MizanEtebarBestankar)
                .GreaterThanOrEqualTo(0).WithMessage("میزان اعتبار بستانکار نمی‌تواند منفی باشد");

            // FK ها: اگر مقدار داده شده، باید بزرگتر از 0 باشد
            RuleFor(x => x.TblComboIdVazeiatZirGrp)
                .GreaterThan(0).WithMessage("شناسه وضعیت زیرگروه باید بزرگتر از صفر باشد")
                .When(x => x.TblComboIdVazeiatZirGrp.HasValue);

            RuleFor(x => x.TblComboIdControlAmaliat)
                .GreaterThan(0).WithMessage("شناسه کنترل عملیات باید بزرگتر از صفر باشد")
                .When(x => x.TblComboIdControlAmaliat.HasValue);

            RuleFor(x => x.TedadArghamZirGrp)
                .GreaterThan(0).WithMessage("تعداد ارقام زیرگروه باید بزرگتر از صفر باشد")
                .When(x => x.TedadArghamZirGrp.HasValue);
        }
    }
}
