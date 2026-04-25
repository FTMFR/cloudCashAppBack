using BnpCashClaudeApp.Application.MediatR.Commands;
using FluentValidation;

namespace BnpCashClaudeApp.Application.MediatR.Validators
{
    /// <summary>
    /// اعتبارسنجی فرمان ایجاد کاربر
    /// </summary>
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("نام کاربری الزامی است")
                .MaximumLength(100).WithMessage("نام کاربری نمی‌تواند بیشتر از 100 کاراکتر باشد");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("رمز عبور الزامی است");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("نام الزامی است")
                .MaximumLength(100).WithMessage("نام نمی‌تواند بیشتر از 100 کاراکتر باشد");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("نام خانوادگی الزامی است")
                .MaximumLength(100).WithMessage("نام خانوادگی نمی‌تواند بیشتر از 100 کاراکتر باشد");

            RuleFor(x => x.MobileNumber)
                .NotEmpty().WithMessage("شماره موبایل الزامی است. برای بازیابی رمز عبور و MFA نیاز است.")
                .Matches(@"^09\d{9}$").WithMessage("شماره موبایل باید با 09 شروع شده و 11 رقم باشد (مثال: 09123456789)");

            RuleFor(x => x.GrpPublicId)
                .NotEmpty().WithMessage("گروه کاربری الزامی است");

            // FK ها: اگر مقدار داده شده، باید بزرگتر از 0 باشد
            RuleFor(x => x.tblCustomerId)
                .GreaterThan(0).WithMessage("شناسه مشتری باید بزرگتر از صفر باشد")
                .When(x => x.tblCustomerId.HasValue);

            RuleFor(x => x.tblShobeId)
                .GreaterThan(0).WithMessage("شناسه شعبه باید بزرگتر از صفر باشد")
                .When(x => x.tblShobeId.HasValue);

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("فرمت ایمیل نامعتبر است")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));
        }
    }
}
