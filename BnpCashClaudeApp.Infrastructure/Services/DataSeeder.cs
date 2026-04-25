using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Entities.ManagementSubsystem;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Entities.SecuritySubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// سرویس Seed اولیه داده‌ها
    /// ============================================
    /// شامل Seed کردن تمام اطلاعات پایه سیستم
    /// این نسخه ابتدا داده‌های قدیمی را پاک‌سازی و سپس داده‌های جدید را Seed می‌کند
    /// برای MVP - امن و قابل تکرار
    /// ============================================
    /// رمزها و کلیدهای حساس از IConfiguration خوانده می‌شوند.
    /// در محیط Development: از appsettings.Development.json
    /// در محیط Production: از Environment Variables
    ///   مثال: SeedSecrets__AdminPassword=YourSecurePassword
    /// ============================================
    /// </summary>
    public class DataSeeder
    {
        private readonly NavigationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IDatabaseConnectionService _dbConnectionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(
            NavigationDbContext context,
            IPasswordHasher passwordHasher,
            IDatabaseConnectionService dbConnectionService,
            IConfiguration configuration,
            ILogger<DataSeeder> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _dbConnectionService = dbConnectionService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Seed تمام داده‌های اولیه سیستم
        /// </summary>
        public async Task SeedAllAsync()
        {
            _logger.LogInformation("========================================");
            _logger.LogInformation("Starting DataSeeder.SeedAllAsync...");
            _logger.LogInformation("========================================");

            try
            {
                // 0. پاک‌سازی داده‌های قدیمی منوها برای جلوگیری از تکرار
                await CleanupOldMenuDataAsync();

                // 1. Seed گروه‌ها
                await SeedGroupsAsync();

                // 2. Seed منوها
                await SeedMenusAsync();

                // 3. Seed Permission ها
                await SeedPermissionsAsync();

                // 4. Seed کاربران
                await SeedUsersAsync();

                // 5. Seed ارتباط کاربر-گروه
                await SeedUserGroupsAsync();

                // 6. Seed ارتباط گروه-Permission (همه Permission ها به Admin)
                await SeedGrpPermissionsAsync();

                // 7. Seed ارتباط منو-Permission
                await SeedMenuPermissionsAsync();

                // 8. Seed تنظیمات حفاظت از داده‌های ممیزی (FAU_STG.3.1, FAU_STG.4.1)
                await SeedAuditLogProtectionSettingsAsync();

                // 8.1. Seed تنظیمات کپچا (غیرفعال به صورت پیش‌فرض)
                await SeedCaptchaSettingsAsync();

                // 8.2. Seed تنظیمات MFA (فعال ولی غیراجباری)
                await SeedMfaSettingsAsync();

                // 9. Seed تنظیمات SMS
                await SeedShobeSmsSettingsAsync();

                // 10. Seed تنظیمات Attachment
                await SeedShobeAttachmentSettingsAsync();

                // 10.1. Seed تنظیمات خروجی داده‌ها (DataExport) در tblShobeSettings
                await SeedShobeDataExportSettingsAsync();

                // 11. Seed داده‌های اولیه راهبری (نرم‌افزار، دیتابیس‌ها، مشتری دمو)
                await SeedManagementDataAsync();

                _logger.LogInformation("========================================");
                _logger.LogInformation("SeedAllAsync completed successfully!");
                _logger.LogInformation("========================================");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SeedAllAsync: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Seed تنظیمات حفاظت از داده‌های ممیزی (FAU_STG.3.1, FAU_STG.4.1)
        /// </summary>
        private async Task SeedAuditLogProtectionSettingsAsync()
        {
            var now = DateTime.UtcNow;
            const string settingKey = "AuditLogProtection";

            var existing = await _context.SecuritySettings
                .FirstOrDefaultAsync(s => s.SettingKey == settingKey);

            if (existing != null)
            {
                _logger.LogInformation("AuditLogProtection settings already exist");
                return;
            }

            var defaultSettings = new AuditLogProtectionSettings
            {
                IsEnabled = true,
                MaxRetryAttempts = 3,
                EnableAlertOnFailure = true,
                AlertEmailAddresses = string.Empty,
                AlertSmsNumbers = string.Empty,
                RetentionDays = 365,
                ArchiveAfterDays = 90,
                BackupIntervalHours = 24,
                RetentionCheckIntervalHours = 24,
                FallbackRecoveryIntervalMinutes = 5,
                HealthCheckIntervalMinutes = 10,
                FallbackDirectory = "C://",
                BackupDirectory = "C://",
                ArchiveDirectory = "C://"
            };

            var settingJson = JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var newSetting = new SecuritySetting
            {
                SettingKey = settingKey,
                SettingName = "تنظیمات حفاظت از داده‌های ممیزی",
                Description = "تنظیمات مربوط به پشتیبان‌گیری، آرشیو و حفاظت از داده‌های ممیزی (FAU_STG.3.1, FAU_STG.4.1)",
                SettingValue = settingJson,
                SettingType = SecuritySettingType.AuditLogProtection,
                IsActive = true,
                IsEditable = true,
                DisplayOrder = 8,
                TblUserGrpIdInsert = 1
            };
            newSetting.SetZamanInsert(now);

            _context.SecuritySettings.Add(newSetting);
            await _context.SaveChangesAsync();

            _logger.LogInformation("AuditLogProtection settings seeded successfully");
        }

        /// <summary>
        /// Seed تنظیمات کپچا (غیرفعال به صورت پیش‌فرض)
        /// </summary>
        private async Task SeedCaptchaSettingsAsync()
        {
            var now = DateTime.UtcNow;
            const string settingKey = "Captcha";

            var existing = await _context.SecuritySettings
                .FirstOrDefaultAsync(s => s.SettingKey == settingKey);

            if (existing != null)
            {
                _logger.LogInformation("Captcha settings already exist");
                return;
            }

            var defaultSettings = new
            {
                IsEnabled = false, // کپچا غیرفعال به صورت پیش‌فرض
                CodeLength = 5,
                ExpiryMinutes = 2,
                NoiseLineCount = 10,
                NoiseDotCount = 50,
                ImageWidth = 130,
                ImageHeight = 40,
                RequireOnMfa = false
            };

            var settingJson = JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var newSetting = new SecuritySetting
            {
                SettingKey = settingKey,
                SettingName = "تنظیمات کپچا",
                Description = "تنظیمات مربوط به CAPTCHA برای احرازهویت - پیش‌فرض: غیرفعال",
                SettingValue = settingJson,
                SettingType = SecuritySettingType.Captcha,
                IsActive = true,
                IsEditable = true,
                DisplayOrder = 5,
                TblUserGrpIdInsert = 1
            };
            newSetting.SetZamanInsert(now);

            _context.SecuritySettings.Add(newSetting);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Captcha settings seeded successfully (disabled by default)");
        }

        /// <summary>
        /// Seed تنظیمات MFA (فعال ولی غیراجباری به صورت پیش‌فرض)
        /// </summary>
        private async Task SeedMfaSettingsAsync()
        {
            var now = DateTime.UtcNow;
            const string settingKey = "Mfa";

            var existing = await _context.SecuritySettings
                .FirstOrDefaultAsync(s => s.SettingKey == settingKey);

            if (existing != null)
            {
                _logger.LogInformation("MFA settings already exist");
                return;
            }

            var defaultSettings = new
            {
                IsEnabled = true,      // MFA فعال
                IsRequired = false,    // اما اجباری نیست
                OtpLength = 6,
                OtpExpirySeconds = 120,
                RecoveryCodesCount = 10,
                MaxFailedOtpAttempts = 3,
                LockoutDurationMinutes = 5
            };

            var settingJson = JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var newSetting = new SecuritySetting
            {
                SettingKey = settingKey,
                SettingName = "تنظیمات احراز هویت دو مرحله‌ای",
                Description = "تنظیمات MFA - پیش‌فرض: فعال ولی غیراجباری",
                SettingValue = settingJson,
                SettingType = SecuritySettingType.Mfa,
                IsActive = true,
                IsEditable = true,
                DisplayOrder = 6,
                TblUserGrpIdInsert = 1
            };
            newSetting.SetZamanInsert(now);

            _context.SecuritySettings.Add(newSetting);
            await _context.SaveChangesAsync();

            _logger.LogInformation("MFA settings seeded successfully (enabled but not required)");
        }

        /// <summary>
        /// پاک‌سازی داده‌های قدیمی منوها برای جلوگیری از تکرار
        /// </summary>
        private async Task CleanupOldMenuDataAsync()
        {
            var menuCount = await _context.tblMenus.CountAsync();
            //در صورت اضافه شدن منو باید این عدد هم اضافه شود
            // 64 + 1 (مدیریت مشتریان) + 12 (زیرمنوهای راهبری) = 77
            const int expectedMenuCount = 89;

            if (menuCount > 0 && menuCount != expectedMenuCount)
            {
                _logger.LogWarning("Detected {Count} menus (expected {Expected}). Cleaning up menus...",
                    menuCount, expectedMenuCount);

                // حذف همه رابطه‌های منو-Permission
                var menuPermissions = await _context.tblMenuPermissions.ToListAsync();
                if (menuPermissions.Any())
                {
                    _context.tblMenuPermissions.RemoveRange(menuPermissions);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Removed {Count} menu-permission relationships", menuPermissions.Count);
                }

                // حذف همه منوها
                var menus = await _context.tblMenus.ToListAsync();
                if (menus.Any())
                {
                    _context.tblMenus.RemoveRange(menus);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Removed {Count} old menus", menus.Count);
                }
            }
        }

        /// <summary>
        /// Seed گروه‌های پایه
        /// </summary>
        private async Task SeedGroupsAsync()
        {
            var now = DateTime.UtcNow;

            var groupsToSeed = new List<(int GrpCode, string Title, string Description, long? ParenetId)>
            {
                (1, "Admin", "گروه مدیران سیستم" , null ),
                (2, "Operator", "گروه اپراتورها" , 1),
                (3, "Customer", "گروه ‌مشتریان" , null),
                (4, "CustomerAdmin", "مدیر ‌مشتریان" , 3),
                (5, "CustomerUser", "کاربر ‌مشتریان" , 4),
            };

            foreach (var groupData in groupsToSeed)
            {
                var existing = await _context.tblGrps.FirstOrDefaultAsync(g => g.Title == groupData.Title);
                if (existing == null)
                {
                    var newGroup = new tblGrp
                    {
                        GrpCode = groupData.GrpCode,
                        Title = groupData.Title,
                        Description = groupData.Description,
                        IsActive = true,
                        TblUserGrpIdInsert = 1,
                        ParentId = groupData.ParenetId
                    };
                    newGroup.SetZamanInsert(now);
                    _context.tblGrps.Add(newGroup);
                    _logger.LogInformation("Adding group: {Title}", groupData.Title);
                }
                else
                {
                    if (!existing.IsActive)
                    {
                        existing.IsActive = true;
                        _logger.LogInformation("Activating group: {Title}", groupData.Title);
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Groups seeding completed. Total groups: {Count}", await _context.tblGrps.CountAsync());
        }

        /// <summary>
        /// Seed منوهای پایه - شامل تمام امکانات سیستم با آیکون‌ها
        /// Path ها با api/ شروع می‌شوند
        /// </summary>
        private async Task SeedMenusAsync()
        {
            var now = DateTime.UtcNow;

            // ============================================
            // منوهای اصلی (سطح اول) با آیکون
            // ============================================
            var mainMenusData = new List<(string Title, string Path, string Icon)>
            {
                #region منوهای راهبری سیستم 
                    ("راهبری سیستم", "SysApi", "layout-dashboard"),
                    //("مدیریت کاربران", "api/Users", "users", 39),
                    //("مدیریت گروه‌ها", "api/Grp", "square-chart-gantt", 39),
                    //("مدیریت منوها", "api/Menu", "layout-grid", 39),
                    //("مدیریت دسترسی‌ها", "api/Permission", "shield-check", 39),
                    //("مدیریت امنیت", "api/Security", "shield", 1),
                    //("لاگ‌های امنیتی", "api/AuditLog", "file-lock", 1),
                    //("احراز هویت", "api/Auth", "fingerprint-pattern", 1),
                    //("مدیریت نسخه", "api/Version", "git-branch-plus", 1),
                    //("مدیریت خروجی داده‌ها", "api/DataExport", "file-output",1),
                    //("مدیریت شعب", "api/Shobe", "building-2", 1),
                    #endregion
            };

            var mainMenuIds = new Dictionary<string, long>();
            var subMenuIds = new Dictionary<string, long>();
            foreach (var menuData in mainMenusData)
            {
                var existing = await _context.tblMenus
                    .FirstOrDefaultAsync(m => m.Path == menuData.Path || m.Title == menuData.Title);

                if (existing == null)
                {
                    var newMenu = new tblMenu
                    {
                        Title = menuData.Title,
                        Path = menuData.Path,
                        Icon = menuData.Icon,
                        IsMenu = true,
                        ParentId = null,
                        TblUserGrpIdInsert = 1
                    };
                    newMenu.SetZamanInsert(now);
                    _context.tblMenus.Add(newMenu);
                    await _context.SaveChangesAsync();
                    mainMenuIds[menuData.Path] = newMenu.Id;
                    _logger.LogInformation("Adding main menu: {Title} ({Path}) [{Icon}]", menuData.Title, menuData.Path, menuData.Icon);
                }
                else
                {
                    if (existing.Path != menuData.Path) existing.Path = menuData.Path;
                    if (existing.Icon != menuData.Icon) existing.Icon = menuData.Icon;
                    await _context.SaveChangesAsync();
                    mainMenuIds[menuData.Path] = existing.Id;
                }
            }

            // ============================================
            // زیرمنوها سطح 1با آیکون
            // ============================================
            var subMenusData1 = new List<(string Title, string Path, string Icon, string ParentPath, bool IsMenu)>
            {
                ("مدیریت کاربران", "api/Users", "users", "SysApi",true),
                ("مدیریت گروه‌ها", "api/Grp", "square-chart-gantt", "SysApi",true),
                ("مدیریت منوها", "api/Menu", "layout-grid", "SysApi",true),
                ("مدیریت دسترسی‌ها", "api/Permission", "shield-check", "SysApi",true),
                ("مدیریت امنیت", "api/Security", "shield", "SysApi",true),
                ("لاگ‌های امنیتی", "api/AuditLog", "file-lock","SysApi",true),
                ("احراز هویت", "api/Auth", "fingerprint-pattern", "SysApi",false),
                ("مدیریت نسخه", "api/Version", "git-branch-plus", "SysApi",true),
                ("مدیریت خروجی داده‌ها", "api/DataExport", "file-output","SysApi",true),
                ("مدیریت شعب", "api/Shobe", "building-2", "SysApi",true),
                ("مدیریت مشتریان", "api/Management", "users-group", "SysApi",true),
            };

            foreach (var menuData in subMenusData1)
            {
                var existing = await _context.tblMenus
                    .FirstOrDefaultAsync(m => m.Path == menuData.Path || m.Title == menuData.Title);

                if (existing == null)
                {
                    if (mainMenuIds.TryGetValue(menuData.ParentPath, out var parentId))
                    {
                        var newMenu = new tblMenu
                        {
                            Title = menuData.Title,
                            Path = menuData.Path,
                            Icon = menuData.Icon,
                            IsMenu = menuData.IsMenu,
                            ParentId = parentId,
                            TblUserGrpIdInsert = 1
                        };
                        newMenu.SetZamanInsert(now);
                        _context.tblMenus.Add(newMenu);
                        await _context.SaveChangesAsync();
                        subMenuIds[menuData.Path] = newMenu.Id;
                        _logger.LogInformation("Adding sub-menu: {Title} ({Path}) [{Icon}]", menuData.Title, menuData.Path, menuData.Icon);
                    }
                    else
                    {
                        _logger.LogWarning("Parent menu not found for: {Title} (Parent: {ParentPath})", menuData.Title, menuData.ParentPath);
                    }
                }
                else
                {
                    if (existing.Path != menuData.Path) existing.Path = menuData.Path;
                    if (existing.Icon != menuData.Icon) existing.Icon = menuData.Icon;
                    await _context.SaveChangesAsync();
                    subMenuIds[menuData.Path] = existing.Id;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Menus seeding completed. Total menus: {Count}", await _context.tblMenus.CountAsync());

            // ============================================
            // زیرمنوها سطح 2با آیکون
            // ============================================
            var subMenusData2 = new List<(string Title, string Path, string Icon, string ParentPath, bool IsMenu)>
            {
                // زیرمنوهای کاربران
                ("لیست کاربران", "api/Users/List", "user-search", "api/Users", true),
                ("اطلاعات کاربر", "api/Users/GetById", "user-info", "api/Users" , false),
                ("تعریف کاربر جدید", "api/Users/Create", "user-plus", "api/Users" , false),
                ("فعال/غیرفعال کردن کاربر", "api/Users/Activate", "user-check", "api/Users", false),
                ("ریست رمز عبور", "api/Users/ResetPassword", "key-round", "api/Users", false),
                ("باز کردن قفل کاربر", "api/Users/Unlock", "lock-open", "api/Users", false),
                ("وضعیت قفل کاربر", "api/Users/LockoutStatus", "message-square-lock", "api/Users", false),
                ("خروجی لیست کاربران", "api/Users/Export", "file-output", "api/Users", false),

                // زیرمنوهای گروه‌ها
                ("لیست گروه‌ها", "api/Grp/List", "list-ordered", "api/Grp", true),
                ("تعریف گروه جدید", "api/Grp/Create", "folder-plus", "api/Grp", false),
                ("ویرایش گروه", "api/Grp/Edit", "file-pen-line", "api/Grp", false),

                // زیرمنوهای منوها
                ("لیست منوها", "api/Menu/List", "scroll-text", "api/Menu", true),
                ("تعریف منوی جدید", "api/Menu/Create", "square-pen", "api/Menu", false),
                ("ویرایش منو", "api/Menu/Edit", "file-sliders", "api/Menu", false),
                ("درخت منو", "api/Menu/Tree", "git-branch", "api/Menu", false),

                // زیرمنوهای دسترسی‌ها
                ("لیست دسترسی‌ها", "api/Permission/List", "list-ordered", "api/Permission", false),
                ("تخصیص دسترسی به گروه", "api/Permission/Assign", "shield-plus", "api/Permission", true),

                // زیرمنوهای امنیتی
                ("تنظیمات امنیتی", "api/Security/Settings", "shield-check", "api/Security", true),
                ("سیاست رمز عبور", "api/Security/PasswordPolicy", "siren", "api/Security", false),
                ("تنظیمات قفل حساب", "api/Security/LockoutPolicy", "wrench", "api/Security", false),
                ("وضعیت امنیتی کاربران", "api/Security/UsersSecurityStatus", "shield-user", "api/Security", false),
                ("بررسی سلامت امنیتی", "api/Security/HealthCheck", "shield-alert", "api/Security", false),
                ("اطلاعات محیط", "api/Security/EnvironmentInfo", "server", "api/Security", false),
                ("بستن نشست‌های کاربر", "api/Security/TerminateSessions", "log-out", "api/Security", false),
                ("مدیریت کلیدهای رمزنگاری", "api/Security/Keys", "key-round", "api/Security", false),
                ("احراز هویت دو عاملی", "api/Security/MFA", "fingerprint-pattern", "api/Security", false),

                // زیرمنوهای لاگ
                ("مشاهده لاگ‌ها", "api/AuditLog/List", "file-text", "api/AuditLog", true),
                ("جستجوی لاگ", "api/AuditLog/Search", "file-search", "api/AuditLog", false),
                ("لاگ‌های امروز", "api/AuditLog/Today", "calendar-search", "api/AuditLog", false),
                ("ورودهای ناموفق", "api/AuditLog/FailedLogins", "circle-x", "api/AuditLog", false),
                ("آمار امنیتی", "api/AuditLog/Statistics", "chart-bar-stacked", "api/AuditLog", false),
                ("انواع رویدادها", "api/AuditLog/EventTypes", "activity", "api/AuditLog", false),
                ("لاگ‌های کاربر", "api/AuditLog/User", "scroll-text", "api/AuditLog", false),

                // زیرمنوهای احراز هویت
                ("ورود", "api/Auth/Login", "log-in", "api/Auth", false),
                ("خروج", "api/Auth/Logout", "log-out", "api/Auth", false),
                ("خروج از همه دستگاه‌ها", "api/Auth/LogoutAll", "phone-off", "api/Auth", false),
                ("تغییر رمز عبور", "api/Auth/ChangePassword", "rotate-ccw-key", "api/Auth", false),
                ("سیاست رمز عبور", "api/Auth/PasswordPolicy", "siren", "api/Auth", false),

                // زیرمنوهای مدیریت نسخه (FPT_TUD_EXT.1.2 و FPT_TUD_EXT.1.3)
                ("نسخه بک‌اند", "api/Version/current", "server", "api/Version", false),
                ("بررسی نسخه بک‌اند", "api/Version/check", "refresh-cw", "api/Version", false),
                ("اطلاعات سیستم", "api/Version/info", "info", "api/Version", true),
                ("نسخه فرانت‌اند", "api/Version/frontend/current", "monitor", "api/Version", false),
                ("بررسی نسخه فرانت‌اند", "api/Version/frontend/check", "monitor-check", "api/Version", false),

                // زیرمنوهای مدیریت خروجی داده‌ها (FDP_ETC.2.1, FDP_ETC.2.2, FDP_ETC.2.4)
                ("تنظیمات خروجی", "api/DataExport/Settings", "settings", "api/DataExport",true),
                ("قوانین خروجی", "api/DataExport/Rules", "list-checks", "api/DataExport", false),
                ("ماسک داده‌ها", "api/DataExport/Masking", "eye-off", "api/DataExport", false),
                ("سطوح حساسیت", "api/DataExport/SensitivityLevels", "shield-alert", "api/DataExport", false),
                ("لاگ خروجی داده‌ها", "api/DataExport/AuditLog", "file-clock", "api/DataExport", false),

                // زیرمنوهای مدیریت شعب
                ("لیست شعب", "api/Shobe/List", "list-ordered", "api/Shobe",true),
                ("تعریف شعبه جدید", "api/Shobe/Create", "building-2-plus", "api/Shobe", false),
                ("ویرایش شعبه", "api/Shobe/Edit", "building-2-pen", "api/Shobe", false),
                ("حذف شعبه", "api/Shobe/Delete", "trash-2", "api/Shobe", false),
                ("تنظیمات شعبه", "api/Shobe/Settings", "settings", "api/Shobe", false),

                // زیرمنوهای مدیریت مشتریان (راهبری سیستم)
                ("داشبورد راهبری", "api/Management/dashboard", "layout-dashboard", "api/Management", true),
                ("لیست نرم‌افزارها", "api/Management/softwares", "package", "api/Management", true),
                ("تعریف نرم‌افزار جدید", "api/Management/softwares/create", "package-plus", "api/Management", false),
                ("لیست پلن‌ها", "api/Management/plans", "credit-card", "api/Management", true),
                ("تعریف پلن جدید", "api/Management/plans/create", "credit-card-plus", "api/Management", false),
                ("لیست مشتریان", "api/Management/customers", "users", "api/Management", true),
                ("تعریف مشتری جدید", "api/Management/customers/create", "user-plus", "api/Management", false),
                ("اشتراک‌های مشتریان", "api/Management/subscriptions", "file-check", "api/Management", true),
                ("تعریف اشتراک جدید", "api/Management/subscriptions/create", "file-plus", "api/Management", false),
                ("لیست دیتابیس‌ها", "api/Management/databases", "database", "api/Management", true),
                ("تعریف دیتابیس جدید", "api/Management/databases/create", "database-plus", "api/Management", false),
                ("تست اتصال دیتابیس", "api/Management/databases/test-connection", "database-zap", "api/Management", false),
            };

            foreach (var menuData in subMenusData2)
            {
                var existing = await _context.tblMenus
                    .FirstOrDefaultAsync(m => m.Path == menuData.Path || m.Title == menuData.Title);

                if (existing == null)
                {
                    if (subMenuIds.TryGetValue(menuData.ParentPath, out var parentId))
                    {
                        var newMenu = new tblMenu
                        {
                            Title = menuData.Title,
                            Path = menuData.Path,
                            Icon = menuData.Icon,
                            IsMenu = menuData.IsMenu,
                            ParentId = parentId,
                            TblUserGrpIdInsert = 1
                        };
                        newMenu.SetZamanInsert(now);
                        _context.tblMenus.Add(newMenu);
                        _logger.LogInformation("Adding sub-menu: {Title} ({Path}) [{Icon}]", menuData.Title, menuData.Path, menuData.Icon);
                    }
                    else
                    {
                        _logger.LogWarning("Parent menu not found for: {Title} (Parent: {ParentPath})", menuData.Title, menuData.ParentPath);
                    }
                }
                else
                {
                    if (existing.Path != menuData.Path) existing.Path = menuData.Path;
                    if (existing.Icon != menuData.Icon) existing.Icon = menuData.Icon;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Menus seeding completed. Total menus: {Count}", await _context.tblMenus.CountAsync());
        }

        /// <summary>
        /// Seed Permission های اولیه
        /// </summary>
        private async Task SeedPermissionsAsync()
        {
            var now = DateTime.UtcNow;

            var permissionsData = new List<(string Name, string Resource, string Action, string Description, int DisplayOrder)>
            {
                // Dashboard
                ("Dashboard.Read", "Dashboard", "Read", "مشاهده داشبورد", 1),

                // Users
                ("Users.Menu", "Users", "Menu", "دسترسی به منوی مدیریت کاربران", 8),
                ("Users.Read", "Users", "Read", "مشاهده لیست همه کاربران", 9),
                ("Users.ReadById", "Users", "Read", "اطلاعات یک کاربر", 10),
                ("Users.Create", "Users", "Create", "ایجاد کاربر جدید", 11),
                ("Users.Update", "Users", "Update", "ویرایش کاربران", 12),
                ("Users.Delete", "Users", "Delete", "حذف کاربران", 13),
                ("Users.Activate", "Users", "Activate", "فعال/غیرفعال کردن کاربران", 14),
                ("Users.ResetPassword", "Users", "ResetPassword", "ریست رمز عبور کاربران", 15),
                ("Users.Unlock", "Users", "Unlock", "باز کردن قفل کاربران", 16),
                ("Users.LockoutStatus", "Users", "LockoutStatus", "مشاهده وضعیت قفل کاربران", 17),
                ("Users.Export", "Users", "Export", "خروجی لیست کاربران (FDP_ETC.2)", 18),
                ("Users.UploadProfilePicture", "Users", "UploadProfilePicture", "آپلود تصویر پروفایل", 25),
                ("Users.DeleteProfilePicture", "Users", "DeleteProfilePicture", "حذف تصویر پروفایل", 26),

                // Groups
                ("Groups.Menu", "Groups", "Menu", "دسترسی به منوی مدیریت گروه‌ها", 19),
                ("Groups.Read", "Groups", "Read", "مشاهده لیست گروه‌ها", 20),
                ("Groups.Create", "Groups", "Create", "ایجاد گروه جدید", 21),
                ("Groups.Update", "Groups", "Update", "ویرایش گروه‌ها", 22),
                ("Groups.Delete", "Groups", "Delete", "حذف گروه‌ها", 23),
                ("Groups.ManagePermissions", "Groups", "ManagePermissions", "مدیریت دسترسی‌های گروه", 24),

                // Menus
                ("Menus.Menu", "Menus", "Menu", "دسترسی به منوی مدیریت منوها", 29),
                ("Menus.Read", "Menus", "Read", "مشاهده منوها", 30),
                ("Menus.Create", "Menus", "Create", "ایجاد منوی جدید", 31),
                ("Menus.Update", "Menus", "Update", "ویرایش منوها", 32),
                ("Menus.Delete", "Menus", "Delete", "حذف منوها", 33),

                // Permissions
                ("Permissions.Menu", "Permissions", "Menu", "دسترسی به منوی مدیریت دسترسی‌ها", 39),
                ("Permissions.Read", "Permissions", "Read", "مشاهده لیست دسترسی‌ها", 40),
                ("Permissions.Manage", "Permissions", "Manage", "مدیریت دسترسی‌ها", 41),

                // AuditLog
                ("AuditLog.Menu", "AuditLog", "Menu", "دسترسی به منوی لاگ‌های امنیتی", 49),
                ("AuditLog.Read", "AuditLog", "Read", "مشاهده لاگ‌های امنیتی", 50),
                ("AuditLog.Search", "AuditLog", "Search", "جستجو در لاگ‌ها", 51),
                ("AuditLog.Export", "AuditLog", "Export", "خروجی گرفتن از لاگ‌ها", 52),
                ("AuditLog.Statistics", "AuditLog", "Statistics", "مشاهده آمار امنیتی", 53),
                ("AuditLog.Today", "AuditLog", "Today", "مشاهده لاگ‌های امروز", 54),
                ("AuditLog.FailedLogins", "AuditLog", "FailedLogins", "مشاهده ورودهای ناموفق", 55),
                ("AuditLog.EventTypes", "AuditLog", "EventTypes", "مشاهده انواع رویدادها", 56),
                ("AuditLog.UserLogs", "AuditLog", "UserLogs", "مشاهده لاگ‌های کاربر", 57),
                ("AuditLog.Admin", "AuditLog", "Admin", "مدیریت کامل سیاست‌های حفاظت لاگ", 58),

                // Security
                ("Security.Menu", "Security", "Menu", "دسترسی به منوی مدیریت امنیت", 59),
                ("Security.Read", "Security", "Read", "مشاهده تنظیمات امنیتی", 60),
                ("Security.Manage", "Security", "Manage", "مدیریت تنظیمات امنیتی", 61),
                ("Security.PasswordPolicy", "Security", "PasswordPolicy", "مدیریت سیاست رمز عبور", 62),
                ("Security.LockoutPolicy", "Security", "LockoutPolicy", "مدیریت سیاست قفل حساب", 63),
                ("Security.TerminateSessions", "Security", "TerminateSessions", "بستن نشست‌های کاربران", 64),
                ("Security.Write", "Security", "Write", "ویرایش تنظیمات امنیتی", 65),
                ("Security.Delete", "Security", "Delete", "حذف تنظیمات امنیتی", 66),
                ("Security.UsersStatus", "Security", "UsersStatus", "مشاهده وضعیت امنیتی کاربران", 67),
                ("Security.HealthCheck", "Security", "HealthCheck", "بررسی سلامت امنیتی", 68),
                ("Security.EnvironmentInfo", "Security", "EnvironmentInfo", "مشاهده اطلاعات محیط", 69),

                // Security - Key Management (FCS_CKM)
                ("Security.KeyRead", "Security", "KeyRead", "مشاهده کلیدهای رمزنگاری", 70),
                ("Security.KeyCreate", "Security", "KeyCreate", "ایجاد کلید رمزنگاری جدید", 71),
                ("Security.KeyRotate", "Security", "KeyRotate", "چرخش کلیدهای رمزنگاری", 72),
                ("Security.KeyDestroy", "Security", "KeyDestroy", "تخریب کلیدهای رمزنگاری", 73),
                ("Security.Admin", "Security", "Admin", "مدیریت کامل تنظیمات امنیتی", 74),
                ("Security.DataIntegrity", "Security", "DataIntegrity", "مدیریت کنترل‌های صحت داده", 75),

                // Sessions
                ("Sessions.Read", "Sessions", "Read", "مشاهده نشست‌های خود", 80),
                ("Sessions.ReadAll", "Sessions", "ReadAll", "مشاهده نشست‌های همه کاربران", 81),
                ("Sessions.Revoke", "Sessions", "Revoke", "بستن نشست‌های خود", 82),
                ("Sessions.RevokeAll", "Sessions", "RevokeAll", "بستن نشست‌های همه کاربران", 83),

                // MFA (FIA_UAU.5)
                ("MFA.Read", "MFA", "Read", "مشاهده وضعیت MFA", 90),
                ("MFA.Manage", "MFA", "Manage", "مدیریت تنظیمات MFA", 91),
                ("MFA.Reset", "MFA", "Reset", "ریست MFA کاربران", 92),

                // Auth
                ("Auth.Login", "Auth", "Login", "ورود به سیستم", 100),
                ("Auth.Logout", "Auth", "Logout", "خروج از سیستم", 101),
                ("Auth.LogoutAll", "Auth", "LogoutAll", "خروج از همه دستگاه‌ها", 102),
                ("Auth.ChangePassword", "Auth", "ChangePassword", "تغییر رمز عبور", 103),

                // System Version (FPT_TUD_EXT.1.2 و FPT_TUD_EXT.1.3)
                ("System.Version.Menu", "System", "Version.Menu", "دسترسی به منوی مدیریت نسخه", 109),
                ("System.Version.Read", "System", "Version.Read", "مشاهده نسخه سیستم", 110),
                ("System.Version.BackendCheck", "System", "Version.BackendCheck", "بررسی نسخه بک‌اند", 111),
                ("System.Version.FrontendCheck", "System", "Version.FrontendCheck", "بررسی نسخه فرانت‌اند", 112),
                ("System.Version.Info", "System", "Version.Info", "مشاهده اطلاعات سیستم", 113),
                ("System.Version.VerifySignature", "System", "Version.VerifySignature", "اعتبارسنجی امضای دیجیتال به‌روزرسانی (FPT_TUD_EXT.1.3)", 114),

                // Data Export - خروجی داده‌ها (FDP_ETC.2.1, FDP_ETC.2.2, FDP_ETC.2.4)
                ("DataExport.Read", "DataExport", "Read", "مشاهده تنظیمات خروجی داده‌ها", 120),
                ("DataExport.Settings", "DataExport", "Settings", "مدیریت تنظیمات خروجی", 121),
                ("DataExport.Rules", "DataExport", "Rules", "مدیریت قوانین خروجی داده‌ها", 122),
                ("DataExport.Masking", "DataExport", "Masking", "مدیریت ماسک داده‌ها", 123),
                ("DataExport.Audit", "DataExport", "Audit", "مشاهده لاگ خروجی داده‌ها", 124),
                ("DataExport.SensitivityLevel", "DataExport", "SensitivityLevel", "مدیریت سطوح حساسیت داده", 125),
                ("DataExport.Admin", "DataExport", "Admin", "مدیریت کامل خروجی داده‌ها (FDP_ETC.2)", 126),

                // Shobes (شعب)
                ("Shobes.Menu", "Shobes", "Menu", "دسترسی به منوی مدیریت شعب", 129),
                ("Shobes.Read", "Shobes", "Read", "مشاهده لیست شعب", 130),
                ("Shobes.Create", "Shobes", "Create", "ایجاد شعبه جدید", 131),
                ("Shobes.Update", "Shobes", "Update", "ویرایش شعب", 132),
                ("Shobes.Delete", "Shobes", "Delete", "حذف شعب", 133),
                ("Shobes.Settings", "Shobes", "Settings", "مدیریت تنظیمات شعبه", 134),

                // Management - راهبری سیستم (مشتریان، نرم‌افزارها، پلن‌ها، دیتابیس‌ها)
                ("Management.Dashboard.Read", "Management", "Dashboard.Read", "مشاهده داشبورد راهبری", 140),

                // Software - نرم‌افزارها
                ("Management.Software.Read", "Management", "Software.Read", "مشاهده لیست نرم‌افزارها", 141),
                ("Management.Software.Create", "Management", "Software.Create", "ایجاد نرم‌افزار جدید", 142),
                ("Management.Software.Update", "Management", "Software.Update", "ویرایش نرم‌افزار", 143),
                ("Management.Software.Delete", "Management", "Software.Delete", "حذف نرم‌افزار", 144),

                // Plan - پلن‌ها
                ("Management.Plan.Read", "Management", "Plan.Read", "مشاهده لیست پلن‌ها", 145),
                ("Management.Plan.Create", "Management", "Plan.Create", "ایجاد پلن جدید", 146),
                ("Management.Plan.Update", "Management", "Plan.Update", "ویرایش پلن", 147),
                ("Management.Plan.Delete", "Management", "Plan.Delete", "حذف پلن", 148),

                // Customer - مشتریان
                ("Management.Customer.Read", "Management", "Customer.Read", "مشاهده لیست مشتریان", 150),
                ("Management.Customer.Create", "Management", "Customer.Create", "ایجاد مشتری جدید", 151),
                ("Management.Customer.Update", "Management", "Customer.Update", "ویرایش مشتری", 152),
                ("Management.Customer.Delete", "Management", "Customer.Delete", "حذف مشتری", 153),

                // Subscription - اشتراک‌ها
                ("Management.Subscription.Read", "Management", "Subscription.Read", "مشاهده اشتراک‌های مشتریان", 155),
                ("Management.Subscription.Create", "Management", "Subscription.Create", "ایجاد اشتراک جدید", 156),
                ("Management.Subscription.Update", "Management", "Subscription.Update", "ویرایش اشتراک", 157),
                ("Management.Subscription.Delete", "Management", "Subscription.Delete", "حذف اشتراک", 158),

                // Database - دیتابیس‌ها
                ("Management.Database.Read", "Management", "Database.Read", "مشاهده لیست دیتابیس‌ها", 160),
                ("Management.Database.Create", "Management", "Database.Create", "ایجاد دیتابیس جدید", 161),
                ("Management.Database.Update", "Management", "Database.Update", "ویرایش دیتابیس", 162),
                ("Management.Database.Delete", "Management", "Database.Delete", "حذف دیتابیس", 163),
            };

            foreach (var permData in permissionsData)
            {
                var existing = await _context.tblPermissions.FirstOrDefaultAsync(p => p.Name == permData.Name);
                if (existing == null)
                {
                    var newPermission = new tblPermission
                    {
                        Name = permData.Name,
                        Resource = permData.Resource,
                        Action = permData.Action,
                        Description = permData.Description,
                        DisplayOrder = permData.DisplayOrder,
                        IsActive = true,
                        TblUserGrpIdInsert = 1
                    };
                    newPermission.SetZamanInsert(now);
                    _context.tblPermissions.Add(newPermission);
                    _logger.LogInformation("Adding permission: {Name}", permData.Name);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Permissions seeding completed. Total permissions: {Count}", await _context.tblPermissions.CountAsync());
        }

        /// <summary>
        /// Seed کاربر Admin
        /// </summary>
        private async Task SeedUsersAsync()
        {
            var existing = await _context.tblUsers.FirstOrDefaultAsync(u => u.UserName == "admin");
            if (existing != null)
            {
                _logger.LogInformation("Admin user already exists with Id={Id}", existing.Id);
                return;
            }

            var now = DateTime.UtcNow;
            var adminPassword = _configuration["SeedSecrets:AdminPassword"];
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                _logger.LogError("SeedSecrets:AdminPassword is not configured. Cannot seed admin user.");
                throw new InvalidOperationException(
                    "رمز عبور ادمین تنظیم نشده است. لطفاً SeedSecrets:AdminPassword را در appsettings یا Environment Variable تنظیم کنید.");
            }

            var adminUser = new tblUser
            {
                UserName = "admin",
                Password = _passwordHasher.HashPassword(adminPassword),
                FirstName = "مدیر",
                LastName = "سیستم",
                Email = "info@bnpco.ir",
                Phone = "09129052758",
                MobileNumber = "09134236323",
                IsActive = true,
                IpAddress = "127.0.0.1",
                TblUserGrpIdInsert = 1
            };
            adminUser.SetZamanInsert(now);
            adminUser.SetPasswordLastChangedAt(now);

            _context.tblUsers.Add(adminUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin user created with Id={Id}", adminUser.Id);
        }

        /// <summary>
        /// Seed ارتباط کاربر با گروه
        /// </summary>
        private async Task SeedUserGroupsAsync()
        {
            var adminUser = await _context.tblUsers.FirstOrDefaultAsync(u => u.UserName == "admin");
            var adminGroup = await _context.tblGrps.FirstOrDefaultAsync(g => g.Title == "Admin");

            if (adminUser == null || adminGroup == null)
            {
                _logger.LogWarning("Admin user or group not found for UserGroups seeding");
                return;
            }

            var existing = await _context.tblUserGrps
                .FirstOrDefaultAsync(ug => ug.tblUserId == adminUser.Id && ug.tblGrpId == adminGroup.Id);

            if (existing != null)
            {
                _logger.LogInformation("Admin user already linked to Admin group");
                return;
            }

            var now = DateTime.UtcNow;
            var userGrp = new tblUserGrp
            {
                tblUserId = adminUser.Id,
                tblGrpId = adminGroup.Id,
                TblUserGrpIdInsert = 1
            };
            userGrp.SetZamanInsert(now);

            _context.tblUserGrps.Add(userGrp);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin user (Id={UserId}) linked to Admin group (Id={GroupId})",
                adminUser.Id, adminGroup.Id);
        }

        /// <summary>
        /// نام‌های Permission های پایه احراز هویت و نشست‌ها
        /// این دسترسی‌ها باید به همه گروه‌ها اعطا شوند
        /// </summary>
        private static readonly string[] BaseAuthPermissionNames = new[]
        {
            "Auth.Login",
            "Auth.Logout",
            "Auth.LogoutAll",
            "Auth.ChangePassword",
            "Sessions.Read",
            "Sessions.Revoke",
            "Users.ReadById",
            "Users.UploadProfilePicture",
            "Users.DeleteProfilePicture"
        };

        /// <summary>
        /// Seed ارتباط گروه Admin با تمام Permission ها
        /// و اعطای دسترسی‌های پایه احراز هویت و Sessions.Read به همه گروه‌ها
        /// </summary>
        private async Task SeedGrpPermissionsAsync()
        {
            var allPermissions = await _context.tblPermissions.ToListAsync();
            if (!allPermissions.Any())
            {
                _logger.LogWarning("No permissions found for GrpPermissions seeding");
                return;
            }

            var now = DateTime.UtcNow;

            // ============================================
            // 1. اعطای تمام Permission ها به گروه Admin
            // ============================================
            var adminGroup = await _context.tblGrps.FirstOrDefaultAsync(g => g.Title == "Admin");
            if (adminGroup == null)
            {
                _logger.LogWarning("Admin group not found for GrpPermissions seeding");
            }
            else
            {
                var existingAdminPermissionIds = await _context.tblGrpPermissions
                    .Where(gp => gp.tblGrpId == adminGroup.Id)
                    .Select(gp => gp.tblPermissionId)
                    .ToListAsync();

                var adminAddedCount = 0;
                foreach (var permission in allPermissions)
                {
                    if (!existingAdminPermissionIds.Contains(permission.Id))
                    {
                        var grpPermission = new tblGrpPermission
                        {
                            tblGrpId = adminGroup.Id,
                            tblPermissionId = permission.Id,
                            IsGranted = true,
                            GrantedAt = now,
                            GrantedBy = 1,
                            TblUserGrpIdInsert = 1
                        };
                        grpPermission.SetZamanInsert(now);
                        _context.tblGrpPermissions.Add(grpPermission);
                        adminAddedCount++;
                    }
                }

                if (adminAddedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Added {AddedCount} new permissions to Admin group. Total: {Total}",
                        adminAddedCount, existingAdminPermissionIds.Count + adminAddedCount);
                }
                else
                {
                    _logger.LogInformation("Admin group already has all {Count} permissions", allPermissions.Count);
                }
            }

            // ============================================
            // 2. اعطای دسترسی‌های پایه احراز هویت و Sessions.Read به همه گروه‌ها
            // ============================================
            var baseAuthPermissions = allPermissions
                .Where(p => BaseAuthPermissionNames.Contains(p.Name))
                .ToList();

            if (!baseAuthPermissions.Any())
            {
                _logger.LogWarning("No base auth permissions found for assigning to all groups");
                return;
            }

            var allGroups = await _context.tblGrps.ToListAsync();
            var totalBaseAdded = 0;

            foreach (var group in allGroups)
            {
                // گروه Admin قبلاً همه دسترسی‌ها را دارد، رد شود
                if (adminGroup != null && group.Id == adminGroup.Id)
                    continue;

                var existingPermissionIds = await _context.tblGrpPermissions
                    .Where(gp => gp.tblGrpId == group.Id)
                    .Select(gp => gp.tblPermissionId)
                    .ToListAsync();

                foreach (var permission in baseAuthPermissions)
                {
                    if (!existingPermissionIds.Contains(permission.Id))
                    {
                        var grpPermission = new tblGrpPermission
                        {
                            tblGrpId = group.Id,
                            tblPermissionId = permission.Id,
                            IsGranted = true,
                            GrantedAt = now,
                            GrantedBy = 1,
                            TblUserGrpIdInsert = 1,
                            Notes = "دسترسی پایه احراز هویت - اعطای خودکار"
                        };
                        grpPermission.SetZamanInsert(now);
                        _context.tblGrpPermissions.Add(grpPermission);
                        totalBaseAdded++;
                    }
                }
            }

            if (totalBaseAdded > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Added {Count} base auth permissions to non-Admin groups", totalBaseAdded);
            }
            else
            {
                _logger.LogInformation("All groups already have base auth permissions");
            }
        }

        /// <summary>
        /// Seed ارتباط منوها با Permission ها
        /// </summary>
        private async Task SeedMenuPermissionsAsync()
        {
            var menus = await _context.tblMenus.ToListAsync();
            var permissions = await _context.tblPermissions.ToListAsync();

            if (!menus.Any() || !permissions.Any())
            {
                _logger.LogWarning("No menus or permissions found for MenuPermissions seeding");
                return;
            }

            var existingRelations = await _context.tblMenuPermissions.ToListAsync();
            var now = DateTime.UtcNow;
            var addedCount = 0;
            var defaultPermission = permissions.FirstOrDefault(p => p.Name == "Dashboard.Read");

            foreach (var menu in menus)
            {
                var existingPermission = existingRelations.FirstOrDefault(mp => mp.tblMenuId == menu.Id);
                if (existingPermission != null) continue;

                var matchedPermission = FindMatchingPermission(menu, permissions) ?? defaultPermission;

                if (matchedPermission == null)
                {
                    _logger.LogWarning("No permission found for menu: {Title}", menu.Title);
                    continue;
                }

                var menuPermission = new tblMenuPermission
                {
                    tblMenuId = menu.Id,
                    tblPermissionId = matchedPermission.Id,
                    IsRequired = true,
                    TblUserGrpIdInsert = 1
                };
                menuPermission.SetZamanInsert(now);
                _context.tblMenuPermissions.Add(menuPermission);
                addedCount++;
            }

            if (addedCount > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Added {AddedCount} menu-permission relationships", addedCount);
            }
        }

        /// <summary>
        /// پیدا کردن Permission مناسب برای هر منو بر اساس Path
        /// </summary>
        private tblPermission? FindMatchingPermission(tblMenu menu, List<tblPermission> permissions)
        {
            var path = (menu.Path ?? "").ToLower();

            // Dashboard
            if (path.Contains("dashboard")) return permissions.FirstOrDefault(p => p.Name == "Dashboard.Read");

            // Auth
            if (path.Contains("auth/login")) return permissions.FirstOrDefault(p => p.Name == "Auth.Login");
            if (path.Contains("auth/logout") && path.Contains("all")) return permissions.FirstOrDefault(p => p.Name == "Auth.LogoutAll");
            if (path.Contains("auth/logout")) return permissions.FirstOrDefault(p => p.Name == "Auth.Logout");
            if (path.Contains("auth/changepassword")) return permissions.FirstOrDefault(p => p.Name == "Auth.ChangePassword");
            if (path.Contains("auth/passwordpolicy")) return permissions.FirstOrDefault(p => p.Name == "Security.PasswordPolicy");
            if (path.Contains("auth")) return permissions.FirstOrDefault(p => p.Name == "Auth.Login");

            // Security - منوی والد قبل از زیرمنوها
            if (path == "api/security") return permissions.FirstOrDefault(p => p.Name == "Security.Menu");
            if (path.Contains("security/passwordpolicy")) return permissions.FirstOrDefault(p => p.Name == "Security.PasswordPolicy");
            if (path.Contains("security/lockoutpolicy")) return permissions.FirstOrDefault(p => p.Name == "Security.LockoutPolicy");
            if (path.Contains("security/terminatesessions")) return permissions.FirstOrDefault(p => p.Name == "Security.TerminateSessions");
            if (path.Contains("security/userssecuritystatus")) return permissions.FirstOrDefault(p => p.Name == "Security.UsersStatus");
            if (path.Contains("security/healthcheck")) return permissions.FirstOrDefault(p => p.Name == "Security.HealthCheck");
            if (path.Contains("security/environmentinfo")) return permissions.FirstOrDefault(p => p.Name == "Security.EnvironmentInfo");
            if (path.Contains("security/keys")) return permissions.FirstOrDefault(p => p.Name == "Security.KeyRead");
            if (path.Contains("security/mfa")) return permissions.FirstOrDefault(p => p.Name == "MFA.Read");
            if (path.Contains("security/settings")) return permissions.FirstOrDefault(p => p.Name == "Security.Read");
            if (path.Contains("security")) return permissions.FirstOrDefault(p => p.Name == "Security.Read");

            // AuditLog - منوی والد قبل از زیرمنوها
            if (path == "api/auditlog") return permissions.FirstOrDefault(p => p.Name == "AuditLog.Menu");
            if (path.Contains("auditlog/search")) return permissions.FirstOrDefault(p => p.Name == "AuditLog.Search");
            if (path.Contains("auditlog/statistics")) return permissions.FirstOrDefault(p => p.Name == "AuditLog.Statistics");
            if (path.Contains("auditlog/today")) return permissions.FirstOrDefault(p => p.Name == "AuditLog.Today");
            if (path.Contains("auditlog/failedlogins")) return permissions.FirstOrDefault(p => p.Name == "AuditLog.FailedLogins");
            if (path.Contains("auditlog/eventtypes")) return permissions.FirstOrDefault(p => p.Name == "AuditLog.EventTypes");
            if (path.Contains("auditlog/user")) return permissions.FirstOrDefault(p => p.Name == "AuditLog.UserLogs");
            if (path.Contains("auditlog")) return permissions.FirstOrDefault(p => p.Name == "AuditLog.Read");

            // Menus - منوی والد قبل از زیرمنوها
            if (path == "api/menu") return permissions.FirstOrDefault(p => p.Name == "Menus.Menu");
            if (path.Contains("menu/create")) return permissions.FirstOrDefault(p => p.Name == "Menus.Create");
            if (path.Contains("menu/edit")) return permissions.FirstOrDefault(p => p.Name == "Menus.Update");
            if (path.Contains("menu/tree")) return permissions.FirstOrDefault(p => p.Name == "Menus.Read");
            if (path.Contains("menu")) return permissions.FirstOrDefault(p => p.Name == "Menus.Read");

            // Permissions - منوی والد قبل از زیرمنوها
            if (path == "api/permission") return permissions.FirstOrDefault(p => p.Name == "Permissions.Menu");
            if (path.Contains("permission/assign")) return permissions.FirstOrDefault(p => p.Name == "Permissions.Manage");
            if (path.Contains("permission")) return permissions.FirstOrDefault(p => p.Name == "Permissions.Read");

            // Users - منوی والد قبل از زیرمنوها
            if (path == "api/users") return permissions.FirstOrDefault(p => p.Name == "Users.Menu");
            if (path.Contains("users/list")) return permissions.FirstOrDefault(p => p.Name == "Users.Read");
            if (path.Contains("users/create")) return permissions.FirstOrDefault(p => p.Name == "Users.Create");
            if (path.Contains("users/getbyid")) return permissions.FirstOrDefault(p => p.Name == "Users.ReadById");
            if (path.Contains("users/activate")) return permissions.FirstOrDefault(p => p.Name == "Users.Activate");
            if (path.Contains("users/resetpassword")) return permissions.FirstOrDefault(p => p.Name == "Users.ResetPassword");
            if (path.Contains("users/unlock")) return permissions.FirstOrDefault(p => p.Name == "Users.Unlock");
            if (path.Contains("users/lockoutstatus")) return permissions.FirstOrDefault(p => p.Name == "Users.LockoutStatus");
            if (path.Contains("users/export")) return permissions.FirstOrDefault(p => p.Name == "Users.Export");
            if (path.Contains("users")) return permissions.FirstOrDefault(p => p.Name == "Users.Read");

            // Groups - منوی والد قبل از زیرمنوها
            if (path == "api/grp") return permissions.FirstOrDefault(p => p.Name == "Groups.Menu");
            if (path.Contains("grp/list")) return permissions.FirstOrDefault(p => p.Name == "Groups.Read");
            if (path.Contains("grp/create")) return permissions.FirstOrDefault(p => p.Name == "Groups.Create");
            if (path.Contains("grp/edit")) return permissions.FirstOrDefault(p => p.Name == "Groups.Update");
            if (path.Contains("grp")) return permissions.FirstOrDefault(p => p.Name == "Groups.Read");

            // Version (FPT_TUD_EXT.1.2 و FPT_TUD_EXT.1.3) - منوی والد قبل از زیرمنوها
            if (path == "api/version") return permissions.FirstOrDefault(p => p.Name == "System.Version.Menu");
            if (path.Contains("version/signature/verify")) return permissions.FirstOrDefault(p => p.Name == "System.Version.VerifySignature");
            if (path.Contains("version/signature/verify-metadata")) return permissions.FirstOrDefault(p => p.Name == "System.Version.VerifySignature");
            if (path.Contains("version/signature/compute-hash")) return permissions.FirstOrDefault(p => p.Name == "System.Version.VerifySignature");
            if (path.Contains("version/signature/public-key-info")) return permissions.FirstOrDefault(p => p.Name == "System.Version.Read");
            if (path.Contains("version/frontend/check")) return permissions.FirstOrDefault(p => p.Name == "System.Version.FrontendCheck");
            if (path.Contains("version/frontend/current")) return permissions.FirstOrDefault(p => p.Name == "System.Version.Read");
            if (path.Contains("version/check")) return permissions.FirstOrDefault(p => p.Name == "System.Version.BackendCheck");
            if (path.Contains("version/info")) return permissions.FirstOrDefault(p => p.Name == "System.Version.Info");
            if (path.Contains("version/current")) return permissions.FirstOrDefault(p => p.Name == "System.Version.Read");
            if (path.Contains("version")) return permissions.FirstOrDefault(p => p.Name == "System.Version.Read");

            // Data Export - خروجی داده‌ها (FDP_ETC.2)
            if (path.Contains("dataexport/settings")) return permissions.FirstOrDefault(p => p.Name == "DataExport.Settings");
            if (path.Contains("dataexport/rules")) return permissions.FirstOrDefault(p => p.Name == "DataExport.Rules");
            if (path.Contains("dataexport/masking")) return permissions.FirstOrDefault(p => p.Name == "DataExport.Masking");
            if (path.Contains("dataexport/sensitivitylevels")) return permissions.FirstOrDefault(p => p.Name == "DataExport.SensitivityLevel");
            if (path.Contains("dataexport/auditlog")) return permissions.FirstOrDefault(p => p.Name == "DataExport.Audit");
            if (path.Contains("dataexport")) return permissions.FirstOrDefault(p => p.Name == "DataExport.Read");

            // Shobes (شعب) - منوی والد قبل از زیرمنوها
            if (path == "api/shobe") return permissions.FirstOrDefault(p => p.Name == "Shobes.Menu");
            if (path.Contains("shobe/list")) return permissions.FirstOrDefault(p => p.Name == "Shobes.Read");
            if (path.Contains("shobe/create")) return permissions.FirstOrDefault(p => p.Name == "Shobes.Create");
            if (path.Contains("shobe/edit")) return permissions.FirstOrDefault(p => p.Name == "Shobes.Update");
            if (path.Contains("shobe/delete")) return permissions.FirstOrDefault(p => p.Name == "Shobes.Delete");
            if (path.Contains("shobe/settings")) return permissions.FirstOrDefault(p => p.Name == "Shobes.Settings");
            if (path.Contains("shobe")) return permissions.FirstOrDefault(p => p.Name == "Shobes.Read");

            // Management - راهبری سیستم
            if (path.Contains("management/dashboard")) return permissions.FirstOrDefault(p => p.Name == "Management.Dashboard.Read");
            
            // Software - نرم‌افزارها
            if (path.Contains("management/softwares/create")) return permissions.FirstOrDefault(p => p.Name == "Management.Software.Create");
            if (path.Contains("management/softwares")) return permissions.FirstOrDefault(p => p.Name == "Management.Software.Read");
            
            // Plans - پلن‌ها
            if (path.Contains("management/plans/create")) return permissions.FirstOrDefault(p => p.Name == "Management.Plan.Create");
            if (path.Contains("management/plans")) return permissions.FirstOrDefault(p => p.Name == "Management.Plan.Read");
            
            // Customers - مشتریان
            if (path.Contains("management/customers/create")) return permissions.FirstOrDefault(p => p.Name == "Management.Customer.Create");
            if (path.Contains("management/customers")) return permissions.FirstOrDefault(p => p.Name == "Management.Customer.Read");
            
            // Subscriptions - اشتراک‌ها
            if (path.Contains("management/subscriptions/create")) return permissions.FirstOrDefault(p => p.Name == "Management.Subscription.Create");
            if (path.Contains("management/subscriptions")) return permissions.FirstOrDefault(p => p.Name == "Management.Subscription.Read");
            
            // Databases - دیتابیس‌ها
            if (path.Contains("management/databases/test-connection")) return permissions.FirstOrDefault(p => p.Name == "Management.Database.Read");
            if (path.Contains("management/databases/create")) return permissions.FirstOrDefault(p => p.Name == "Management.Database.Create");
            if (path.Contains("management/databases")) return permissions.FirstOrDefault(p => p.Name == "Management.Database.Read");
            
            // Management root
            if (path.Contains("management")) return permissions.FirstOrDefault(p => p.Name == "Management.Dashboard.Read");

            return null;
        }

        /// <summary>
        /// Seed تنظیمات SMS در tblShobeSettings
        /// </summary>
        private async Task SeedShobeSmsSettingsAsync()
        {
            var now = DateTime.UtcNow;
            const string settingKey = "SmsSettings";

            // بررسی وجود تنظیمات عمومی SMS
            var existing = await _context.tblShobeSettings
                .FirstOrDefaultAsync(s => s.SettingKey == settingKey && s.TblShobeId == null);

            if (existing == null)
            {
                // خواندن تنظیمات SMS از Configuration (محیط Development: appsettings.Development.json / محیط Production: Environment Variables)
                var smsBaseUrl = _configuration["SeedSecrets:SmsBaseUrl"];
                var smsApiKey = _configuration["SeedSecrets:SmsApiKey"];
                var smsSenderNumber = _configuration["SeedSecrets:SmsSenderNumber"];

                if (string.IsNullOrWhiteSpace(smsApiKey) || string.IsNullOrWhiteSpace(smsSenderNumber))
                {
                    _logger.LogWarning("SeedSecrets:SmsApiKey or SeedSecrets:SmsSenderNumber is not configured. Using empty defaults for SMS settings.");
                }

                var smsSettings = new
                {
                    BaseUrl = smsBaseUrl ?? "http://ssmss.ir/webservice/rest/sms_send",
                    ApiKey = smsApiKey ?? "",
                    SenderNumber = smsSenderNumber ?? "",
                    OtpLength = 6,
                    OtpExpirySeconds = 120,
                    MessageTemplate = "کد تایید شما: {0}\nاین کد تا {1} ثانیه معتبر است."
                };

                var settingValue = JsonSerializer.Serialize(smsSettings);

                var newSetting = new tblShobeSetting
                {
                    TblShobeId = null, // تنظیمات عمومی
                    SettingKey = settingKey,
                    SettingName = "تنظیمات SMS",
                    Description = "تنظیمات ارسال پیامک شامل BaseUrl، ApiKey، SenderNumber و سایر پارامترها",
                    SettingValue = settingValue,
                    SettingType = ShobeSettingType.Communication,
                    IsActive = true,
                    IsEditable = true,
                    DisplayOrder = 1,
                    TblUserGrpIdInsert = 1
                };

                newSetting.SetZamanInsert(now);
                _context.tblShobeSettings.Add(newSetting);
                await _context.SaveChangesAsync();

                _logger.LogInformation("SMS settings seeded successfully");
            }
            else
            {
                _logger.LogInformation("SMS settings already exist");
            }
        }

        /// <summary>
        /// Seed تنظیمات Attachment در tblShobeSettings
        /// شامل تنظیمات آپلود فایل، مسیر ذخیره‌سازی، Magic Bytes و ...
        /// </summary>
        private async Task SeedShobeAttachmentSettingsAsync()
        {
            var now = DateTime.UtcNow;
            const string settingKey = "AttachmentSettings";

            // بررسی وجود تنظیمات عمومی Attachment
            var existing = await _context.tblShobeSettings
                .FirstOrDefaultAsync(s => s.SettingKey == settingKey && s.TblShobeId == null);

            if (existing == null)
            {
                // تنظیمات پیش‌فرض
                var attachmentSettings = new
                {
                    StoragePath = "wwwroot/attachments",
                    MaxFileSizeMB = 50,
                    AllowedExtensions = ".pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png,.gif,.webp",
                    ValidateMagicBytes = true,
                    EnableVirusScan = false,
                    EnableEncryption = false,
                    MaxProfileImageSizeMB = 5,
                    AllowedImageExtensions = ".jpg,.jpeg,.png,.gif,.webp",
                    UseWebRoot = true
                };

                var settingValue = JsonSerializer.Serialize(attachmentSettings);

                var newSetting = new tblShobeSetting
                {
                    TblShobeId = null, // تنظیمات عمومی
                    SettingKey = settingKey,
                    SettingName = "تنظیمات فایل‌های پیوست",
                    Description = "تنظیمات مربوط به آپلود و مدیریت فایل‌های پیوست شامل مسیر ذخیره‌سازی، حداکثر حجم، فرمت‌های مجاز و اعتبارسنجی Magic Bytes",
                    SettingValue = settingValue,
                    SettingType = ShobeSettingType.Attachment,
                    IsActive = true,
                    IsEditable = true,
                    DisplayOrder = 2,
                    TblUserGrpIdInsert = 1
                };

                newSetting.SetZamanInsert(now);
                _context.tblShobeSettings.Add(newSetting);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Attachment settings seeded successfully");
            }
            else
            {
                _logger.LogInformation("Attachment settings already exist");
            }
        }

        /// <summary>
        /// Seed تنظیمات خروجی داده‌ها (DataExport) در tblShobeSettings
        /// مانند AttachmentSettings - مقادیر پیش‌فرض FDP_ETC.2
        /// </summary>
        private async Task SeedShobeDataExportSettingsAsync()
        {
            var now = DateTime.UtcNow;
            const string settingKey = "DataExport";

            var existing = await _context.tblShobeSettings
                .FirstOrDefaultAsync(s => s.SettingKey == settingKey && s.TblShobeId == null);

            if (existing == null)
            {
                var defaultSettings = new DataExportSettings
                {
                    IsEnabled = true,
                    EnableDigitalSignature = true,
                    EnableDataMasking = true,
                    EnableExportAudit = true,
                    MaxRecordsPerExport = 10000,
                    MaxExportSizeBytes = 50 * 1024 * 1024,
                    AllowedFormats = new List<string> { "JSON" },
                    DefaultSensitivityLevel = "Internal",
                    SignatureAlgorithm = "HMAC-SHA256",
                    AuditLogRetentionDays = 365
                };

                var settingValue = JsonSerializer.Serialize(defaultSettings);

                var newSetting = new tblShobeSetting
                {
                    TblShobeId = null,
                    SettingKey = settingKey,
                    SettingName = "تنظیمات خروجی داده‌ها",
                    Description = "تنظیمات مربوط به FDP_ETC.2 - خروجی داده با ویژگی‌های امنیتی",
                    SettingValue = settingValue,
                    SettingType = ShobeSettingType.DataExport,
                    IsActive = true,
                    IsEditable = true,
                    DisplayOrder = 3,
                    TblUserGrpIdInsert = 1
                };

                newSetting.SetZamanInsert(now);
                _context.tblShobeSettings.Add(newSetting);
                await _context.SaveChangesAsync();

                _logger.LogInformation("DataExport settings seeded successfully in tblShobeSettings");
            }
            else
            {
                _logger.LogInformation("DataExport settings already exist in tblShobeSettings");
            }

            // Seed قوانین خروجی (DataExport:Rules)
            const string rulesKey = "DataExport:Rules";
            var existingRules = await _context.tblShobeSettings
                .FirstOrDefaultAsync(s => s.SettingKey == rulesKey && s.TblShobeId == null);
            if (existingRules == null)
            {
                var defaultRules = new List<ExportRule>
                {
                    new ExportRule
                    {
                        Id = Guid.NewGuid(),
                        Name = "RecordLimit",
                        Description = "محدودیت تعداد رکورد در هر خروجی",
                        RuleType = ExportRuleType.RecordLimit,
                        EntityType = "*",
                        Priority = 10,
                        IsActive = true
                    },
                    new ExportRule
                    {
                        Id = Guid.NewGuid(),
                        Name = "SensitivityFilter",
                        Description = "فیلتر داده‌های با حساسیت بالا",
                        RuleType = ExportRuleType.SensitivityFilter,
                        EntityType = "*",
                        Priority = 20,
                        IsActive = true
                    }
                };
                var rulesSetting = new tblShobeSetting
                {
                    TblShobeId = null,
                    SettingKey = rulesKey,
                    SettingName = "قوانین خروجی داده‌ها",
                    Description = "قوانین کنترل خروجی (FDP_ETC.2.4)",
                    SettingValue = JsonSerializer.Serialize(defaultRules),
                    SettingType = ShobeSettingType.DataExport,
                    IsActive = true,
                    IsEditable = true,
                    DisplayOrder = 4,
                    TblUserGrpIdInsert = 1
                };
                rulesSetting.SetZamanInsert(now);
                _context.tblShobeSettings.Add(rulesSetting);
                await _context.SaveChangesAsync();
                _logger.LogInformation("DataExport:Rules seeded successfully in tblShobeSettings");
            }

            // Seed قوانین ماسک (DataExport:MaskingRules)
            const string maskingKey = "DataExport:MaskingRules";
            var existingMasking = await _context.tblShobeSettings
                .FirstOrDefaultAsync(s => s.SettingKey == maskingKey && s.TblShobeId == null);
            if (existingMasking == null)
            {
                var defaultMaskingRules = new List<DataMaskingRule>
                {
                    new DataMaskingRule
                    {
                        Id = Guid.NewGuid(),
                        Name = "EmailMask",
                        Description = "ماسک آدرس ایمیل",
                        EntityType = "*",
                        FieldName = "Email",
                        MaskingType = MaskingType.EmailMask,
                        IsActive = true
                    },
                    new DataMaskingRule
                    {
                        Id = Guid.NewGuid(),
                        Name = "MobileNumberMask",
                        Description = "ماسک شماره موبایل",
                        EntityType = "*",
                        FieldName = "MobileNumber",
                        MaskingType = MaskingType.PhoneMask,
                        VisibleCharsStart = 0,
                        VisibleCharsEnd = 4,
                        IsActive = true
                    },
                    new DataMaskingRule
                    {
                        Id = Guid.NewGuid(),
                        Name = "PhoneMask",
                        Description = "ماسک شماره تلفن",
                        EntityType = "*",
                        FieldName = "Phone",
                        MaskingType = MaskingType.PhoneMask,
                        VisibleCharsStart = 0,
                        VisibleCharsEnd = 4,
                        IsActive = true
                    },
                    new DataMaskingRule
                    {
                        Id = Guid.NewGuid(),
                        Name = "IpAddressMask",
                        Description = "ماسک آدرس IP",
                        EntityType = "*",
                        FieldName = "IpAddress",
                        MaskingType = MaskingType.PartialMask,
                        VisibleCharsStart = 0,
                        VisibleCharsEnd = 0,
                        MaskPattern = "***.***.***",
                        IsActive = true,
                        ExcludePermissions = "DataExport.Admin"
                    }
                };
                var maskingSetting = new tblShobeSetting
                {
                    TblShobeId = null,
                    SettingKey = maskingKey,
                    SettingName = "قوانین ماسک داده‌ها",
                    Description = "قوانین ماسک کردن داده‌های حساس (FDP_ETC.2.4)",
                    SettingValue = JsonSerializer.Serialize(defaultMaskingRules),
                    SettingType = ShobeSettingType.DataExport,
                    IsActive = true,
                    IsEditable = true,
                    DisplayOrder = 5,
                    TblUserGrpIdInsert = 1
                };
                maskingSetting.SetZamanInsert(now);
                _context.tblShobeSettings.Add(maskingSetting);
                await _context.SaveChangesAsync();
                _logger.LogInformation("DataExport:MaskingRules seeded successfully in tblShobeSettings");
            }

            // Seed سطوح حساسیت (DataExport:SensitivityLevels)
            const string sensitivityKey = "DataExport:SensitivityLevels";
            var existingSensitivity = await _context.tblShobeSettings
                .FirstOrDefaultAsync(s => s.SettingKey == sensitivityKey && s.TblShobeId == null);
            if (existingSensitivity == null)
            {
                var defaultSensitivityLevels = new List<SensitivityLevel>
                {
                    new SensitivityLevel
                    {
                        Code = "Public",
                        Name = "عمومی",
                        Description = "داده‌های قابل انتشار عمومی",
                        Level = 0,
                        Color = "#28a745",
                        RequiresEncryption = false,
                        RequiresAudit = false,
                        RequiresApproval = false
                    },
                    new SensitivityLevel
                    {
                        Code = "Internal",
                        Name = "داخلی",
                        Description = "داده‌های داخلی سازمان",
                        Level = 1,
                        Color = "#17a2b8",
                        RequiresEncryption = false,
                        RequiresAudit = true,
                        RequiresApproval = false
                    },
                    new SensitivityLevel
                    {
                        Code = "Confidential",
                        Name = "محرمانه",
                        Description = "داده‌های محرمانه",
                        Level = 2,
                        Color = "#ffc107",
                        RequiresEncryption = true,
                        RequiresAudit = true,
                        RequiresApproval = false,
                        RequiredPermission = "DataExport.Read"
                    },
                    new SensitivityLevel
                    {
                        Code = "Secret",
                        Name = "سری",
                        Description = "داده‌های سری و حساس",
                        Level = 3,
                        Color = "#dc3545",
                        RequiresEncryption = true,
                        RequiresAudit = true,
                        RequiresApproval = true,
                        RequiredPermission = "DataExport.Admin"
                    }
                };
                var sensitivitySetting = new tblShobeSetting
                {
                    TblShobeId = null,
                    SettingKey = sensitivityKey,
                    SettingName = "سطوح حساسیت داده",
                    Description = "سطوح حساسیت برای خروجی داده (FDP_ETC.2)",
                    SettingValue = JsonSerializer.Serialize(defaultSensitivityLevels),
                    SettingType = ShobeSettingType.DataExport,
                    IsActive = true,
                    IsEditable = true,
                    DisplayOrder = 6,
                    TblUserGrpIdInsert = 1
                };
                sensitivitySetting.SetZamanInsert(now);
                _context.tblShobeSettings.Add(sensitivitySetting);
                await _context.SaveChangesAsync();
                _logger.LogInformation("DataExport:SensitivityLevels seeded successfully in tblShobeSettings");
            }
        }

        /// <summary>
        /// Seed داده‌های اولیه راهبری سیستم
        /// شامل: نرم‌افزار، دیتابیس‌های سیستم، پلن پایه، مشتری دمو
        /// </summary>
        private async Task SeedManagementDataAsync()
        {
            var now = DateTime.UtcNow;

            // ============================================
            // 1. Seed نرم‌افزار راهبری سیستم
            // ============================================
            var existingSoftware = await _context.tblSoftwares.FirstOrDefaultAsync(s => s.Code == "NAV");
            tblSoftware navigationSoftware;

            if (existingSoftware == null)
            {
                navigationSoftware = new tblSoftware
                {
                    Name = "نرم‌افزار راهبری سیستم",
                    Code = "NAV",
                    CurrentVersion = "1.0.0",
                    Description = "سیستم مدیریت یکپارچه برای راهبری تمام محصولات نرم‌افزاری شرکت - شامل مدیریت کاربران، دسترسی‌ها، لاگ‌ها و تنظیمات امنیتی",
                    Icon = "layout-dashboard",
                    IsActive = true,
                    DisplayOrder = 1,
                    TblUserGrpIdInsert = 1
                };
                navigationSoftware.SetZamanInsert(now);
                _context.tblSoftwares.Add(navigationSoftware);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Navigation software created with Id={Id}", navigationSoftware.Id);
            }
            else
            {
                navigationSoftware = existingSoftware;
                _logger.LogInformation("Navigation software already exists with Id={Id}", navigationSoftware.Id);
            }

            // ============================================
            // 2. Seed پلن پایه برای نرم‌افزار راهبری
            // ============================================
            var existingPlan = await _context.tblPlans.FirstOrDefaultAsync(p => p.Code == "NAV-BASE" && p.tblSoftwareId == navigationSoftware.Id);
            tblPlan basePlan;

            if (existingPlan == null)
            {
                basePlan = new tblPlan
                {
                    tblSoftwareId = navigationSoftware.Id,
                    Name = "پلن پایه راهبری",
                    Code = "NAV-BASE",
                    MaxMemberCount = null, // نامحدود
                    MaxUserCount = 100,
                    FeaturesJson = @"{""users"":true,""groups"":true,""permissions"":true,""menus"":true,""auditLog"":true,""security"":true,""dataExport"":true,""shobes"":true}",
                    BasePrice = 0,
                    IsActive = true,
                    DisplayOrder = 1,
                    TblUserGrpIdInsert = 1
                };
                basePlan.SetZamanInsert(now);
                _context.tblPlans.Add(basePlan);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Base plan created with Id={Id}", basePlan.Id);
            }
            else
            {
                basePlan = existingPlan;
                _logger.LogInformation("Base plan already exists with Id={Id}", basePlan.Id);
            }

            // ============================================
            // 3. Seed مشتری دمو - شرکت پاسارگاد
            // ============================================
            var existingCustomer = await _context.tblCustomers.FirstOrDefaultAsync(c => c.CustomerCode == "PASARGAD-DEMO");
            tblCustomer demoCustomer;

            if (existingCustomer == null)
            {
                demoCustomer = new tblCustomer
                {
                    Name = "شرکت پاسارگاد (دمو)",
                    CustomerCode = "PASARGAD-DEMO",
                    CustomerType = 2, // حقوقی
                    ManagerName = "مدیر سیستم",
                    Phone = "021-12345678",
                    Mobile = "09121234567",
                    Email = "demo@pasargad.ir",
                    Website = "https://www.pasargad.ir",
                    Address = "تهران، خیابان آزادی",
                    Province = "تهران",
                    City = "تهران",
                    MembershipDate = "1404/01/01",
                    Status = 1, // فعال
                    CustomerLevel = 4, // الماسی
                    Description = "مشتری دمو برای تست سیستم راهبری",
                    TblUserGrpIdInsert = 1
                };
                demoCustomer.SetZamanInsert(now);
                _context.tblCustomers.Add(demoCustomer);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Demo customer (Pasargad) created with Id={Id}", demoCustomer.Id);
            }
            else
            {
                demoCustomer = existingCustomer;
                _logger.LogInformation("Demo customer (Pasargad) already exists with Id={Id}", demoCustomer.Id);
            }

            // رمز عبور رمزگذاری شده برای دیتابیس‌ها - از Configuration
            var dbPassword = _configuration["SeedSecrets:DatabasePassword"];
            if (string.IsNullOrWhiteSpace(dbPassword))
            {
                _logger.LogError("SeedSecrets:DatabasePassword is not configured. Cannot seed database records.");
                throw new InvalidOperationException(
                    "رمز عبور دیتابیس تنظیم نشده است. لطفاً SeedSecrets:DatabasePassword را در appsettings یا Environment Variable تنظیم کنید.");
            }
            var encryptedDbPassword = _dbConnectionService.EncryptPassword(dbPassword);
            _logger.LogInformation("Database password encrypted for Seed");

            // ============================================
            // 4. Seed دیتابیس راهبری سیستم (NavigationDb)
            // ============================================
            var existingNavDb = await _context.tblDbs.FirstOrDefaultAsync(d => d.DbCode == "NAV-DB");

            if (existingNavDb == null)
            {
                var navDb = new tblDb
                {
                    tblCustomerId = demoCustomer.Id, // مشتری پیش‌فرض (پاسارگاد دمو)
                    tblSoftwareId = navigationSoftware.Id,
                    Name = "دیتابیس راهبری سیستم",
                    DbCode = "NAV-DB",
                    ServerName = ".", // localhost
                    Port = 1433,
                    DatabaseName = "NavigationDb",
                    Username = "sa",
                    EncryptedPassword = encryptedDbPassword,
                    DbType = 1, // SqlServer
                    Environment = 4, // Production
                    IsShared = false,
                    IsPrimary = true,
                    IsReadOnly = false,
                    Status = 1, // فعال
                    DisplayOrder = 1,
                    Description = "دیتابیس اصلی راهبری سیستم شامل جداول کاربران، گروه‌ها، منوها، دسترسی‌ها و تنظیمات امنیتی",
                    TblUserGrpIdInsert = 1
                };
                navDb.SetZamanInsert(now);
                _context.tblDbs.Add(navDb);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Navigation database registered with Id={Id}", navDb.Id);
            }
            else
            {
                _logger.LogInformation("Navigation database already exists with Id={Id}", existingNavDb.Id);
            }

            // ============================================
            // 5. Seed دیتابیس لاگ‌ها (BnpLogCloudDB)
            // ============================================
            var existingLogDb = await _context.tblDbs.FirstOrDefaultAsync(d => d.DbCode == "LOG-DB");

            if (existingLogDb == null)
            {
                var logDb = new tblDb
                {
                    tblCustomerId = demoCustomer.Id, // مشتری پیش‌فرض
                    tblSoftwareId = navigationSoftware.Id,
                    Name = "دیتابیس لاگ‌های امنیتی",
                    DbCode = "LOG-DB",
                    ServerName = ".", // localhost
                    Port = 1433,
                    DatabaseName = "BnpLogCloudDB",
                    Username = "sa",
                    EncryptedPassword = encryptedDbPassword,
                    DbType = 1, // SqlServer
                    Environment = 4, // Production
                    IsShared = true, // لاگ‌ها برای همه نرم‌افزارها مشترک
                    IsPrimary = false,
                    IsReadOnly = false,
                    Status = 1, // فعال
                    DisplayOrder = 2,
                    Description = "دیتابیس مرکزی لاگ‌های امنیتی و ممیزی برای تمام محصولات نرم‌افزاری - مطابق با استانداردهای ISO 15408 (FAU_STG)",
                    TblUserGrpIdInsert = 1
                };
                logDb.SetZamanInsert(now);
                _context.tblDbs.Add(logDb);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Log database registered with Id={Id}", logDb.Id);
            }
            else
            {
                _logger.LogInformation("Log database already exists with Id={Id}", existingLogDb.Id);
            }

            // ============================================
            // 6. Seed نرم‌افزار قرض‌الحسنه / تعاونی اعتبار
            // ============================================
            var existingCashSoftware = await _context.tblSoftwares.FirstOrDefaultAsync(s => s.Code == "CASH");
            tblSoftware cashSoftware;

            if (existingCashSoftware == null)
            {
                cashSoftware = new tblSoftware
                {
                    Name = "نرم‌افزار قرض‌الحسنه / تعاونی اعتبار",
                    Code = "CASH",
                    CurrentVersion = "1.0.0",
                    Description = "سیستم جامع مدیریت صندوق قرض‌الحسنه و تعاونی اعتبار - شامل مدیریت اعضا، حساب‌ها، سپرده‌ها، وام‌ها و تراکنش‌های مالی",
                    Icon = "wallet",
                    IsActive = true,
                    DisplayOrder = 2,
                    TblUserGrpIdInsert = 1
                };
                cashSoftware.SetZamanInsert(now);
                _context.tblSoftwares.Add(cashSoftware);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cash software created with Id={Id}", cashSoftware.Id);
            }
            else
            {
                cashSoftware = existingCashSoftware;
                _logger.LogInformation("Cash software already exists with Id={Id}", cashSoftware.Id);
            }

            // ============================================
            // 7. Seed پلن‌های نرم‌افزار قرض‌الحسنه
            // ============================================
            var cashPlans = new List<(string Code, string Name, int? MaxMember, int? MaxUser, decimal Price, int Order)>
            {
                ("CASH-50", "پلن ۵۰ عضو", 50, 3, 5000000, 1),
                ("CASH-100", "پلن ۱۰۰ عضو", 100, 5, 8000000, 2),
                ("CASH-500", "پلن ۵۰۰ عضو", 500, 10, 15000000, 3),
                ("CASH-1000", "پلن ۱۰۰۰ عضو", 1000, 20, 25000000, 4),
                ("CASH-UNL", "پلن نامحدود", null, null, 50000000, 5),
            };

            tblPlan? cashBasePlan = null;
            foreach (var planData in cashPlans)
            {
                var existingCashPlan = await _context.tblPlans.FirstOrDefaultAsync(p => p.Code == planData.Code);
                if (existingCashPlan == null)
                {
                    var newPlan = new tblPlan
                    {
                        tblSoftwareId = cashSoftware.Id,
                        Name = planData.Name,
                        Code = planData.Code,
                        MaxMemberCount = planData.MaxMember,
                        MaxUserCount = planData.MaxUser,
                        FeaturesJson = @"{""members"":true,""accounts"":true,""deposits"":true,""loans"":true,""transactions"":true,""reports"":true,""sms"":true}",
                        BasePrice = planData.Price,
                        IsActive = true,
                        DisplayOrder = planData.Order,
                        TblUserGrpIdInsert = 1
                    };
                    newPlan.SetZamanInsert(now);
                    _context.tblPlans.Add(newPlan);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cash plan '{Name}' created with Id={Id}", planData.Name, newPlan.Id);
                    
                    if (cashBasePlan == null) cashBasePlan = newPlan;
                }
                else
                {
                    if (cashBasePlan == null) cashBasePlan = existingCashPlan;
                }
            }

            // ============================================
            // 8. Seed دیتابیس قرض‌الحسنه (BnpCashCloudDB)
            // ============================================
            var existingCashDb = await _context.tblDbs.FirstOrDefaultAsync(d => d.DbCode == "CASH-DB");

            if (existingCashDb == null)
            {
                var cashDb = new tblDb
                {
                    tblCustomerId = demoCustomer.Id, // مشتری پیش‌فرض
                    tblSoftwareId = cashSoftware.Id,
                    Name = "دیتابیس قرض‌الحسنه",
                    DbCode = "CASH-DB",
                    ServerName = ".", // localhost
                    Port = 1433,
                    DatabaseName = "BnpCashCloudDB",
                    Username = "sa",
                    EncryptedPassword = encryptedDbPassword,
                    DbType = 1, // SqlServer
                    Environment = 4, // Production
                    IsShared = true, // چند مستاجره
                    IsPrimary = true,
                    IsReadOnly = false,
                    Status = 1, // فعال
                    DisplayOrder = 3,
                    Description = "دیتابیس اصلی نرم‌افزار قرض‌الحسنه / تعاونی اعتبار - شامل جداول اعضا، حساب‌ها، سپرده‌ها، وام‌ها و تراکنش‌ها",
                    TblUserGrpIdInsert = 1
                };
                cashDb.SetZamanInsert(now);
                _context.tblDbs.Add(cashDb);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cash database registered with Id={Id}", cashDb.Id);
            }
            else
            {
                _logger.LogInformation("Cash database already exists with Id={Id}", existingCashDb.Id);
            }

            // ============================================
            // 9. Seed دیتابیس فایل‌ها و تصاویر (BnpAttachCloudDB)
            // ============================================
            var existingAttachDb = await _context.tblDbs.FirstOrDefaultAsync(d => d.DbCode == "ATTACH-DB");

            if (existingAttachDb == null)
            {
                var attachDb = new tblDb
                {
                    tblCustomerId = demoCustomer.Id, // مشتری پیش‌فرض
                    tblSoftwareId = navigationSoftware.Id, // مشترک بین همه نرم‌افزارها
                    Name = "دیتابیس فایل‌ها و تصاویر",
                    DbCode = "ATTACH-DB",
                    ServerName = ".", // localhost
                    Port = 1433,
                    DatabaseName = "BnpAttachCloudDB",
                    Username = "sa",
                    EncryptedPassword = encryptedDbPassword,
                    DbType = 1, // SqlServer
                    Environment = 4, // Production
                    IsShared = true, // مشترک بین همه نرم‌افزارها
                    IsPrimary = false,
                    IsReadOnly = false,
                    Status = 1, // فعال
                    DisplayOrder = 4,
                    Description = "دیتابیس مرکزی فایل‌ها، تصاویر، اسناد و ضمائم برای تمام محصولات نرم‌افزاری",
                    TblUserGrpIdInsert = 1
                };
                attachDb.SetZamanInsert(now);
                _context.tblDbs.Add(attachDb);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Attachment database registered with Id={Id}", attachDb.Id);
            }
            else
            {
                _logger.LogInformation("Attachment database already exists with Id={Id}", existingAttachDb.Id);
            }

            // ============================================
            // 10. Seed اشتراک مشتری دمو به نرم‌افزار راهبری
            // ============================================
            var existingNavSubscription = await _context.tblCustomerSoftwares
                .FirstOrDefaultAsync(cs => cs.tblCustomerId == demoCustomer.Id && cs.tblSoftwareId == navigationSoftware.Id);

            if (existingNavSubscription == null)
            {
                var subscription = new tblCustomerSoftware
                {
                    tblCustomerId = demoCustomer.Id,
                    tblSoftwareId = navigationSoftware.Id,
                    tblPlanId = basePlan.Id,
                    LicenseKey = GenerateLicenseKey(),
                    LicenseCount = 10,
                    UsedCount = 1,
                    StartDate = "1404/01/01",
                    EndDate = "1405/01/01", // یک سال اعتبار
                    Status = 1, // فعال
                    ActivationCount = 0,
                    MaxActivations = 5,
                    TblUserGrpIdInsert = 1
                };
                subscription.SetZamanInsert(now);
                _context.tblCustomerSoftwares.Add(subscription);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Demo customer navigation subscription created with Id={Id}, LicenseKey={Key}",
                    subscription.Id, subscription.LicenseKey);
            }
            else
            {
                _logger.LogInformation("Demo customer navigation subscription already exists with Id={Id}", existingNavSubscription.Id);
            }

            // ============================================
            // 11. Seed اشتراک مشتری دمو به نرم‌افزار قرض‌الحسنه
            // ============================================
            var existingCashSubscription = await _context.tblCustomerSoftwares
                .FirstOrDefaultAsync(cs => cs.tblCustomerId == demoCustomer.Id && cs.tblSoftwareId == cashSoftware.Id);

            if (existingCashSubscription == null && cashBasePlan != null)
            {
                var subscription = new tblCustomerSoftware
                {
                    tblCustomerId = demoCustomer.Id,
                    tblSoftwareId = cashSoftware.Id,
                    tblPlanId = cashBasePlan.Id,
                    LicenseKey = GenerateLicenseKey(),
                    LicenseCount = 50, // پلن ۵۰ عضو
                    UsedCount = 0,
                    StartDate = "1404/01/01",
                    EndDate = "1405/01/01", // یک سال اعتبار
                    Status = 1, // فعال
                    ActivationCount = 0,
                    MaxActivations = 3,
                    TblUserGrpIdInsert = 1
                };
                subscription.SetZamanInsert(now);
                _context.tblCustomerSoftwares.Add(subscription);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Demo customer cash subscription created with Id={Id}, LicenseKey={Key}",
                    subscription.Id, subscription.LicenseKey);
            }
            else
            {
                _logger.LogInformation("Demo customer cash subscription already exists");
            }

            _logger.LogInformation("Management data seeding completed");
        }

        /// <summary>
        /// تولید کلید لایسنس یکتا
        /// فرمت: XXXX-XXXX-XXXX-XXXX
        /// </summary>
        private static string GenerateLicenseKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var segments = new string[4];

            for (int i = 0; i < 4; i++)
            {
                var segment = new char[4];
                for (int j = 0; j < 4; j++)
                {
                    segment[j] = chars[random.Next(chars.Length)];
                }
                segments[i] = new string(segment);
            }

            return string.Join("-", segments);
        }
    }
}
