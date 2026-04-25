using BnpCashClaudeApp.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Commands
{
    /// <summary>
    /// کامند به‌روزرسانی تنظیمات قفل حساب کاربری
    /// </summary>
    public class UpdateAccountLockoutSettingsCommand : IRequest<bool>
    {
        /// <summary>
        /// حداکثر تعداد تلاش‌های ناموفق قبل از قفل شدن
        /// </summary>
        public int MaxFailedAttempts { get; set; }

        /// <summary>
        /// مدت زمان قفل حساب به دقیقه
        /// </summary>
        public int LockoutDurationMinutes { get; set; }

        /// <summary>
        /// آیا قفل دائمی فعال است
        /// </summary>
        public bool EnablePermanentLockout { get; set; }

        /// <summary>
        /// تعداد تلاش‌های ناموفق برای قفل دائمی
        /// </summary>
        public int PermanentLockoutThreshold { get; set; }

        /// <summary>
        /// مدت زمان ریست شدن شمارنده تلاش‌های ناموفق (به دقیقه)
        /// </summary>
        public int FailedAttemptResetMinutes { get; set; }

        /// <summary>
        /// شناسه کاربر ویرایش‌کننده
        /// </summary>
        public long UserId { get; set; }
    }

    /// <summary>
    /// هندلر به‌روزرسانی تنظیمات قفل حساب کاربری
    /// </summary>
    public class UpdateAccountLockoutSettingsCommandHandler : IRequestHandler<UpdateAccountLockoutSettingsCommand, bool>
    {
        private readonly ISecuritySettingsService _securitySettingsService;

        public UpdateAccountLockoutSettingsCommandHandler(ISecuritySettingsService securitySettingsService)
        {
            _securitySettingsService = securitySettingsService;
        }

        public async Task<bool> Handle(UpdateAccountLockoutSettingsCommand request, CancellationToken cancellationToken)
        {
            var settings = new AccountLockoutSettings
            {
                MaxFailedAttempts = request.MaxFailedAttempts,
                LockoutDurationMinutes = request.LockoutDurationMinutes,
                EnablePermanentLockout = request.EnablePermanentLockout,
                PermanentLockoutThreshold = request.PermanentLockoutThreshold,
                FailedAttemptResetMinutes = request.FailedAttemptResetMinutes
            };

            await _securitySettingsService.SaveAccountLockoutSettingsAsync(settings, request.UserId, cancellationToken);
            return true;
        }
    }

    /// <summary>
    /// کامند به‌روزرسانی تنظیمات سیاست رمز عبور
    /// </summary>
    public class UpdatePasswordPolicySettingsCommand : IRequest<bool>
    {
        /// <summary>
        /// حداقل طول رمز عبور
        /// </summary>
        public int MinimumLength { get; set; }

        /// <summary>
        /// حداکثر طول رمز عبور
        /// </summary>
        public int MaximumLength { get; set; }

        /// <summary>
        /// آیا حداقل یک حرف بزرگ الزامی است
        /// </summary>
        public bool RequireUppercase { get; set; }

        /// <summary>
        /// آیا حداقل یک حرف کوچک الزامی است
        /// </summary>
        public bool RequireLowercase { get; set; }

        /// <summary>
        /// آیا حداقل یک عدد الزامی است
        /// </summary>
        public bool RequireDigit { get; set; }

        /// <summary>
        /// آیا حداقل یک کاراکتر خاص الزامی است
        /// </summary>
        public bool RequireSpecialCharacter { get; set; }

        /// <summary>
        /// لیست کاراکترهای خاص مجاز
        /// </summary>
        public string SpecialCharacters { get; set; } = "!@#$%^&*()_+-=[]{}|;':\",./<>?";

        /// <summary>
        /// آیا رمز عبور نباید شامل نام کاربری باشد
        /// </summary>
        public bool DisallowUsername { get; set; }

        /// <summary>
        /// تعداد رمزهای قبلی که نباید تکرار شوند
        /// </summary>
        public int PasswordHistoryCount { get; set; }

        /// <summary>
        /// مدت اعتبار رمز عبور به روز (0 = بدون انقضا)
        /// </summary>
        public int PasswordExpirationDays { get; set; }

        /// <summary>
        /// شناسه کاربر ویرایش‌کننده
        /// </summary>
        public long UserId { get; set; }
    }

    /// <summary>
    /// هندلر به‌روزرسانی تنظیمات سیاست رمز عبور
    /// </summary>
    public class UpdatePasswordPolicySettingsCommandHandler : IRequestHandler<UpdatePasswordPolicySettingsCommand, bool>
    {
        private readonly ISecuritySettingsService _securitySettingsService;

        public UpdatePasswordPolicySettingsCommandHandler(ISecuritySettingsService securitySettingsService)
        {
            _securitySettingsService = securitySettingsService;
        }

        public async Task<bool> Handle(UpdatePasswordPolicySettingsCommand request, CancellationToken cancellationToken)
        {
            var settings = new PasswordPolicySettings
            {
                MinimumLength = request.MinimumLength,
                MaximumLength = request.MaximumLength,
                RequireUppercase = request.RequireUppercase,
                RequireLowercase = request.RequireLowercase,
                RequireDigit = request.RequireDigit,
                RequireSpecialCharacter = request.RequireSpecialCharacter,
                SpecialCharacters = request.SpecialCharacters,
                DisallowUsername = request.DisallowUsername,
                PasswordHistoryCount = request.PasswordHistoryCount,
                PasswordExpirationDays = request.PasswordExpirationDays
            };

            await _securitySettingsService.SavePasswordPolicySettingsAsync(settings, request.UserId, cancellationToken);
            return true;
        }
    }
}

