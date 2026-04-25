using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixGrpMenuPermissionKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // این migration فقط برای هماهنگ کردن پیکربندی EF Core با دیتابیس است
            // تغییرات ساختاری قبلاً در migration FixIdentityForPermissionJoinTables اعمال شده است
            // بنابراین این migration خالی است و فقط پیکربندی EF Core را هماهنگ می‌کند
            
            // بررسی و ایجاد Index های یکتا در صورت عدم وجود (برای اطمینان)
            migrationBuilder.Sql(@"
-- ============================================
-- Ensure unique indexes exist (if not already created by previous migration)
-- ============================================
IF OBJECT_ID('dbo.tblMenuPermissions') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tblMenuPermissions') AND name = 'IX_tblMenuPermissions_tblMenuId_tblPermissionId')
        CREATE UNIQUE INDEX IX_tblMenuPermissions_tblMenuId_tblPermissionId ON dbo.tblMenuPermissions(tblMenuId, tblPermissionId);
END

IF OBJECT_ID('dbo.tblGrpPermissions') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tblGrpPermissions') AND name = 'IX_tblGrpPermissions_tblGrpId_tblPermissionId')
        CREATE UNIQUE INDEX IX_tblGrpPermissions_tblGrpId_tblPermissionId ON dbo.tblGrpPermissions(tblGrpId, tblPermissionId);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // این migration فقط پیکربندی EF Core را هماهنگ می‌کند
            // تغییرات ساختاری قابل برگشت نیستند (به دلیل IDENTITY)
            // بنابراین Down method خالی است
        }
    }
}
