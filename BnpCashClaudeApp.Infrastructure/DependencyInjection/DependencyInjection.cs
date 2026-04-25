using System.Linq;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.Settings;
using BnpCashClaudeApp.Domain.Entities.CashSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using BnpCashClaudeApp.Infrastructure.Repositories;
using BnpCashClaudeApp.Infrastructure.Services;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BnpCashClaudeApp.Infrastructure.DependencyInjection
{
    /// <summary>
    /// کلاس تنظیم Dependency Injection برای لایه Infrastructure
    /// ============================================
    /// تمام سرویس‌های امنیتی ISO 15408 در اینجا ثبت می‌شوند
    /// ============================================
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // ============================================
            // Navigation Database
            // ============================================
            // ابتدا ثبت با AddDbContext برای سازگاری کامل با EF Core (migrations, etc.)
            services.AddDbContext<NavigationDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            }, ServiceLifetime.Scoped);
            
            // حذف ثبت پیش‌فرض و جایگزینی با factory سفارشی
            // این کار برای تزریق IServiceProvider جهت دسترسی به IDataIntegrityService
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(NavigationDbContext));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            services.AddScoped<NavigationDbContext>(sp =>
            {
                var options = sp.GetRequiredService<DbContextOptions<NavigationDbContext>>();
                return new NavigationDbContext(options, sp);
            });

            // ============================================
            // سرویس مدیریت Connection String های دیتابیس
            // این سرویس Connection String ها را از jدول tblDbs می‌خواند
            // ============================================
            services.AddScoped<IDatabaseConnectionService, DatabaseConnectionService>();

            // ============================================
            // Audit Log Database با DbContextFactory
            // Connection String از tblDbs خوانده می‌شود (DbCode: LOG-DB)
            // در صورت عدم وجود، از appsettings استفاده می‌شود
            // ============================================
            services.AddDbContextFactory<LogDbContext>((sp, options) =>
            {
                // ابتدا تلاش برای خواندن از tblDbs از طریق NavigationDbContext
                // چون در زمان Startup هنوز tblDbs خالی است، از appsettings استفاده می‌کنیم
                // بعداً می‌توان یک Background Service برای به‌روزرسانی Connection String اضافه کرد
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                
                // ساخت Connection String برای Log Database
                // با تغییر نام دیتابیس از NavigationDb به BnpLogCloudDB
                if (!string.IsNullOrEmpty(connectionString))
                {
                    var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
                    {
                        InitialCatalog = "BnpLogCloudDB"
                    };
                    connectionString = builder.ConnectionString;
                }
                
                options.UseSqlServer(connectionString);
            });

            // ============================================
            // Cash Database (قرض‌الحسنه / تعاونی اعتبار)
            // دیتابیس: BnpCashCloudDB
            // Connection String از DefaultConnection با تغییر نام دیتابیس
            // ============================================
            services.AddDbContext<CashDbContext>((sp, options) =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                
                if (!string.IsNullOrEmpty(connectionString))
                {
                    var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
                    {
                        InitialCatalog = "BnpCashCloudDB"
                    };
                    connectionString = builder.ConnectionString;
                }
                
                options.UseSqlServer(connectionString);
            }, ServiceLifetime.Scoped);

            // ============================================
            // Attachment Database (فایل‌ها و تصاویر)
            // دیتابیس: BnpAttachCloudDB
            // Connection String از DefaultConnection با تغییر نام دیتابیس
            // استفاده از DbContextFactory برای جلوگیری از مشکلات concurrent access
            // ============================================
            services.AddDbContextFactory<AttachDbContext>((sp, options) =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                
                if (!string.IsNullOrEmpty(connectionString))
                {
                    var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
                    {
                        InitialCatalog = "BnpAttachCloudDB"
                    };
                    connectionString = builder.ConnectionString;
                }
                
                options.UseSqlServer(connectionString);
            });

            // ============================================
            // Memory Cache برای سرویس‌های Lockout و Blacklist
            // ============================================
            services.AddMemoryCache();

            // ============================================
            // HttpClient برای درخواست‌های OCSP و CRL
            // ============================================
            services.AddHttpClient();

            // ============================================
            // Repositories
            // ============================================
            // Repository پیش‌فرض برای NavigationDbContext
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // ============================================
            // Cash Repositories - برای Entity های CashDbContext
            // ============================================
            services.AddScoped<IRepository<tblSarfasl>, CashRepository<tblSarfasl>>();
            services.AddScoped<IRepository<tblSarfaslType>, CashRepository<tblSarfaslType>>();
            services.AddScoped<IRepository<tblSarfaslProtocol>, CashRepository<tblSarfaslProtocol>>();
            services.AddScoped<IRepository<tblCombo>, CashRepository<tblCombo>>();
            services.AddScoped<IRepository<tblTafsiliType>, CashRepository<tblTafsiliType>>();
            services.AddScoped<IRepository<tblAzaNoe>, CashRepository<tblAzaNoe>>();

            // ============================================
            // Security Services (ISO 15408 Requirements)
            // ============================================

            services.Configure<CryptographicPolicySettings>(configuration.GetSection("Security:CryptographyPolicy"));
            services.AddSingleton<ICryptographicAlgorithmPolicyService, CryptographicAlgorithmPolicyService>();

            // ============================================
            // سرویس پاکسازی امن اطلاعات باقیمانده (FDP_RIP.2)
            // الزام FDP_RIP.2.1 از ISO 15408 - حفاظت از اطلاعات باقیمانده
            // ============================================
            services.AddScoped<ISecureMemoryService, SecureMemoryService>();

            // سرویس Hash کردن رمز عبور
            services.AddScoped<IPasswordHasher, PasswordHasher>();

            // سرویس قفل حساب کاربری
            services.AddScoped<IAccountLockoutService, AccountLockoutService>();

            // سرویس سیاست رمز عبور
            services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();

            // سرویس تاریخچه رمز عبور (Password History)
            // الزام FDP (User Data Protection) از ISO 15408
            services.AddScoped<IPasswordHistoryService, PasswordHistoryService>();

            // سرویس Blacklist توکن
            services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();

            // سرویس احرازهویت چندگانه (MFA)
            // الزام FIA_UAU.5 از ISO 15408
            services.AddScoped<IMfaService, MfaService>();

            // سرویس بازیابی رمز عبور با OTP
            // ارسال کد یکبار مصرف به موبایل و تغییر رمز عبور
            services.AddScoped<IPasswordResetService, PasswordResetService>();

            // سرویس ارسال پیامک
            // استفاده برای ارسال هشدارها و اعلان‌های سیستم (FAU_STG.3.1)
            services.AddScoped<ISmsService, SmsService>();

            // سرویس CAPTCHA
            // الزام FIA_UAU.5 - لایه امنیتی اضافی برای MFA
            services.AddScoped<ICaptchaService, CaptchaService>();

            // سرویس Audit Log
            services.AddScoped<IAuditLogService, AuditLogService>();

            // سرویس اعتبارسنجی گواهینامه X.509
            services.AddScoped<IX509CertificateValidationService, X509CertificateValidationService>();

            // #region agent log
            // سرویس تنظیمات امنیتی از دیتابیس (FIX: این سرویس قبلاً ثبت نشده بود)
            services.AddScoped<ISecuritySettingsService, SecuritySettingsService>();
            //System.IO.File.AppendAllText(@"c:\BnpProject\BnpCashClaudeApp\BnpCashClaudeApp\.cursor\debug.log",
            //    System.Text.Json.JsonSerializer.Serialize(new { hypothesisId = "A", location = "DependencyInjection.cs:78", message = "ISecuritySettingsService registered", timestamp = System.DateTime.UtcNow.Ticks }) + "\n");
            // #endregion

            // سرویس تنظیمات شعبه از دیتابیس
            services.AddScoped<IShobeSettingsService, ShobeSettingsService>();

            // سرویس Refresh Token
            // الزام: تمدید خودکار Access Token بدون logout مکرر کاربر
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();

            // ============================================
            // سرویس Permission (FDP_ACF)
            // الزام FDP_ACF از ISO 15408 - کنترل دسترسی دقیق
            // ============================================
            services.AddSingleton<IResourceAuthorizationPolicyService, ResourceAuthorizationPolicyService>();
            services.AddScoped<IPermissionService, PermissionService>();

            // ============================================
            // سرویس کنترل دسترسی مبتنی بر Context (FDP_ACF.1.4)
            // الزام FDP_ACF.1.4 از ISO 15408 - عملیات کنترل دسترسی 4
            // کنترل دسترسی بر اساس: IP، زمان، مکان، نوع دستگاه
            // ============================================
            services.AddScoped<IContextAccessControlService, ContextAccessControlService>();

            // ============================================
            // سرویس‌های مدیریت کلید رمزنگاری (FCS_CKM)
            // الزام FCS_CKM.1.1 - تولید کلید رمزنگاری
            // الزام FCS_CKM.4.1 - تخریب کلید رمزنگاری
            // ============================================
            services.AddScoped<IKeyGenerationService, KeyGenerationService>();
            services.AddScoped<IKeyManagementService, KeyManagementService>();

            // ============================================
            // سرویس بررسی صحت داده‌های ذخیره شده (FDP_SDI)
            // الزام FDP_SDI.2.1 - Integrity Checks برای داده‌های حساس
            // الزام FDP_SDI.2.2 - Periodic Integrity Verification
            // ============================================
            services.AddScoped<IDataIntegrityService, DataIntegrityService>();

            // ============================================
            // سرویس اعتبارسنجی امضای دیجیتال به‌روزرسانی (FPT_TUD_EXT.1.3)
            // الزام 51 - اعتبارسنجی امضای دیجیتال قبل از نصب به‌روزرسانی
            // ============================================
            services.AddScoped<IUpdateSignatureVerificationService, UpdateSignatureVerificationService>();

            // ============================================
            // سرویس Fail-Secure (FPT_FLS.1.1)
            // الزام 46 - حفظ وضعیت امن در زمان شکست
            // ============================================
            services.Configure<FailSecureSettings>(configuration.GetSection("FailSecure"));
            services.AddScoped<IFailSecureService, FailSecureService>();

            // ============================================
            // سرویس‌های حفاظت از داده‌های ممیزی (FAU_STG.3.1, FAU_STG.4.1)
            // الزام FAU_STG.3.1 - اقدامات لازم در زمان از دست رفتن داده ممیزی
            //   - ارسال هشدار در صورت شکست ذخیره‌سازی
            //   - مکانیزم Retry با Exponential Backoff
            //   - ذخیره‌سازی جایگزین در فایل سیستم (Fallback)
            // الزام FAU_STG.4.1 - پیشگیری از اتلاف و از بین رفتن داده ممیزی
            //   - پشتیبان‌گیری خودکار
            //   - سیاست نگهداری (Retention Policy)
            //   - آرشیو داده‌های قدیمی
            // ============================================
            services.AddScoped<IAuditLogProtectionService, AuditLogProtectionService>();
            services.AddHostedService<AuditLogBackupBackgroundService>();

            // ============================================
            // سرویس خروجی داده با ویژگی‌های امنیتی (FDP_ETC.2)
            // الزام FDP_ETC.2.1 - خروجی داده با ویژگی‌های امنیتی مرتبط
            // الزام FDP_ETC.2.2 - ارتباط بدون ابهام ویژگی‌های امنیتی
            // الزام FDP_ETC.2.4 - قوانین کنترل خروجی اضافی
            // ============================================
            services.AddScoped<IDataExportService, DataExportService>();

            // ============================================
            // سرویس‌های مدیریت فایل‌های پیوست (FDP_ITC.2, FDP_ETC.2, FDP_SDI.2)
            // الزام FDP_ITC.2.1-3 - ورود داده با مشخصه امنیتی
            // الزام FDP_ETC.2.1-4 - خروج داده با مشخصه امنیتی
            // الزام FDP_SDI.2.1-2 - صحت داده ذخیره شده
            // الزام FDP_RIP.2.1 - حفاظت اطلاعات باقیمانده (حذف امن)
            // ============================================
            services.AddScoped<IAttachmentService, AttachmentService>();

            // ============================================
            // سرویس لاگ دسترسی به فایل‌های پیوست (FAU_GEN, FTA_TAH)
            // الزام FAU_GEN.1.1-2 - تولید داده ممیزی
            // الزام FAU_GEN.2.1 - مرتبط نمودن هویت کاربر
            // الزام FTA_TAH.1.1-3 - سوابق دسترسی به محصول
            // ============================================
            services.AddScoped<IAttachmentAccessLogService, AttachmentAccessLogService>();

            // سرویس DataSeeder
            services.AddScoped<DataSeeder>();

            // سرویس CashDataSeeder (قرض‌الحسنه)
            services.AddScoped<CashDataSeeder>();

            return services;
        }
    }
}
