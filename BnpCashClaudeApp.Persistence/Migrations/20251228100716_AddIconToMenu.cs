using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIconToMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "tblMenus",
                type: "nvarchar(max)",
                nullable: true);

            // ============================================
            // به‌روزرسانی آیکون‌های منوها
            // ============================================
            
            // مدیریت کاربران
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'users' WHERE Title = N'مدیریت کاربران'");
            
            // زیرمنوهای مدیریت کاربران
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'user-search' WHERE Title = N'لیست کاربران'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'user-plus' WHERE Title = N'تعریف کاربر جدید'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'user-check' WHERE Title = N'فعال/غیرفعال کردن کاربر'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'key-round' WHERE Title = N'ریست رمز عبور'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'lock-open' WHERE Title = N'باز کردن قفل کاربر'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'message-square-lock' WHERE Title = N'وضعیت قفل کاربر'");
            
            // مدیریت گروه‌ها
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'square-chart-gantt' WHERE Title = N'مدیریت گروه‌ها'");
            
            // زیرمنوهای مدیریت گروه‌ها
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'list-ordered' WHERE Title = N'لیست گروه‌ها'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'folder-plus' WHERE Title = N'تعریف گروه جدید'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'file-pen-line' WHERE Title = N'ویرایش گروه'");
            
            // مدیریت منوها
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'layout-grid' WHERE Title = N'مدیریت منوها'");
            
            // زیرمنوهای مدیریت منوها
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'scroll-text' WHERE Title = N'لیست منوها'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'square-pen' WHERE Title = N'تعریف منوی جدید'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'file-sliders' WHERE Title = N'ویرایش منو'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'git-branch' WHERE Title = N'درخت منو'");
            
            // مدیریت امنیت
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'shield' WHERE Title = N'مدیریت امنیت'");
            
            // زیرمنوهای مدیریت امنیت
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'shield-check' WHERE Title = N'تنظیمات امنیتی'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'siren' WHERE Title = N'سیاست رمز عبور'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'wrench' WHERE Title = N'تنظیمات قفل حساب'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'shield-user' WHERE Title = N'وضعیت امنیتی کاربران'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'shield-alert' WHERE Title = N'بررسی سلامت امنیتی'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'server' WHERE Title = N'اطلاعات محیط'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'log-out' WHERE Title = N'بستن نشست‌های کاربر'");
            
            // لاگ‌های امنیتی
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'file-lock' WHERE Title = N'لاگ‌های امنیتی'");
            
            // زیرمنوهای لاگ‌های امنیتی
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'file-text' WHERE Title = N'مشاهده لاگ‌ها'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'file-search-corner' WHERE Title = N'جستجوی لاگ'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'calendar-search' WHERE Title = N'لاگ‌های امروز'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'circle-x' WHERE Title = N'ورودهای ناموفق'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'chart-bar-stacked' WHERE Title = N'آمار امنیتی'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'activity' WHERE Title = N'انواع رویدادها'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'scroll-text' WHERE Title = N'لاگ‌های کاربر'");
            
            // احراز هویت
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'fingerprint-pattern' WHERE Title = N'احراز هویت'");
            
            // زیرمنوهای احراز هویت
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'log-in' WHERE Title = N'ورود'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'log-out' WHERE Title = N'خروج'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'phone-off' WHERE Title = N'خروج از همه دستگاه‌ها'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'rotate-ccw-key' WHERE Title = N'تغییر رمز عبور'");
            migrationBuilder.Sql("UPDATE tblMenus SET Icon = 'siren' WHERE Title = N'سیاست رمز عبور' AND Path = 'api/Auth/PasswordPolicy'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Icon",
                table: "tblMenus");
        }
    }
}
