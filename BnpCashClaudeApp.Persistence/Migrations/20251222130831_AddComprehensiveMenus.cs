//using Microsoft.EntityFrameworkCore.Migrations;
//using System;

//#nullable disable

//namespace BnpCashClaudeApp.Persistence.Migrations
//{
//    /// <summary>
//    /// Migration برای ایجاد منوهای جامع سیستم
//    /// ============================================
//    /// این Migration تمام منوها و زیرمنوها را برای سیستم ایجاد می‌کند
//    /// و دسترسی گروه Admin به تمام منوها را تنظیم می‌کند
//    /// ============================================
//    /// </summary>
//    public partial class AddComprehensiveMenus : Migration
//    {
//        protected override void Up(MigrationBuilder migrationBuilder)
//        {
//            // ============================================
//            // تاریخ و زمان فعلی برای ZamanInsert
//            // ============================================
//            var now = DateTime.UtcNow;
//            var nowString = now.ToString("yyyy-MM-dd HH:mm:ss");

//            // ============================================
//            // ابتدا تمام منوهای قدیمی را پاک می‌کنیم
//            // ============================================
//            migrationBuilder.Sql("DELETE FROM tblGrpMenus");
//            migrationBuilder.Sql("DELETE FROM tblMenus");

//            // ============================================
//            // منوی 1: مدیریت کاربران (Users Management)
//            // ============================================
//            migrationBuilder.Sql($@"
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'مدیریت کاربران', NULL, NULL, 1, '{nowString}', 1)
//            ");

//            // زیرمنوهای مدیریت کاربران
//            migrationBuilder.Sql($@"
//                DECLARE @UsersMenuId INT = (SELECT Id FROM tblMenus WHERE Title = N'مدیریت کاربران');

//                -- لیست کاربران
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'لیست کاربران', 'api/Users', @UsersMenuId, 0, '{nowString}', 1);

//                -- تعریف کاربر جدید
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'تعریف کاربر جدید', 'api/Users/Create', @UsersMenuId, 0, '{nowString}', 1);

//                -- فعال/غیرفعال کردن کاربر
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'فعال/غیرفعال کردن کاربر', 'api/Users/Activate', @UsersMenuId, 0, '{nowString}', 1);

//                -- ریست رمز عبور
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'ریست رمز عبور', 'api/Users/ResetPassword', @UsersMenuId, 0, '{nowString}', 1);

//                -- باز کردن قفل کاربر
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'باز کردن قفل کاربر', 'api/Users/Unlock', @UsersMenuId, 0, '{nowString}', 1);

//                -- وضعیت قفل کاربر
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'وضعیت قفل کاربر', 'api/Users/LockoutStatus', @UsersMenuId, 0, '{nowString}', 1);
//            ");

//            // ============================================
//            // منوی 2: مدیریت گروه‌ها (Groups Management)
//            // ============================================
//            migrationBuilder.Sql($@"
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'مدیریت گروه‌ها', NULL, NULL, 1, '{nowString}', 1)
//            ");

//            // زیرمنوهای مدیریت گروه‌ها
//            migrationBuilder.Sql($@"
//                DECLARE @GrpMenuId INT = (SELECT Id FROM tblMenus WHERE Title = N'مدیریت گروه‌ها');

//                -- لیست گروه‌ها
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'لیست گروه‌ها', 'api/Grp', @GrpMenuId, 0, '{nowString}', 1);

//                -- تعریف گروه جدید
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'تعریف گروه جدید', 'api/Grp/Create', @GrpMenuId, 0, '{nowString}', 1);

//                -- ویرایش گروه
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'ویرایش گروه', 'api/Grp/Edit', @GrpMenuId, 0, '{nowString}', 1);
//            ");

//            // ============================================
//            // منوی 3: مدیریت منوها (Menus Management)
//            // ============================================
//            migrationBuilder.Sql($@"
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'مدیریت منوها', NULL, NULL, 1, '{nowString}', 1)
//            ");

//            // زیرمنوهای مدیریت منوها
//            migrationBuilder.Sql($@"
//                DECLARE @MenuMenuId INT = (SELECT Id FROM tblMenus WHERE Title = N'مدیریت منوها');

//                -- لیست منوها
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'لیست منوها', 'api/Menu', @MenuMenuId, 0, '{nowString}', 1);

//                -- تعریف منوی جدید
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'تعریف منوی جدید', 'api/Menu/Create', @MenuMenuId, 0, '{nowString}', 1);

//                -- ویرایش منو
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'ویرایش منو', 'api/Menu/Edit', @MenuMenuId, 0, '{nowString}', 1);

//                -- درخت منو
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'درخت منو', 'api/Menu/Tree', @MenuMenuId, 0, '{nowString}', 1);
//            ");

//            // ============================================
//            // منوی 4: مدیریت امنیت (Security Management)
//            // ============================================
//            migrationBuilder.Sql($@"
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'مدیریت امنیت', NULL, NULL, 1, '{nowString}', 1)
//            ");

//            // زیرمنوهای مدیریت امنیت
//            migrationBuilder.Sql($@"
//                DECLARE @SecurityMenuId INT = (SELECT Id FROM tblMenus WHERE Title = N'مدیریت امنیت');

//                -- تنظیمات امنیتی
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'تنظیمات امنیتی', 'api/Security/Settings', @SecurityMenuId, 0, '{nowString}', 1);

//                -- سیاست رمز عبور
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'سیاست رمز عبور', 'api/Security/PasswordPolicy', @SecurityMenuId, 0, '{nowString}', 1);

//                -- تنظیمات قفل حساب
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'تنظیمات قفل حساب', 'api/Security/LockoutPolicy', @SecurityMenuId, 0, '{nowString}', 1);

//                -- وضعیت امنیتی کاربران
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'وضعیت امنیتی کاربران', 'api/Security/UsersSecurityStatus', @SecurityMenuId, 0, '{nowString}', 1);

//                -- بررسی سلامت امنیتی
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'بررسی سلامت امنیتی', 'api/Security/HealthCheck', @SecurityMenuId, 0, '{nowString}', 1);

//                -- اطلاعات محیط
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'اطلاعات محیط', 'api/Security/EnvironmentInfo', @SecurityMenuId, 0, '{nowString}', 1);

//                -- بستن نشست‌های کاربر
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'بستن نشست‌های کاربر', 'api/Security/TerminateSessions', @SecurityMenuId, 0, '{nowString}', 1);
//            ");

//            // ============================================
//            // منوی 5: لاگ‌های امنیتی (Audit Logs)
//            // ============================================
//            migrationBuilder.Sql($@"
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'لاگ‌های امنیتی', NULL, NULL, 1, '{nowString}', 1)
//            ");

//            // زیرمنوهای لاگ‌های امنیتی
//            migrationBuilder.Sql($@"
//                DECLARE @AuditLogMenuId INT = (SELECT Id FROM tblMenus WHERE Title = N'لاگ‌های امنیتی');

//                -- مشاهده لاگ‌ها
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'مشاهده لاگ‌ها', 'api/AuditLog', @AuditLogMenuId, 0, '{nowString}', 1);

//                -- جستجوی لاگ
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'جستجوی لاگ', 'api/AuditLog/Search', @AuditLogMenuId, 0, '{nowString}', 1);

//                -- لاگ‌های امروز
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'لاگ‌های امروز', 'api/AuditLog/Today', @AuditLogMenuId, 0, '{nowString}', 1);

//                -- ورودهای ناموفق
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'ورودهای ناموفق', 'api/AuditLog/FailedLogins', @AuditLogMenuId, 0, '{nowString}', 1);

//                -- آمار امنیتی
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'آمار امنیتی', 'api/AuditLog/Statistics', @AuditLogMenuId, 0, '{nowString}', 1);

//                -- انواع رویدادها
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'انواع رویدادها', 'api/AuditLog/EventTypes', @AuditLogMenuId, 0, '{nowString}', 1);

//                -- لاگ‌های کاربر
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'لاگ‌های کاربر', 'api/AuditLog/User', @AuditLogMenuId, 0, '{nowString}', 1);
//            ");

//            // ============================================
//            // منوی 6: احراز هویت (Authentication)
//            // این منو برای دسترسی کاربران نیست و فقط برای مدیریت است
//            // ============================================
//            migrationBuilder.Sql($@"
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'احراز هویت', NULL, NULL, 1, '{nowString}', 1)
//            ");

//            // زیرمنوهای احراز هویت
//            migrationBuilder.Sql($@"
//                DECLARE @AuthMenuId INT = (SELECT Id FROM tblMenus WHERE Title = N'احراز هویت');

//                -- ورود
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'ورود', 'api/Auth/Login', @AuthMenuId, 0, '{nowString}', 1);

//                -- خروج
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'خروج', 'api/Auth/Logout', @AuthMenuId, 0, '{nowString}', 1);

//                -- خروج از همه دستگاه‌ها
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'خروج از همه دستگاه‌ها', 'api/Auth/LogoutAll', @AuthMenuId, 0, '{nowString}', 1);

//                -- تغییر رمز عبور
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'تغییر رمز عبور', 'api/Auth/ChangePassword', @AuthMenuId, 0, '{nowString}', 1);

//                -- سیاست رمز عبور (عمومی)
//                INSERT INTO tblMenus (Title, Path, ParentId, IsMenu, ZamanInsert, TblUserGrpIdInsert)
//                VALUES (N'سیاست رمز عبور', 'api/Auth/PasswordPolicy', @AuthMenuId, 0, '{nowString}', 1);
//            ");

//            // ============================================
//            // تنظیم دسترسی گروه Admin به تمام منوها
//            // ============================================
//            migrationBuilder.Sql(@"
//                -- پیدا کردن شناسه گروه Admin
//                DECLARE @AdminGrpId INT = (SELECT TOP 1 Id FROM tblGrps WHERE Title = N'Admin' OR Title = N'admin');

//                -- اگر گروه Admin وجود ندارد، آن را ایجاد کن
//                IF @AdminGrpId IS NULL
//                BEGIN
//                    INSERT INTO tblGrps (Title, GrpCode, ZamanInsert, TblUserGrpIdInsert)
//                    VALUES (N'Admin', 1, GETUTCDATE(), 1);
//                    SET @AdminGrpId = SCOPE_IDENTITY();
//                END

//                -- حذف دسترسی‌های قبلی گروه Admin
//                DELETE FROM tblGrpMenus WHERE tblGrpId = @AdminGrpId;

//                -- ایجاد دسترسی به تمام منوها برای گروه Admin
//                INSERT INTO tblGrpMenus (Id, tblGrpId, tblMenuId, Status, ZamanInsert, TblUserGrpIdInsert)
//                SELECT 0, @AdminGrpId, Id, 1, GETUTCDATE(), 1
//                FROM tblMenus;
//            ");
//        }

//        protected override void Down(MigrationBuilder migrationBuilder)
//        {
//            // ============================================
//            // بازگردانی - حذف منوها و دسترسی‌ها
//            // ============================================
//            migrationBuilder.Sql("DELETE FROM tblGrpMenus");
//            migrationBuilder.Sql("DELETE FROM tblMenus");
//        }
//    }
//}
