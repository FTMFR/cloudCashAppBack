using BnpCashClaudeApp.Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Queries
{
    /// <summary>
    /// کوئری دریافت تنظیمات قفل حساب کاربری
    /// </summary>
    public class GetAccountLockoutSettingsQuery : IRequest<AccountLockoutSettings>
    {
    }

    /// <summary>
    /// هندلر دریافت تنظیمات قفل حساب کاربری
    /// </summary>
    public class GetAccountLockoutSettingsQueryHandler : IRequestHandler<GetAccountLockoutSettingsQuery, AccountLockoutSettings>
    {
        private readonly ISecuritySettingsService _securitySettingsService;

        public GetAccountLockoutSettingsQueryHandler(ISecuritySettingsService securitySettingsService)
        {
            _securitySettingsService = securitySettingsService;
        }

        public async Task<AccountLockoutSettings> Handle(GetAccountLockoutSettingsQuery request, CancellationToken cancellationToken)
        {
            return await _securitySettingsService.GetAccountLockoutSettingsAsync(cancellationToken);
        }
    }

    /// <summary>
    /// کوئری دریافت تنظیمات سیاست رمز عبور
    /// </summary>
    public class GetPasswordPolicySettingsQuery : IRequest<PasswordPolicySettings>
    {
    }

    /// <summary>
    /// هندلر دریافت تنظیمات سیاست رمز عبور
    /// </summary>
    public class GetPasswordPolicySettingsQueryHandler : IRequestHandler<GetPasswordPolicySettingsQuery, PasswordPolicySettings>
    {
        private readonly ISecuritySettingsService _securitySettingsService;

        public GetPasswordPolicySettingsQueryHandler(ISecuritySettingsService securitySettingsService)
        {
            _securitySettingsService = securitySettingsService;
        }

        public async Task<PasswordPolicySettings> Handle(GetPasswordPolicySettingsQuery request, CancellationToken cancellationToken)
        {
            return await _securitySettingsService.GetPasswordPolicySettingsAsync(cancellationToken);
        }
    }
}

