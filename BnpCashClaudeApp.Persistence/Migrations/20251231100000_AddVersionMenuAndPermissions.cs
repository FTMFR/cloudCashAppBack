using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <summary>
    /// Migration برای اضافه کردن منوها و Permission های مدیریت نسخه
    /// FPT_TUD_EXT.1.2 و FPT_TUD_EXT.1.3
    /// </summary>
    public partial class AddVersionMenuAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            // ============================================
            // 1. اضافه کردن Permission های مدیریت نسخه
            // ============================================
            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM tblPermissions WHERE Name = 'System.Version.Read')
                BEGIN
                    INSERT INTO tblPermissions (Name, Resource, Action, Description, DisplayOrder, IsActive, TblUserGrpIdInsert, ZamanInsert)
                    VALUES ('System.Version.Read', 'System', 'Version.Read', N'مشاهده نسخه سیستم', 110, 1, 1, '{now}')
                END
            ");

            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM tblPermissions WHERE Name = 'System.Version.BackendCheck')
                BEGIN
                    INSERT INTO tblPermissions (Name, Resource, Action, Description, DisplayOrder, IsActive, TblUserGrpIdInsert, ZamanInsert)
                    VALUES ('System.Version.BackendCheck', 'System', 'Version.BackendCheck', N'بررسی نسخه بک‌اند', 111, 1, 1, '{now}')
                END
            ");

            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM tblPermissions WHERE Name = 'System.Version.FrontendCheck')
                BEGIN
                    INSERT INTO tblPermissions (Name, Resource, Action, Description, DisplayOrder, IsActive, TblUserGrpIdInsert, ZamanInsert)
                    VALUES ('System.Version.FrontendCheck', 'System', 'Version.FrontendCheck', N'بررسی نسخه فرانت‌اند', 112, 1, 1, '{now}')
                END
            ");

            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM tblPermissions WHERE Name = 'System.Version.Info')
                BEGIN
                    INSERT INTO tblPermissions (Name, Resource, Action, Description, DisplayOrder, IsActive, TblUserGrpIdInsert, ZamanInsert)
                    VALUES ('System.Version.Info', 'System', 'Version.Info', N'مشاهده اطلاعات سیستم', 113, 1, 1, '{now}')
                END
            ");

            // ============================================
            // 2. اضافه کردن منوی اصلی مدیریت نسخه
            // ============================================
            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM tblMenus WHERE Path = 'api/Version')
                BEGIN
                    INSERT INTO tblMenus (Title, Path, Icon, IsMenu, ParentId, TblUserGrpIdInsert, ZamanInsert)
                    VALUES (N'مدیریت نسخه', 'api/Version', 'git-branch-plus', 1, NULL, 1, '{now}')
                END
            ");

            // ============================================
            // 3. اضافه کردن زیرمنوهای مدیریت نسخه
            // ============================================
            migrationBuilder.Sql($@"
                DECLARE @ParentId BIGINT = (SELECT Id FROM tblMenus WHERE Path = 'api/Version')
                
                IF @ParentId IS NOT NULL
                BEGIN
                    -- نسخه بک‌اند
                    IF NOT EXISTS (SELECT 1 FROM tblMenus WHERE Path = 'api/Version/current')
                    BEGIN
                        INSERT INTO tblMenus (Title, Path, Icon, IsMenu, ParentId, TblUserGrpIdInsert, ZamanInsert)
                        VALUES (N'نسخه بک‌اند', 'api/Version/current', 'server', 1, @ParentId, 1, '{now}')
                    END

                    -- بررسی نسخه بک‌اند
                    IF NOT EXISTS (SELECT 1 FROM tblMenus WHERE Path = 'api/Version/check')
                    BEGIN
                        INSERT INTO tblMenus (Title, Path, Icon, IsMenu, ParentId, TblUserGrpIdInsert, ZamanInsert)
                        VALUES (N'بررسی نسخه بک‌اند', 'api/Version/check', 'refresh-cw', 1, @ParentId, 1, '{now}')
                    END

                    -- اطلاعات سیستم
                    IF NOT EXISTS (SELECT 1 FROM tblMenus WHERE Path = 'api/Version/info')
                    BEGIN
                        INSERT INTO tblMenus (Title, Path, Icon, IsMenu, ParentId, TblUserGrpIdInsert, ZamanInsert)
                        VALUES (N'اطلاعات سیستم', 'api/Version/info', 'info', 1, @ParentId, 1, '{now}')
                    END

                    -- نسخه فرانت‌اند
                    IF NOT EXISTS (SELECT 1 FROM tblMenus WHERE Path = 'api/Version/frontend/current')
                    BEGIN
                        INSERT INTO tblMenus (Title, Path, Icon, IsMenu, ParentId, TblUserGrpIdInsert, ZamanInsert)
                        VALUES (N'نسخه فرانت‌اند', 'api/Version/frontend/current', 'monitor', 1, @ParentId, 1, '{now}')
                    END

                    -- بررسی نسخه فرانت‌اند
                    IF NOT EXISTS (SELECT 1 FROM tblMenus WHERE Path = 'api/Version/frontend/check')
                    BEGIN
                        INSERT INTO tblMenus (Title, Path, Icon, IsMenu, ParentId, TblUserGrpIdInsert, ZamanInsert)
                        VALUES (N'بررسی نسخه فرانت‌اند', 'api/Version/frontend/check', 'monitor-check', 1, @ParentId, 1, '{now}')
                    END
                END
            ");

            // ============================================
            // 4. اختصاص Permission ها به گروه Admin
            // ============================================
            migrationBuilder.Sql($@"
                DECLARE @AdminGrpId BIGINT = (SELECT Id FROM tblGrps WHERE Title = 'Admin')
                
                IF @AdminGrpId IS NOT NULL
                BEGIN
                    -- System.Version.Read
                    IF NOT EXISTS (SELECT 1 FROM tblGrpPermissions WHERE tblGrpId = @AdminGrpId AND tblPermissionId = (SELECT Id FROM tblPermissions WHERE Name = 'System.Version.Read'))
                    BEGIN
                        INSERT INTO tblGrpPermissions (tblGrpId, tblPermissionId, IsGranted, GrantedAt, GrantedBy, TblUserGrpIdInsert, ZamanInsert)
                        SELECT @AdminGrpId, Id, 1, '{now}', 1, 1, '{now}' FROM tblPermissions WHERE Name = 'System.Version.Read'
                    END

                    -- System.Version.BackendCheck
                    IF NOT EXISTS (SELECT 1 FROM tblGrpPermissions WHERE tblGrpId = @AdminGrpId AND tblPermissionId = (SELECT Id FROM tblPermissions WHERE Name = 'System.Version.BackendCheck'))
                    BEGIN
                        INSERT INTO tblGrpPermissions (tblGrpId, tblPermissionId, IsGranted, GrantedAt, GrantedBy, TblUserGrpIdInsert, ZamanInsert)
                        SELECT @AdminGrpId, Id, 1, '{now}', 1, 1, '{now}' FROM tblPermissions WHERE Name = 'System.Version.BackendCheck'
                    END

                    -- System.Version.FrontendCheck
                    IF NOT EXISTS (SELECT 1 FROM tblGrpPermissions WHERE tblGrpId = @AdminGrpId AND tblPermissionId = (SELECT Id FROM tblPermissions WHERE Name = 'System.Version.FrontendCheck'))
                    BEGIN
                        INSERT INTO tblGrpPermissions (tblGrpId, tblPermissionId, IsGranted, GrantedAt, GrantedBy, TblUserGrpIdInsert, ZamanInsert)
                        SELECT @AdminGrpId, Id, 1, '{now}', 1, 1, '{now}' FROM tblPermissions WHERE Name = 'System.Version.FrontendCheck'
                    END

                    -- System.Version.Info
                    IF NOT EXISTS (SELECT 1 FROM tblGrpPermissions WHERE tblGrpId = @AdminGrpId AND tblPermissionId = (SELECT Id FROM tblPermissions WHERE Name = 'System.Version.Info'))
                    BEGIN
                        INSERT INTO tblGrpPermissions (tblGrpId, tblPermissionId, IsGranted, GrantedAt, GrantedBy, TblUserGrpIdInsert, ZamanInsert)
                        SELECT @AdminGrpId, Id, 1, '{now}', 1, 1, '{now}' FROM tblPermissions WHERE Name = 'System.Version.Info'
                    END
                END
            ");

            // ============================================
            // 5. اختصاص Permission ها به منوها
            // ============================================
            migrationBuilder.Sql($@"
                -- منوی اصلی مدیریت نسخه
                IF NOT EXISTS (SELECT 1 FROM tblMenuPermissions WHERE tblMenuId = (SELECT Id FROM tblMenus WHERE Path = 'api/Version'))
                BEGIN
                    INSERT INTO tblMenuPermissions (tblMenuId, tblPermissionId, IsRequired, TblUserGrpIdInsert, ZamanInsert)
                    SELECT m.Id, p.Id, 1, 1, '{now}'
                    FROM tblMenus m, tblPermissions p
                    WHERE m.Path = 'api/Version' AND p.Name = 'System.Version.Read'
                END

                -- نسخه بک‌اند
                IF NOT EXISTS (SELECT 1 FROM tblMenuPermissions WHERE tblMenuId = (SELECT Id FROM tblMenus WHERE Path = 'api/Version/current'))
                BEGIN
                    INSERT INTO tblMenuPermissions (tblMenuId, tblPermissionId, IsRequired, TblUserGrpIdInsert, ZamanInsert)
                    SELECT m.Id, p.Id, 1, 1, '{now}'
                    FROM tblMenus m, tblPermissions p
                    WHERE m.Path = 'api/Version/current' AND p.Name = 'System.Version.Read'
                END

                -- بررسی نسخه بک‌اند
                IF NOT EXISTS (SELECT 1 FROM tblMenuPermissions WHERE tblMenuId = (SELECT Id FROM tblMenus WHERE Path = 'api/Version/check'))
                BEGIN
                    INSERT INTO tblMenuPermissions (tblMenuId, tblPermissionId, IsRequired, TblUserGrpIdInsert, ZamanInsert)
                    SELECT m.Id, p.Id, 1, 1, '{now}'
                    FROM tblMenus m, tblPermissions p
                    WHERE m.Path = 'api/Version/check' AND p.Name = 'System.Version.BackendCheck'
                END

                -- اطلاعات سیستم
                IF NOT EXISTS (SELECT 1 FROM tblMenuPermissions WHERE tblMenuId = (SELECT Id FROM tblMenus WHERE Path = 'api/Version/info'))
                BEGIN
                    INSERT INTO tblMenuPermissions (tblMenuId, tblPermissionId, IsRequired, TblUserGrpIdInsert, ZamanInsert)
                    SELECT m.Id, p.Id, 1, 1, '{now}'
                    FROM tblMenus m, tblPermissions p
                    WHERE m.Path = 'api/Version/info' AND p.Name = 'System.Version.Info'
                END

                -- نسخه فرانت‌اند
                IF NOT EXISTS (SELECT 1 FROM tblMenuPermissions WHERE tblMenuId = (SELECT Id FROM tblMenus WHERE Path = 'api/Version/frontend/current'))
                BEGIN
                    INSERT INTO tblMenuPermissions (tblMenuId, tblPermissionId, IsRequired, TblUserGrpIdInsert, ZamanInsert)
                    SELECT m.Id, p.Id, 1, 1, '{now}'
                    FROM tblMenus m, tblPermissions p
                    WHERE m.Path = 'api/Version/frontend/current' AND p.Name = 'System.Version.Read'
                END

                -- بررسی نسخه فرانت‌اند
                IF NOT EXISTS (SELECT 1 FROM tblMenuPermissions WHERE tblMenuId = (SELECT Id FROM tblMenus WHERE Path = 'api/Version/frontend/check'))
                BEGIN
                    INSERT INTO tblMenuPermissions (tblMenuId, tblPermissionId, IsRequired, TblUserGrpIdInsert, ZamanInsert)
                    SELECT m.Id, p.Id, 1, 1, '{now}'
                    FROM tblMenus m, tblPermissions p
                    WHERE m.Path = 'api/Version/frontend/check' AND p.Name = 'System.Version.FrontendCheck'
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // حذف ارتباط منو-Permission
            migrationBuilder.Sql(@"
                DELETE FROM tblMenuPermissions WHERE tblMenuId IN (SELECT Id FROM tblMenus WHERE Path LIKE 'api/Version%')
            ");

            // حذف ارتباط گروه-Permission
            migrationBuilder.Sql(@"
                DELETE FROM tblGrpPermissions WHERE tblPermissionId IN (SELECT Id FROM tblPermissions WHERE Name LIKE 'System.Version.%')
            ");

            // حذف زیرمنوها
            migrationBuilder.Sql(@"
                DELETE FROM tblMenus WHERE Path LIKE 'api/Version/%'
            ");

            // حذف منوی اصلی
            migrationBuilder.Sql(@"
                DELETE FROM tblMenus WHERE Path = 'api/Version'
            ");

            // حذف Permission ها
            migrationBuilder.Sql(@"
                DELETE FROM tblPermissions WHERE Name LIKE 'System.Version.%'
            ");
        }
    }
}

