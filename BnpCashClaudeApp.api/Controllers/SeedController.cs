//using BnpCashClaudeApp.api.Attributes;
//using BnpCashClaudeApp.Infrastructure.Services;
//using BnpCashClaudeApp.Persistence.Migrations;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.RateLimiting;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace BnpCashClaudeApp.api.Controllers
//{
//    /// <summary>
//    /// کنترلر برای Seed کردن داده‌های اولیه
//    /// ============================================
//    /// این Controller فقط در Development باید استفاده شود
//    /// در محیط Production این endpoint ها را غیرفعال کنید
//    /// ============================================
//    /// </summary>
//    [ApiController]
//    [Route("api/[controller]")]
//    [Authorize]
//    [EnableRateLimiting("ApiPolicy")]
//    public class SeedController : ControllerBase
//    {
//        private readonly DataSeeder _dataSeeder;
//        private readonly CashDataSeeder _cashDataSeeder;
//        private readonly NavigationDbContext _context;
//        private readonly IWebHostEnvironment _environment;

//        public SeedController(
//            DataSeeder dataSeeder,
//            CashDataSeeder cashDataSeeder,
//            NavigationDbContext context,
//            IWebHostEnvironment environment)
//        {
//            _dataSeeder = dataSeeder;
//            _cashDataSeeder = cashDataSeeder;
//            _context = context;
//            _environment = environment;
//        }


//        /// <summary>
//        /// Seed کردن تمام داده‌های اولیه
//        /// ============================================
//        /// این endpoint تمام داده‌های اولیه را Seed می‌کند:
//        /// 1. کاربر Admin
//        /// 2. Permission های سیستم (FDP_ACF)
//        /// 3. اعطای Permission ها به گروه Admin
//        /// ============================================
//        /// </summary>
//        [HttpPost("all")]
//        [RequirePermission("Security.Manage")]
//        public async Task<IActionResult> SeedAll()
//        {
//            // SECURITY HARDENING: seed endpoints must never run outside Development.
//            var environmentRestriction = EnsureDevelopmentOnly();
//            if (environmentRestriction != null)
//                return environmentRestriction;

//            try
//            {
//                await _dataSeeder.SeedAllAsync();

//                return Ok(new
//                {
//                    success = true,
//                    message = "تمام داده‌های اولیه با موفقیت ایجاد شدند.",
//                    credentials = new
//                    {
//                        username = "admin",
//                        //password = "123!@#"
//                    },
//                });
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new
//                {
//                    success = false,
//                    message = "خطا در Seed کردن داده‌ها",
//                    error = ex.Message
//                });
//            }
//        }

//        /// <summary>
//        /// Seed کردن داده‌های اولیه نرم‌افزار قرض‌الحسنه
//        /// ============================================
//        /// این endpoint داده‌های اولیه مربوط به قرض‌الحسنه را Seed می‌کند:
//        /// 1. منوهای قرض‌الحسنه (اطلاعات پایه، تنظیمات اولیه، انواع مشتریان، انواع حوزه)
//        /// 2. Permission های قرض‌الحسنه
//        /// 3. اعطای Permission ها به گروه Admin
//        /// ============================================
//        /// </summary>
//        [HttpPost("cash")]
//        [RequirePermission("Security.Manage")]
//        public async Task<IActionResult> SeedCash()
//        {
//            // SECURITY HARDENING: seed endpoints must never run outside Development.
//            var environmentRestriction = EnsureDevelopmentOnly();
//            if (environmentRestriction != null)
//                return environmentRestriction;

//            try
//            {
//                await _cashDataSeeder.SeedAllAsync();

//                return Ok(new
//                {
//                    success = true,
//                    message = "داده‌های اولیه قرض‌الحسنه با موفقیت ایجاد شدند.",
//                    seededItems = new[]
//                    {
//                        "منوهای قرض‌الحسنه (اطلاعات پایه، تنظیمات اولیه، انواع مشتریان، انواع حوزه)",
//                        "Permission های TafsiliType و AzaNoe",
//                        "اعطای Permission ها به گروه Admin"
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new
//                {
//                    success = false,
//                    message = "خطا در Seed کردن داده‌های قرض‌الحسنه",
//                    error = ex.Message
//                });
//            }
//        }

//        /// <summary>
//        /// حذف منوهای اضافی قرض‌الحسنه
//        /// ============================================
//        /// این endpoint منوهای اضافی را از دیتابیس حذف می‌کند:
//        /// - لیست انواع مشتریان
//        /// - نمایش درختی انواع مشتریان
//        /// - لیست انواع حوزه
//        /// ============================================
//        /// </summary>
//        //[HttpDelete("cash/cleanup-extra-menus")]
//        //public async Task<IActionResult> CleanupExtraCashMenus()
//        //{
//        //    try
//        //    {
//        //        // لیست Path های منوهای اضافی که باید حذف شوند
//        //        var extraMenuPaths = new[]
//        //        {
//        //            "api/TafsiliType/List",
//        //            "api/TafsiliType/Tree",
//        //            "api/AzaNoe/List"
//        //        };

//        //        var deletedMenus = new List<string>();
//        //        var deletedPermissions = 0;

//        //        foreach (var path in extraMenuPaths)
//        //        {
//        //            var menu = await _context.tblMenus.FirstOrDefaultAsync(m => m.Path == path);
//        //            if (menu != null)
//        //            {
//        //                // حذف MenuPermissions مرتبط
//        //                var menuPermissions = await _context.tblMenuPermissions
//        //                    .Where(mp => mp.tblMenuId == menu.Id)
//        //                    .ToListAsync();
                        
//        //                if (menuPermissions.Any())
//        //                {
//        //                    _context.tblMenuPermissions.RemoveRange(menuPermissions);
//        //                    deletedPermissions += menuPermissions.Count;
//        //                }

//        //                // حذف منو
//        //                _context.tblMenus.Remove(menu);
//        //                deletedMenus.Add($"{menu.Title} ({menu.Path})");
//        //            }
//        //        }

//        //        await _context.SaveChangesAsync();

//        //        return Ok(new
//        //        {
//        //            success = true,
//        //            message = "منوهای اضافی با موفقیت حذف شدند.",
//        //            deletedMenus = deletedMenus,
//        //            deletedMenuPermissionsCount = deletedPermissions
//        //        });
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        return BadRequest(new
//        //        {
//        //            success = false,
//        //            message = "خطا در حذف منوهای اضافی",
//        //            error = ex.Message
//        //        });
//        //    }
//        //}

//        /// <summary>
//        /// Re-seed کردن MenuPermissions
//        /// ============================================
//        /// این endpoint ابتدا تمام MenuPermissions موجود را پاک می‌کند
//        /// سپس دوباره آنها را بر اساس منطق جدید ایجاد می‌کند
//        /// ============================================
//        /// </summary>
//        [HttpPost("reseed-menu-permissions")]
//        [RequirePermission("Security.Manage")]
//        public async Task<IActionResult> ReseedMenuPermissions()
//        {
//            // SECURITY HARDENING: seed endpoints must never run outside Development.
//            var environmentRestriction = EnsureDevelopmentOnly();
//            if (environmentRestriction != null)
//                return environmentRestriction;

//            try
//            {
//                // حذف تمام MenuPermissions موجود
//                var existingMenuPermissions = await _context.tblMenuPermissions.ToListAsync();
//                var deletedCount = existingMenuPermissions.Count;
                
//                if (existingMenuPermissions.Any())
//                {
//                    _context.tblMenuPermissions.RemoveRange(existingMenuPermissions);
//                    await _context.SaveChangesAsync();
//                }

//                // Re-seed
//                await _dataSeeder.SeedAllAsync();

//                // شمارش MenuPermissions جدید
//                var newCount = await _context.tblMenuPermissions.CountAsync();

//                return Ok(new
//                {
//                    success = true,
//                    message = "MenuPermissions با موفقیت دوباره ایجاد شدند.",
//                    deletedCount = deletedCount,
//                    newCount = newCount
//                });
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new
//                {
//                    success = false,
//                    message = "خطا در Re-seed کردن MenuPermissions",
//                    error = ex.Message
//                });
//            }
//        }

//        private IActionResult? EnsureDevelopmentOnly()
//        {
//            // SECURITY HARDENING: explicit runtime guard in addition to auth/permission checks.
//            if (!_environment.IsDevelopment())
//            {
//                return StatusCode(403, new
//                {
//                    success = false,
//                    message = "Seed endpoints are disabled outside Development environment."
//                });
//            }

//            return null;
//        }
//    }
//}
