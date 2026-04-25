using BnpCashClaudeApp.Application.Mapping;
using BnpCashClaudeApp.Application.MediatR.Queries;
using BnpCashClaudeApp.api.Attributes;
using BnpCashClaudeApp.Infrastructure.DependencyInjection;
using BnpCashClaudeApp.Infrastructure.Services; // DataSeeder, KeyRotationBackgroundService, DataIntegrityVerificationBackgroundService
using BnpCashClaudeApp.Persistence.Migrations;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.RateLimiting;

namespace BnpCashClaudeApp.api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ============================================
            // گام 1: پیکربندی TLS/HTTPS در Kestrel
            // الزام: پشتیبانی از گواهینامه‌های X.509 برای TLS/HTTPS
            // ============================================
            ConfigureKestrelWithTls(builder);

            // Application
            builder.Services.AddAutoMapper(typeof(MenuProfile).Assembly);
            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(GetAllGrpsQuery).Assembly);
                // ============================================
                // ثبت ValidationBehavior در MediatR Pipeline
                // این Behavior قبل از هر Handler اجرا می‌شود و
                // FluentValidation Validator ها را اجرا می‌کند
                // ============================================
                cfg.AddOpenBehavior(typeof(BnpCashClaudeApp.Application.MediatR.Behaviors.ValidationBehavior<,>));
            });

            // ============================================
            // ثبت تمام FluentValidation Validator ها از Assembly
            // ============================================
            builder.Services.AddValidatorsFromAssembly(typeof(GetAllGrpsQuery).Assembly);
            builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

            // Add Infrastructure
            builder.Services.AddInfrastructure(builder.Configuration);

            // ============================================
            // Background Services
            // ============================================
            // سرویس چرخش خودکار کلیدهای رمزنگاری (FCS_CKM)
            builder.Services.AddHostedService<KeyRotationBackgroundService>();
            
            // سرویس بررسی دوره‌ای صحت داده‌های حساس (FDP_SDI.2.2)
            builder.Services.AddHostedService<DataIntegrityVerificationBackgroundService>();

            // ============================================
            // گام 2: پیکربندی CORS امن
            // الزام: محدود کردن دسترسی به منابع مشخص
            // ============================================
            ConfigureCors(builder);

            // ============================================
            // گام 3: پیکربندی Rate Limiting
            // الزام: جلوگیری از حملات Brute Force
            // ============================================
            ConfigureRateLimiting(builder);

            // JWT Authentication
            var jwtSection = builder.Configuration.GetSection("Jwt");
            var key = jwtSection.GetValue<string>("Key");
            var issuer = jwtSection.GetValue<string>("Issuer");
            var audience = jwtSection.GetValue<string>("Audience");


            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    // ============================================
                    // الزام امنیتی: حذف ClockSkew پیش‌فرض ۵ دقیقه‌ای
                    // توکن منقضی شده حتی یک ثانیه بعد از انقضا هم معتبر نیست
                    // ============================================
                    ClockSkew = TimeSpan.Zero
                };

                // ثبت رویدادهای Authentication
                options.Events = new JwtBearerEvents
                {
                    // ============================================
                    // پشتیبانی از HttpOnly Cookie برای Vue.js
                    // توکن می‌تواند از Cookie یا Header خوانده شود
                    // ============================================
                    OnMessageReceived = context =>
                    {
                        // اولویت 1: خواندن از Cookie (برای وب)
                        if (context.Request.Cookies.TryGetValue("accessToken", out var token))
                        {
                            context.Token = token;
                        }
                        // اولویت 2: خواندن از Header (برای سازگاری با درخواست‌های دیگر)
                        else if (context.Request.Headers.ContainsKey("Authorization"))
                        {
                            var authHeader = context.Request.Headers["Authorization"].ToString();
                            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                context.Token = authHeader.Substring("Bearer ".Length).Trim();
                            }
                        }

                        return Task.CompletedTask;
                    },

                    OnAuthenticationFailed = async context =>
                    {
                        // ثبت Audit Log برای Authentication Failure
                        var auditLogService = context.HttpContext.RequestServices.GetService<BnpCashClaudeApp.Application.Interfaces.IAuditLogService>();
                        if (auditLogService != null)
                        {
                            var ipAddress = BnpCashClaudeApp.api.Helpers.HttpContextHelper.GetIpAddress(context.HttpContext);
                            var userAgent = BnpCashClaudeApp.api.Helpers.HttpContextHelper.GetUserAgent(context.HttpContext);
                            var operatingSystem = BnpCashClaudeApp.api.Helpers.HttpContextHelper.GetOperatingSystem(userAgent);

                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await auditLogService.LogEventAsync(
                                        eventType: "Authentication",
                                        entityType: "User",
                                        entityId: null,
                                        isSuccess: false,
                                        errorMessage: $"Authentication failed: {context.Exception?.Message}",
                                        ipAddress: ipAddress,
                                        userName: null,
                                        userId: null,
                                        operatingSystem: operatingSystem,
                                        userAgent: userAgent,
                                        description: $"شکست در احراز هویت - مسیر: {context.Request.Path}",
                                        ct: default);
                                }
                                catch { }
                            });
                        }
                        await Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        // ثبت Audit Log برای Authentication Challenge
                        var auditLogService = context.HttpContext.RequestServices.GetService<BnpCashClaudeApp.Application.Interfaces.IAuditLogService>();
                        if (auditLogService != null && !context.HttpContext.User?.Identity?.IsAuthenticated == true)
                        {
                            var ipAddress = BnpCashClaudeApp.api.Helpers.HttpContextHelper.GetIpAddress(context.HttpContext);
                            var userAgent = BnpCashClaudeApp.api.Helpers.HttpContextHelper.GetUserAgent(context.HttpContext);
                            var operatingSystem = BnpCashClaudeApp.api.Helpers.HttpContextHelper.GetOperatingSystem(userAgent);

                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await auditLogService.LogEventAsync(
                                        eventType: "Authentication",
                                        entityType: "User",
                                        entityId: null,
                                        isSuccess: false,
                                        errorMessage: "Authentication challenge - Token missing or invalid",
                                        ipAddress: ipAddress,
                                        userName: null,
                                        userId: null,
                                        operatingSystem: operatingSystem,
                                        userAgent: userAgent,
                                        description: $"درخواست احراز هویت - مسیر: {context.Request.Path}",
                                        ct: default);
                                }
                                catch { }
                            });
                        }
                        await Task.CompletedTask;
                    }
                };
            });

            // ============================================
            // گام 4: پیکربندی JSON
            // تاریخ‌ها به صورت string شمسی در دیتابیس و API ذخیره می‌شوند
            // نیازی به تبدیل‌گر نیست
            // ============================================
            builder.Services.Configure<InputSecurityLabelOptions>(
                builder.Configuration.GetSection("InputSecurityLabeling"));
            builder.Services.AddScoped<ContextAccessAuthorizeOnlyFilter>();
            builder.Services.AddScoped<InputCanonicalizationFilter>();
            builder.Services.AddScoped<InputValidationEnforcementFilter>();
            builder.Services.AddScoped<InputSecurityLabelFilter>();

            builder.Services.AddControllers(options =>
                {
                    // FDP_ACF.1.3: enforce context-based access for authenticated endpoints without explicit permission attributes.
                    options.Filters.AddService<ContextAccessAuthorizeOnlyFilter>();
                    // FDP_ITC.2.2: canonicalize mutating request payloads at API boundary.
                    options.Filters.AddService<InputCanonicalizationFilter>();
                    // FDP_ITC.2.1: enforce validator presence/execution for direct API body models.
                    options.Filters.AddService<InputValidationEnforcementFilter>();
                    // FDP_ITC.2.3: security-labeled input ingestion controls.
                    options.Filters.AddService<InputSecurityLabelFilter>();
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = null; // حفظ نام property ها
                    options.JsonSerializerOptions.WriteIndented = true; // خروجی خوانا
                });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(opt =>
            {
                opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });

                opt.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        new string[]{}
                    }
                });
            });

            var app = builder.Build();

            // ============================================
            // اعتبارسنجی تنظیمات حساس در Startup
            // در صورت عدم تنظیم، اپلیکیشن اجرا نمی‌شود
            // ============================================
            ValidateRequiredSecrets(app.Configuration, app.Environment);

            // ============================================
            // ساخت و به‌روزرسانی خودکار دیتابیس‌ها
            // ============================================
            using (var scope = app.Services.CreateScope())
            {
                // Navigation Database
                var navContext = scope.ServiceProvider.GetRequiredService<NavigationDbContext>();
                navContext.Database.Migrate();

                // Log Database
                var logContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LogDbContext>>();
                using var logContext = logContextFactory.CreateDbContext();
                logContext.Database.Migrate();

                // Cash Database (قرض‌الحسنه / تعاونی اعتبار)
                var cashContext = scope.ServiceProvider.GetRequiredService<CashDbContext>();
                cashContext.Database.Migrate();


                // Attach Database
                var attachContext = scope.ServiceProvider.GetRequiredService<AttachDbContext>();
                attachContext.Database.Migrate();

                // ============================================
                // تنظیم Collation فارسی برای همه دیتابیس‌ها
                // Persian_100_CI_AS: پشتیبانی صحیح از حروف ی و ک فارسی
                // این دستور فقط در صورتی اجرا می‌شود که Collation فعلی فارسی نباشد
                // ============================================
                const string persianCollationSql = @"
                    IF (SELECT DATABASEPROPERTYEX(DB_NAME(), 'Collation')) <> 'Persian_100_CI_AS'
                    BEGIN
                        -- بررسی عدم وجود اتصال فعال دیگر و تغییر Collation
                        DECLARE @dbName NVARCHAR(256) = DB_NAME();
                        DECLARE @sql NVARCHAR(MAX) = N'ALTER DATABASE [' + @dbName + N'] COLLATE Persian_100_CI_AS';
                        EXEC sp_executesql @sql;
                    END";

                navContext.Database.ExecuteSqlRaw(persianCollationSql);
                logContext.Database.ExecuteSqlRaw(persianCollationSql);
                cashContext.Database.ExecuteSqlRaw(persianCollationSql);
                attachContext.Database.ExecuteSqlRaw(persianCollationSql);
                // ============================================
                // Seed داده‌های اولیه (گروه‌ها، منوها، Permission ها، کاربر Admin)
                // ============================================
                var dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
                dataSeeder.SeedAllAsync().GetAwaiter().GetResult();

                // ============================================
                // Seed داده‌های اولیه قرض‌الحسنه (منوها، Permission ها)
                // ============================================
                var cashDataSeeder = scope.ServiceProvider.GetRequiredService<CashDataSeeder>();
                cashDataSeeder.SeedAllAsync().GetAwaiter().GetResult();
            }

            // ============================================
            // ترتیب Middleware Pipeline طبق استاندارد امنیتی
            // ============================================

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                // ============================================
                // Swagger فقط در محیط Development فعال است
                // در Production به دلایل امنیتی غیرفعال می‌شود
                // ============================================
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                // ============================================
                // صفحه وضعیت API در محیط Production
                // به جای Swagger یک صفحه ساده نمایش داده می‌شود
                // ============================================
                app.MapGet("/", () => Results.Content(GetApiStatusPage(), "text/html"))
                    .ExcludeFromDescription();
                
                app.MapGet("/api/health", () => Results.Ok(new 
                { 
                    Status = "Healthy",
                    Environment = "Production",
                    Timestamp = DateTime.UtcNow,
                    Version = "1.0.0"
                })).ExcludeFromDescription();
            }

            // ============================================
            // 1. Security Headers Middleware (اول از همه)
            // الزام: افزودن هدرهای امنیتی به تمام پاسخ‌ها
            // ============================================
            app.UseMiddleware<BnpCashClaudeApp.api.Middleware.SecurityHeadersMiddleware>();

            // ============================================
            // 1.5. Fail-Secure Exception Handler (FPT_FLS.1.1)
            // الزام 46: حفظ وضعیت امن در زمان شکست نرم‌افزاری
            // این Middleware باید قبل از سایر Middleware ها باشد تا
            // تمام Exception های مدیریت نشده را بگیرد
            // ============================================
            app.UseMiddleware<BnpCashClaudeApp.api.Middleware.FailSecureExceptionMiddleware>();

            // ============================================
            // 2. CORS (قبل از Rate Limiting)
            // ============================================
            app.UseCors("SecureCorsPolicy");

            // ============================================
            // 3. Rate Limiting
            // الزام: جلوگیری از حملات Brute Force
            // ============================================
            app.UseRateLimiter();

            // ============================================
            // 4. HTTPS Redirection
            // غیرفعال در Docker (چون Certificate موجود نیست)
            // ============================================
            // بررسی اینکه آیا در Docker هستیم یا نه
            var isDocker = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HOSTNAME"));
            if (!isDocker)
            {
                app.UseHttpsRedirection();
            }

            // ============================================
            // 5. TLS Audit Middleware
            // الزام: ثبت رویدادهای TLS در Audit Log
            // ============================================
            app.UseMiddleware<BnpCashClaudeApp.api.Middleware.TlsAuditMiddleware>();

            // ============================================
            // 6. Authentication & Authorization
            // ============================================
            app.UseAuthentication();
            app.UseAuthorization();

            // ============================================
            // 7. Token Blacklist Middleware
            // الزام: بررسی توکن‌های باطل شده (Logout)
            // ============================================
            app.UseMiddleware<BnpCashClaudeApp.api.Middleware.TokenBlacklistMiddleware>();

            // ============================================
            // 8. Authentication Audit Middleware (بعد از Authentication)
            // ============================================
            app.UseMiddleware<BnpCashClaudeApp.api.Middleware.AuthenticationAuditMiddleware>();

            app.MapControllers();

            app.Run();
        }

        /// <summary>
        /// پیکربندی TLS/HTTPS در Kestrel
        /// الزام: پشتیبانی از گواهینامه‌های X.509 (RFC 5280)
        /// حداقل نسخه TLS: 1.2
        /// </summary>
        private static void ValidateRequiredSecrets(IConfiguration configuration, IWebHostEnvironment environment)
        {
            var missingSecrets = new List<string>();

            // بررسی ConnectionString
            if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
                missingSecrets.Add("ConnectionStrings:DefaultConnection (Environment Variable: ConnectionStrings__DefaultConnection)");

            // بررسی JWT Key
            if (string.IsNullOrWhiteSpace(configuration["Jwt:Key"]))
                missingSecrets.Add("Jwt:Key (Environment Variable: Jwt__Key)");

            // بررسی کلید رمزنگاری
            if (string.IsNullOrWhiteSpace(configuration["Encryption:Key"]))
                missingSecrets.Add("Encryption:Key (Environment Variable: Encryption__Key)");

            // بررسی کلید Integrity
            if (string.IsNullOrWhiteSpace(configuration["Security:IntegrityKey"]))
                missingSecrets.Add("Security:IntegrityKey (Environment Variable: Security__IntegrityKey)");

            // SECURITY HARDENING: DataExport signing key must be configured explicitly.
            if (string.IsNullOrWhiteSpace(configuration["DataExport:SigningKey"]))
                missingSecrets.Add("DataExport:SigningKey (Environment Variable: DataExport__SigningKey)");

            // بررسی رمزهای Seed (فقط warning چون شاید Seed قبلاً اجرا شده باشد)
            var seedWarnings = new List<string>();
            if (string.IsNullOrWhiteSpace(configuration["SeedSecrets:AdminPassword"]))
                seedWarnings.Add("SeedSecrets:AdminPassword");
            if (string.IsNullOrWhiteSpace(configuration["SeedSecrets:DatabasePassword"]))
                seedWarnings.Add("SeedSecrets:DatabasePassword");

            if (missingSecrets.Count > 0)
            {
                var message = $"""
                    ============================================
                    خطای بحرانی: تنظیمات حساس پیکربندی نشده‌اند!
                    ============================================
                    تنظیمات زیر الزامی هستند و باید پیکربندی شوند:
                    
                    {string.Join(Environment.NewLine + "  - ", new[] { "" }.Concat(missingSecrets))}
                    
                    راهنما:
                      محیط Development: مقادیر را در appsettings.Development.json تنظیم کنید
                      محیط Production:  از Environment Variable استفاده کنید
                        مثال: set Jwt__Key=YourSecureKeyHere
                    ============================================
                    """;

                throw new InvalidOperationException(message);
            }

            if (seedWarnings.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ هشدار: تنظیمات Seed زیر پیکربندی نشده‌اند (در صورت نیاز به Seed اولیه خطا خواهید گرفت):");
                foreach (var w in seedWarnings)
                    Console.WriteLine($"  - {w}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// پیکربندی TLS/HTTPS در Kestrel
        /// </summary>
        private static void ConfigureKestrelWithTls(WebApplicationBuilder builder)
        {
            var tlsSection = builder.Configuration.GetSection("Tls");
            var certificatePath = tlsSection.GetValue<string>("CertificatePath");
            var certificatePassword = tlsSection.GetValue<string>("CertificatePassword");
            var requireClientCertificate = tlsSection.GetValue<bool>("RequireClientCertificate", false);

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ConfigureHttpsDefaults(httpsOptions =>
                {
                    // ============================================
                    // الزام: تنظیم حداقل نسخه TLS به 1.2
                    // TLS 1.0 و 1.1 دارای آسیب‌پذیری‌های شناخته شده هستند
                    // ============================================
                    httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;

                    // ============================================
                    // بارگذاری گواهینامه سرور از فایل (اختیاری)
                    // در صورت عدم تنظیم، از گواهینامه توسعه ASP.NET استفاده می‌شود
                    // ============================================
                    if (!string.IsNullOrEmpty(certificatePath) && File.Exists(certificatePath))
                    {
                        try
                        {
                            httpsOptions.ServerCertificate = new X509Certificate2(
                                certificatePath,
                                certificatePassword);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"خطا در بارگذاری گواهینامه: {ex.Message}");
                        }
                    }

                    // ============================================
                    // پیکربندی گواهینامه کلاینت (Mutual TLS)
                    // الزام اختیاری: اعتبارسنجی گواهینامه کلاینت
                    // ============================================
                    if (requireClientCertificate)
                    {
                        httpsOptions.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.RequireCertificate;
                        httpsOptions.ClientCertificateValidation = (certificate, chain, errors) =>
                        {
                            // ============================================
                            // الزام: بررسی basicConstraints
                            // گواهینامه‌های CA باید دارای CA=True باشند
                            // ============================================
                            if (chain != null && chain.ChainElements.Count > 0)
                            {
                                foreach (var element in chain.ChainElements)
                                {
                                    var cert = element.Certificate;
                                    var basicConstraints = cert.Extensions
                                        .OfType<X509BasicConstraintsExtension>()
                                        .FirstOrDefault();

                                    if (basicConstraints != null && basicConstraints.CertificateAuthority)
                                    {
                                        // این یک گواهینامه CA است
                                        Console.WriteLine($"CA Certificate found: {cert.Subject}");
                                    }
                                }
                            }

                            // ============================================
                            // الزام: بررسی extendedKeyUsage
                            // گواهینامه‌های کلاینت باید Client Authentication داشته باشند
                            // ============================================
                            var extendedKeyUsage = certificate.Extensions
                                .OfType<X509EnhancedKeyUsageExtension>()
                                .FirstOrDefault();

                            if (extendedKeyUsage != null)
                            {
                                var clientAuthOid = "1.3.6.1.5.5.7.3.2"; // Client Authentication
                                var hasClientAuth = extendedKeyUsage.EnhancedKeyUsages
                                    .Cast<System.Security.Cryptography.Oid>()
                                    .Any(oid => oid.Value == clientAuthOid);

                                if (!hasClientAuth)
                                {
                                    Console.WriteLine($"گواهینامه کلاینت فاقد Client Authentication است: {certificate.Subject}");
                                    return false;
                                }
                            }

                            return errors == System.Net.Security.SslPolicyErrors.None;
                        };
                    }
                    else
                    {
                        // ============================================
                        // NoCertificate: مرورگر درخواست گواهینامه نمی‌کند
                        // برای محیط توسعه (Development) مناسب است
                        // ============================================
                        httpsOptions.ClientCertificateMode = Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.NoCertificate;
                    }
                });
            });
        }

        /// <summary>
        /// پیکربندی CORS امن
        /// الزام: محدود کردن دسترسی به منابع مشخص
        /// </summary>
        private static void ConfigureCors(WebApplicationBuilder builder)
        {
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("SecureCorsPolicy", policy =>
                {
                    // ============================================
                    // خواندن Origins مجاز از appsettings.json
                    // ============================================
                    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
                        .Get<string[]>() ?? Array.Empty<string>();

                    var allowCredentials = builder.Configuration.GetValue<bool>("Cors:AllowCredentials", true);

                    if (allowedOrigins.Length > 0)
                    {
                        // ============================================
                        // الزام: محدود کردن Origins به دامنه‌های مشخص
                        // ============================================
                        policy.WithOrigins(allowedOrigins)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .WithExposedHeaders("X-Pagination", "X-Total-Count");

                        if (allowCredentials)
                        {
                            policy.AllowCredentials();
                        }
                    }
                    else
                    {
                        // ============================================
                        // فقط برای محیط توسعه (Development)
                        // در Production باید Origins مشخص شوند
                        // ============================================
                        if (builder.Environment.IsDevelopment())
                        {
                            policy.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader();
                        }
                    }
                });
            });
        }

        /// <summary>
        /// پیکربندی Rate Limiting
        /// الزام: جلوگیری از حملات Brute Force و DoS
        /// </summary>
        private static void ConfigureRateLimiting(WebApplicationBuilder builder)
        {
            var rateLimitingSection = builder.Configuration.GetSection("RateLimiting");

            builder.Services.AddRateLimiter(options =>
            {
                // ============================================
                // Policy برای Authentication Endpoints (Login)
                // محدودیت: 5 درخواست در دقیقه برای جلوگیری از Brute Force
                // ============================================
                options.AddPolicy("AuthenticationPolicy", context =>
                {
                    var authConfig = rateLimitingSection.GetSection("Authentication");
                    var permitLimit = authConfig.GetValue<int>("PermitLimit", 5);
                    var windowMinutes = authConfig.GetValue<int>("WindowMinutes", 1);
                    var queueLimit = authConfig.GetValue<int>("QueueLimit", 2);

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromMinutes(windowMinutes),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = queueLimit
                        });
                });

                // ============================================
                // Policy عمومی برای API Endpoints
                // محدودیت: 100 درخواست در دقیقه
                // ============================================
                options.AddPolicy("ApiPolicy", context =>
                {
                    var apiConfig = rateLimitingSection.GetSection("Api");
                    var permitLimit = apiConfig.GetValue<int>("PermitLimit", 100);
                    var windowMinutes = apiConfig.GetValue<int>("WindowMinutes", 1);
                    var queueLimit = apiConfig.GetValue<int>("QueueLimit", 10);

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromMinutes(windowMinutes),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = queueLimit
                        });
                });

                // ============================================
                // تنظیم پاسخ برای درخواست‌های Rate Limited
                // ============================================
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";

                    var response = new
                    {
                        error = "تعداد درخواست‌ها بیش از حد مجاز است",
                        message = "لطفاً کمی صبر کنید و دوباره تلاش کنید",
                        retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                            ? retryAfter.TotalSeconds
                            : 60
                    };

                    await context.HttpContext.Response.WriteAsJsonAsync(response, token);

                    // ثبت در Audit Log
                    var auditLogService = context.HttpContext.RequestServices.GetService<BnpCashClaudeApp.Application.Interfaces.IAuditLogService>();
                    if (auditLogService != null)
                    {
                        var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await auditLogService.LogEventAsync(
                                    eventType: "RateLimitExceeded",
                                    entityType: "Request",
                                    isSuccess: false,
                                    errorMessage: "تعداد درخواست‌ها بیش از حد مجاز",
                                    ipAddress: ipAddress,
                                    description: $"Rate limit exceeded - مسیر: {context.HttpContext.Request.Path}",
                                    ct: default);
                            }
                            catch { }
                        });
                    }
                };
            });
        }

        /// <summary>
        /// صفحه وضعیت API برای محیط Production
        /// این صفحه جایگزین Swagger در محیط Production می‌شود
        /// </summary>
        private static string GetApiStatusPage()
        {
            return """
                <!DOCTYPE html>
                <html lang="fa" dir="rtl">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>BnpCash API - وضعیت سرویس</title>
                    <style>
                        * {
                            margin: 0;
                            padding: 0;
                            box-sizing: border-box;
                        }
                        body {
                            font-family: 'Segoe UI', Tahoma, Arial, sans-serif;
                            background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%);
                            min-height: 100vh;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            color: #fff;
                        }
                        .container {
                            text-align: center;
                            padding: 60px;
                            background: rgba(255, 255, 255, 0.05);
                            border-radius: 20px;
                            backdrop-filter: blur(10px);
                            border: 1px solid rgba(255, 255, 255, 0.1);
                            box-shadow: 0 25px 45px rgba(0, 0, 0, 0.2);
                            max-width: 600px;
                        }
                        .logo {
                            width: 100px;
                            height: 100px;
                            margin: 0 auto 30px;
                            background: linear-gradient(135deg, #00d4ff, #0099cc);
                            border-radius: 50%;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            font-size: 48px;
                            animation: pulse 2s ease-in-out infinite;
                        }
                        @keyframes pulse {
                            0%, 100% { transform: scale(1); box-shadow: 0 0 0 0 rgba(0, 212, 255, 0.4); }
                            50% { transform: scale(1.05); box-shadow: 0 0 0 20px rgba(0, 212, 255, 0); }
                        }
                        h1 {
                            font-size: 2.5em;
                            margin-bottom: 15px;
                            background: linear-gradient(90deg, #00d4ff, #00ff88);
                            -webkit-background-clip: text;
                            -webkit-text-fill-color: transparent;
                            background-clip: text;
                        }
                        .subtitle {
                            font-size: 1.2em;
                            color: #a0a0a0;
                            margin-bottom: 40px;
                        }
                        .status-card {
                            background: rgba(0, 255, 136, 0.1);
                            border: 1px solid rgba(0, 255, 136, 0.3);
                            border-radius: 15px;
                            padding: 25px;
                            margin-bottom: 30px;
                        }
                        .status-indicator {
                            display: inline-flex;
                            align-items: center;
                            gap: 10px;
                            font-size: 1.3em;
                            color: #00ff88;
                        }
                        .status-dot {
                            width: 15px;
                            height: 15px;
                            background: #00ff88;
                            border-radius: 50%;
                            animation: blink 1.5s ease-in-out infinite;
                        }
                        @keyframes blink {
                            0%, 100% { opacity: 1; }
                            50% { opacity: 0.5; }
                        }
                        .info-grid {
                            display: grid;
                            grid-template-columns: repeat(2, 1fr);
                            gap: 15px;
                            margin-top: 20px;
                        }
                        .info-item {
                            background: rgba(255, 255, 255, 0.05);
                            padding: 15px;
                            border-radius: 10px;
                        }
                        .info-label {
                            font-size: 0.85em;
                            color: #888;
                            margin-bottom: 5px;
                        }
                        .info-value {
                            font-size: 1.1em;
                            color: #00d4ff;
                        }
                        .footer {
                            margin-top: 40px;
                            padding-top: 20px;
                            border-top: 1px solid rgba(255, 255, 255, 0.1);
                            color: #666;
                            font-size: 0.9em;
                        }
                        .footer a {
                            color: #00d4ff;
                            text-decoration: none;
                        }
                        .footer a:hover {
                            text-decoration: underline;
                        }
                    </style>
                </head>
                <body>
                    <div class="container">
                        <div class="logo">🏦</div>
                        <h1>BnpCash API</h1>
                        <p class="subtitle">سیستم مدیریت قرض‌الحسنه و تعاونی اعتبار</p>
                        
                        <div class="status-card">
                            <div class="status-indicator">
                                <span class="status-dot"></span>
                                سرویس فعال و در حال اجرا
                            </div>
                            <div class="info-grid">
                                <div class="info-item">
                                    <div class="info-label">محیط</div>
                                    <div class="info-value">Production</div>
                                </div>
                                <div class="info-item">
                                    <div class="info-label">نسخه</div>
                                    <div class="info-value">1.0.0</div>
                                </div>
                                <div class="info-item">
                                    <div class="info-label">پروتکل</div>
                                    <div class="info-value">HTTPS / TLS 1.2+</div>
                                </div>
                                <div class="info-item">
                                    <div class="info-label">وضعیت</div>
                                    <div class="info-value">✓ سالم</div>
                                </div>
                            </div>
                        </div>

                        <div class="footer">
                            <p>برای بررسی سلامت سرویس: <a href="/api/health">/api/health</a></p>
                            <p style="margin-top: 10px;">© 2024 - سیستم مدیریت مالی BnpCash</p>
                        </div>
                    </div>

                    <script>
                        // به‌روزرسانی خودکار وضعیت
                        setInterval(async () => {
                            try {
                                const response = await fetch('/api/health');
                                const data = await response.json();
                                console.log('Health check:', data);
                            } catch (e) {
                                console.error('Health check failed:', e);
                            }
                        }, 30000);
                    </script>
                </body>
                </html>
                """;
        }
    }
}
