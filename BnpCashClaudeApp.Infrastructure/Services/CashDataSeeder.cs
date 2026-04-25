using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Entities.CashSubsystem;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// سرویس Seed داده‌های اولیه نرم‌افزار قرض‌الحسنه
    /// ============================================
    /// شامل Seed کردن منوها، Permission ها و سایر اطلاعات پایه
    /// مرتبط با نرم‌افزار قرض‌الحسنه / تعاونی اعتبار
    /// ============================================
    /// </summary>
    public class CashDataSeeder
    {
        private readonly NavigationDbContext _context;
        private readonly CashDbContext _cashContext;
        private readonly IDataIntegrityService _dataIntegrityService;
        private readonly ILogger<CashDataSeeder> _logger;

        // فیلدهای حساس برای محاسبه IntegrityHash
        private static readonly string[] TafsiliTypeSensitiveFields = new[] { "Title", "CodeTafsiliType", "tblShobeId", "ParentId", "IsActive" };
        private static readonly string[] AzaNoeSensitiveFields = new[] { "Title", "CodeHoze", "tblShobeId", "tblTafsiliTypeId", "PishFarz", "IsActive" };

        public CashDataSeeder(
            NavigationDbContext context,
            CashDbContext cashContext,
            IDataIntegrityService dataIntegrityService,
            ILogger<CashDataSeeder> logger)
        {
            _context = context;
            _cashContext = cashContext;
            _dataIntegrityService = dataIntegrityService;
            _logger = logger;
        }

        /// <summary>
        /// Seed تمام داده‌های اولیه قرض‌الحسنه
        /// </summary>
        public async Task SeedAllAsync()
        {
            _logger.LogInformation("========================================");
            _logger.LogInformation("Starting CashDataSeeder.SeedAllAsync...");
            _logger.LogInformation("========================================");

            try
            {
                // 1. Seed منوهای قرض‌الحسنه
                await SeedCashMenusAsync();

                // 2. Seed Permission های قرض‌الحسنه
                await SeedCashPermissionsAsync();

                // 3. Seed ارتباط منو با Permission
                await SeedCashMenuPermissionsAsync();

                // 4. اعطای Permission ها به گروه Admin
                await SeedCashGrpPermissionsAsync();

                // 5. Seed داده‌های پیش‌فرض انواع مشتری و انواع حوزه
                await SeedDefaultTafsiliTypeAndAzaNoeAsync();

                _logger.LogInformation("========================================");
                _logger.LogInformation("CashDataSeeder completed successfully!");
                _logger.LogInformation("========================================");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CashDataSeeder: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Seed منوهای نرم‌افزار قرض‌الحسنه
        /// tblSoftwareId = 2 (CASH)
        /// </summary>
        private async Task SeedCashMenusAsync()
        {
            var now = DateTime.UtcNow;

            // پیدا کردن نرم‌افزار قرض‌الحسنه
            var cashSoftware = await _context.tblSoftwares.FirstOrDefaultAsync(s => s.Code == "CASH");
            if (cashSoftware == null)
            {
                _logger.LogWarning("Cash software not found. Please run main DataSeeder first.");
                return;
            }

            var softwareId = cashSoftware.Id;
            _logger.LogInformation("Cash software found with Id={Id}", softwareId);

            // ============================================
            // منوهای سطح اول - اطلاعات پایه
            // ============================================
            var baseInfoMenu = await GetOrCreateMenuAsync(
                title: "اطلاعات پایه",
                path: "CashApi/BaseInfo",
                icon: "database",
                parentId: null,
                softwareId: softwareId,
                isMenu: true,
                now: now);

            // ============================================
            // منوهای سطح دوم - تنظیمات اولیه
            // ============================================
            var settingsMenu = await GetOrCreateMenuAsync(
                title: "تنظیمات اولیه",
                path: "CashApi/BaseInfo/Settings",
                icon: "settings",
                parentId: baseInfoMenu.Id,
                softwareId: softwareId,
                isMenu: true,
                now: now);

            // ============================================
            // منوهای سطح سوم - انواع مشتریان و انواع حوزه
            // ============================================
            
            // منوی انواع مشتریان (انواع تفصیلی)
            var tafsiliTypeMenu = await GetOrCreateMenuAsync(
                title: "انواع مشتریان",
                path: "api/TafsiliType",
                icon: "users-round",
                parentId: settingsMenu.Id,
                softwareId: softwareId,
                isMenu: true,
                now: now);

            // زیرمنوهای انواع مشتریان (فقط عملیات اصلی)
            await GetOrCreateMenuAsync("تعریف نوع مشتری جدید", "api/TafsiliType/Create", "user-plus", tafsiliTypeMenu.Id, softwareId, false, now);
            await GetOrCreateMenuAsync("ویرایش نوع مشتری", "api/TafsiliType/Edit", "user-pen", tafsiliTypeMenu.Id, softwareId, false, now);

            // منوی انواع حوزه (دسته‌بندی)
            var azaNoeMenu = await GetOrCreateMenuAsync(
                title: "انواع حوزه",
                path: "api/AzaNoe",
                icon: "layers",
                parentId: settingsMenu.Id,
                softwareId: softwareId,
                isMenu: true,
                now: now);

            // زیرمنوهای انواع حوزه (فقط عملیات اصلی)
            await GetOrCreateMenuAsync("تعریف حوزه جدید", "api/AzaNoe/Create", "layers-plus", azaNoeMenu.Id, softwareId, false, now);
            await GetOrCreateMenuAsync("ویرایش حوزه", "api/AzaNoe/Edit", "layers-pen", azaNoeMenu.Id, softwareId, false, now);

            // منوی سرفصل‌های حسابداری
            var sarfaslMenu = await GetOrCreateMenuAsync(
                title: "سرفصل‌ها",
                path: "api/Sarfasl",
                icon: "book-open",
                parentId: settingsMenu.Id,
                softwareId: softwareId,
                isMenu: true,
                now: now);

            // زیرمنوهای سرفصل‌ها (فقط عملیات اصلی)
            await GetOrCreateMenuAsync("تعریف سرفصل جدید", "api/Sarfasl/Create", "book-plus", sarfaslMenu.Id, softwareId, false, now);
            await GetOrCreateMenuAsync("ویرایش سرفصل", "api/Sarfasl/Edit", "book-pen", sarfaslMenu.Id, softwareId, false, now);
            await GetOrCreateMenuAsync("درخت سرفصل‌ها", "api/Sarfasl/Tree", "git-branch", sarfaslMenu.Id, softwareId, false, now);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Cash menus seeding completed. Total Cash menus: {Count}",
                await _context.tblMenus.CountAsync(m => m.tblSoftwareId == softwareId));
        }

        /// <summary>
        /// Helper برای ایجاد یا دریافت منو
        /// </summary>
        private async Task<tblMenu> GetOrCreateMenuAsync(
            string title, string path, string icon,
            long? parentId, long softwareId, bool isMenu, DateTime now)
        {
            var existing = await _context.tblMenus
                .FirstOrDefaultAsync(m => m.Path == path && m.tblSoftwareId == softwareId);

            if (existing != null)
            {
                // به‌روزرسانی در صورت نیاز
                if (existing.Icon != icon) existing.Icon = icon;
                if (existing.Title != title) existing.Title = title;
                if (existing.IsMenu != isMenu) existing.IsMenu = isMenu;
                await _context.SaveChangesAsync();
                return existing;
            }

            var newMenu = new tblMenu
            {
                Title = title,
                Path = path,
                Icon = icon,
                ParentId = parentId,
                tblSoftwareId = softwareId,
                IsMenu = isMenu,
                TblUserGrpIdInsert = 1
            };
            newMenu.SetZamanInsert(now);
            _context.tblMenus.Add(newMenu);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created menu: {Title} ({Path})", title, path);
            return newMenu;
        }

        /// <summary>
        /// Seed Permission های قرض‌الحسنه
        /// </summary>
        private async Task SeedCashPermissionsAsync()
        {
            var now = DateTime.UtcNow;

            var permissionsData = new List<(string Name, string Resource, string Action, string Description, int DisplayOrder)>
            {
                // TafsiliType - انواع مشتری
                ("TafsiliType.Read", "TafsiliType", "Read", "مشاهده لیست انواع مشتری", 200),
                ("TafsiliType.Create", "TafsiliType", "Create", "ایجاد نوع مشتری جدید", 201),
                ("TafsiliType.Update", "TafsiliType", "Update", "ویرایش نوع مشتری", 202),
                ("TafsiliType.Delete", "TafsiliType", "Delete", "حذف نوع مشتری", 203),

                // AzaNoe - انواع حوزه
                ("AzaNoe.Read", "AzaNoe", "Read", "مشاهده لیست انواع حوزه", 210),
                ("AzaNoe.Create", "AzaNoe", "Create", "ایجاد حوزه جدید", 211),
                ("AzaNoe.Update", "AzaNoe", "Update", "ویرایش حوزه", 212),
                ("AzaNoe.Delete", "AzaNoe", "Delete", "حذف حوزه", 213),

                // Sarfasl - سرفصل‌های حسابداری
                ("Sarfasl.Read", "Sarfasl", "Read", "مشاهده لیست سرفصل‌ها", 220),
                ("Sarfasl.Create", "Sarfasl", "Create", "ایجاد سرفصل جدید", 221),
                ("Sarfasl.Update", "Sarfasl", "Update", "ویرایش سرفصل", 222),
                ("Sarfasl.Delete", "Sarfasl", "Delete", "حذف سرفصل", 223),
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
                    _logger.LogInformation("Adding Cash permission: {Name}", permData.Name);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Cash permissions seeding completed.");
        }

        /// <summary>
        /// Seed ارتباط منوها با Permission ها (قرض‌الحسنه)
        /// </summary>
        private async Task SeedCashMenuPermissionsAsync()
        {
            var now = DateTime.UtcNow;

            // پیدا کردن نرم‌افزار قرض‌الحسنه
            var cashSoftware = await _context.tblSoftwares.FirstOrDefaultAsync(s => s.Code == "CASH");
            if (cashSoftware == null) return;

            var menus = await _context.tblMenus.Where(m => m.tblSoftwareId == cashSoftware.Id).ToListAsync();
            var permissions = await _context.tblPermissions.ToListAsync();

            foreach (var menu in menus)
            {
                var existingRelation = await _context.tblMenuPermissions
                    .FirstOrDefaultAsync(mp => mp.tblMenuId == menu.Id);
                
                if (existingRelation != null) continue;

                var matchedPermission = FindMatchingPermission(menu, permissions);
                if (matchedPermission == null)
                {
                    _logger.LogWarning("No permission found for Cash menu: {Title}", menu.Title);
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
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Cash menu-permissions seeding completed.");
        }

        /// <summary>
        /// پیدا کردن Permission مناسب برای هر منو
        /// </summary>
        private tblPermission? FindMatchingPermission(tblMenu menu, List<tblPermission> permissions)
        {
            var path = (menu.Path ?? "").ToLower();

            // TafsiliType
            if (path.Contains("tafsilitype/create")) return permissions.FirstOrDefault(p => p.Name == "TafsiliType.Create");
            if (path.Contains("tafsilitype/edit")) return permissions.FirstOrDefault(p => p.Name == "TafsiliType.Update");
            if (path.Contains("tafsilitype")) return permissions.FirstOrDefault(p => p.Name == "TafsiliType.Read");

            // AzaNoe
            if (path.Contains("azanoe/create")) return permissions.FirstOrDefault(p => p.Name == "AzaNoe.Create");
            if (path.Contains("azanoe/edit")) return permissions.FirstOrDefault(p => p.Name == "AzaNoe.Update");
            if (path.Contains("azanoe")) return permissions.FirstOrDefault(p => p.Name == "AzaNoe.Read");

            // Sarfasl - سرفصل‌ها
            if (path.Contains("sarfasl/create")) return permissions.FirstOrDefault(p => p.Name == "Sarfasl.Create");
            if (path.Contains("sarfasl/edit")) return permissions.FirstOrDefault(p => p.Name == "Sarfasl.Update");
            if (path.Contains("sarfasl/tree")) return permissions.FirstOrDefault(p => p.Name == "Sarfasl.Read");
            if (path.Contains("sarfasl")) return permissions.FirstOrDefault(p => p.Name == "Sarfasl.Read");

            // Default - اطلاعات پایه
            if (path.Contains("baseinfo") || path.Contains("settings"))
                return permissions.FirstOrDefault(p => p.Name == "Dashboard.Read");

            return null;
        }

        /// <summary>
        /// اعطای Permission های قرض‌الحسنه به گروه Admin
        /// </summary>
        private async Task SeedCashGrpPermissionsAsync()
        {
            var now = DateTime.UtcNow;

            var adminGroup = await _context.tblGrps.FirstOrDefaultAsync(g => g.Title == "Admin");
            if (adminGroup == null)
            {
                _logger.LogWarning("Admin group not found for Cash permissions");
                return;
            }

            // Permission های قرض‌الحسنه
            var cashPermissionNames = new[] 
            { 
                "TafsiliType.Read", "TafsiliType.Create", "TafsiliType.Update", "TafsiliType.Delete",
                "AzaNoe.Read", "AzaNoe.Create", "AzaNoe.Update", "AzaNoe.Delete",
                "Sarfasl.Read", "Sarfasl.Create", "Sarfasl.Update", "Sarfasl.Delete"
            };

            var cashPermissions = await _context.tblPermissions
                .Where(p => cashPermissionNames.Contains(p.Name))
                .ToListAsync();

            var existingPermissionIds = await _context.tblGrpPermissions
                .Where(gp => gp.tblGrpId == adminGroup.Id)
                .Select(gp => gp.tblPermissionId)
                .ToListAsync();

            var addedCount = 0;
            foreach (var permission in cashPermissions)
            {
                if (!existingPermissionIds.Contains(permission.Id))
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
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Added {Count} Cash permissions to Admin group", addedCount);
            }
        }

        /// <summary>
        /// Seed داده‌های پیش‌فرض انواع مشتری و انواع حوزه
        /// - انواع مشتریان: "دسته بندی عام" (General Classification)
        /// - انواع حوزه: "دسته بندی کل" (Total Classification)
        /// </summary>
        private async Task SeedDefaultTafsiliTypeAndAzaNoeAsync()
        {
            var now = DateTime.UtcNow;
            var persianDate = GetPersianDate(now);

            // ============================================
            // 1. ایجاد نوع مشتری پیش‌فرض: "دسته بندی عام"
            // ============================================
            var defaultTafsiliType = await _cashContext.tblTafsiliTypes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Title == "دسته بندی عام");

            if (defaultTafsiliType == null)
            {
                // محاسبه CodeTafsiliType بعدی
                var maxCode = await _cashContext.tblTafsiliTypes
                    .IgnoreQueryFilters()
                    .MaxAsync(t => (int?)t.CodeTafsiliType) ?? 0;

                defaultTafsiliType = new tblTafsiliType
                {
                    Title = "دسته بندی عام",
                    tblShobeId = 1, // شعبه پیش‌فرض
                    ParentId = null,
                    CodeTafsiliType = maxCode + 1,
                    IsActive = true,
                    IsDeleted = false,
                    TblUserGrpIdInsert = 1
                };
                defaultTafsiliType.SetZamanInsert(now);

                // محاسبه IntegrityHash
                defaultTafsiliType.IntegrityHash = _dataIntegrityService.ComputeIntegrityHash(defaultTafsiliType, TafsiliTypeSensitiveFields);
                
                _cashContext.tblTafsiliTypes.Add(defaultTafsiliType);
                await _cashContext.SaveChangesAsync();
                
                _logger.LogInformation("Created default TafsiliType: 'دسته بندی عام' with CodeTafsiliType={Code}, IntegrityHash computed", defaultTafsiliType.CodeTafsiliType);
            }
            else
            {
                _logger.LogInformation("Default TafsiliType 'دسته بندی عام' already exists with Id={Id}", defaultTafsiliType.Id);
            }

            // ============================================
            // 2. ایجاد حوزه پیش‌فرض: "دسته بندی کل"
            // ============================================
            var defaultAzaNoe = await _cashContext.tblAzaNoes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Title == "دسته بندی کل");

            if (defaultAzaNoe == null)
            {
                // محاسبه CodeHoze بعدی
                var maxCodeHoze = await _cashContext.tblAzaNoes
                    .IgnoreQueryFilters()
                    .Where(a => a.tblShobeId == 1)
                    .MaxAsync(a => (int?)a.CodeHoze) ?? 0;

                defaultAzaNoe = new tblAzaNoe
                {
                    Title = "دسته بندی کل",
                    tblShobeId = 1, // شعبه پیش‌فرض
                    tblTafsiliTypeId = defaultTafsiliType.Id, // اتصال به نوع مشتری پیش‌فرض
                    CodeHoze = maxCodeHoze + 1,
                    PishFarz = true, // این حوزه پیش‌فرض است
                    IsActive = true,
                    IsDeleted = false,
                    TblUserGrpIdInsert = 1
                };
                defaultAzaNoe.SetZamanInsert(now);

                // محاسبه IntegrityHash
                defaultAzaNoe.IntegrityHash = _dataIntegrityService.ComputeIntegrityHash(defaultAzaNoe, AzaNoeSensitiveFields);
                
                _cashContext.tblAzaNoes.Add(defaultAzaNoe);
                await _cashContext.SaveChangesAsync();
                
                _logger.LogInformation("Created default AzaNoe: 'دسته بندی کل' with CodeHoze={Code}, IntegrityHash computed, linked to TafsiliType Id={TafsiliTypeId}", 
                    defaultAzaNoe.CodeHoze, defaultAzaNoe.tblTafsiliTypeId);
            }
            else
            {
                _logger.LogInformation("Default AzaNoe 'دسته بندی کل' already exists with Id={Id}", defaultAzaNoe.Id);
            }

            _logger.LogInformation("Default TafsiliType and AzaNoe seeding completed.");
        }

        /// <summary>
        /// تبدیل تاریخ میلادی به شمسی
        /// </summary>
        private string GetPersianDate(DateTime date)
        {
            var pc = new System.Globalization.PersianCalendar();
            return $"{pc.GetYear(date):0000}/{pc.GetMonth(date):00}/{pc.GetDayOfMonth(date):00} " +
                   $"{pc.GetHour(date):00}:{pc.GetMinute(date):00}:{pc.GetSecond(date):00}";
        }
    }
}
