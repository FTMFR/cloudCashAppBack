using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BnpCashClaudeApp.Infrastructure.DependencyInjection
{
    /// <summary>
    /// ثبت سرویس‌های راهبری سیستم در DI Container
    /// جداول راهبری در همان NavigationDbContext هستند
    /// </summary>
    public static class ManagementServiceRegistration
    {
        /// <summary>
        /// ثبت سرویس‌های راهبری
        /// نکته: NavigationDbContext قبلاً در جای دیگر ثبت شده، فقط سرویس را ثبت می‌کنیم
        /// </summary>
        public static IServiceCollection AddManagementServices(this IServiceCollection services, IConfiguration configuration)
        {
            // ثبت سرویس راهبری
            // NavigationDbContext قبلاً در Program.cs ثبت شده است
            services.AddScoped<IManagementService, ManagementService>();

            return services;
        }
    }
}
