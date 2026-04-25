using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <summary>
    /// Fix: Ensure Id columns on permission join tables are IDENTITY.
    /// Root cause: DB was created with PK on Id but Id was NOT identity -> inserts used Id=0.
    /// </summary>
    [DbContext(typeof(NavigationDbContext))]
    [Migration("20251228195500_FixIdentityForPermissionJoinTables")]
    public partial class FixIdentityForPermissionJoinTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- ============================================
-- Fix tblGrpPermissions.Id -> IDENTITY(1,1)
-- ============================================
IF OBJECT_ID('dbo.tblGrpPermissions') IS NOT NULL
BEGIN
    DECLARE @isIdentityGrp INT = CONVERT(INT, COLUMNPROPERTY(OBJECT_ID('dbo.tblGrpPermissions'),'Id','IsIdentity'));
    IF (@isIdentityGrp = 0)
    BEGIN
        -- drop PK
        DECLARE @pkGrp SYSNAME;
        SELECT @pkGrp = kc.name
        FROM sys.key_constraints kc
        WHERE kc.parent_object_id = OBJECT_ID('dbo.tblGrpPermissions') AND kc.type = 'PK';
        IF @pkGrp IS NOT NULL
            EXEC('ALTER TABLE dbo.tblGrpPermissions DROP CONSTRAINT [' + @pkGrp + ']');

        -- drop default constraint on Id (if any)
        DECLARE @dfGrp SYSNAME;
        SELECT @dfGrp = dc.name
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID('dbo.tblGrpPermissions') AND c.name = 'Id';
        IF @dfGrp IS NOT NULL
            EXEC('ALTER TABLE dbo.tblGrpPermissions DROP CONSTRAINT [' + @dfGrp + ']');

        -- drop Id and recreate as identity
        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tblGrpPermissions') AND name = 'Id')
            ALTER TABLE dbo.tblGrpPermissions DROP COLUMN Id;

        ALTER TABLE dbo.tblGrpPermissions ADD Id bigint IDENTITY(1,1) NOT NULL;

        -- add PK
        ALTER TABLE dbo.tblGrpPermissions ADD CONSTRAINT PK_tblGrpPermissions PRIMARY KEY (Id);
    END

    -- ensure unique index on (tblGrpId, tblPermissionId)
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tblGrpPermissions') AND name = 'IX_tblGrpPermissions_tblGrpId_tblPermissionId')
        CREATE UNIQUE INDEX IX_tblGrpPermissions_tblGrpId_tblPermissionId ON dbo.tblGrpPermissions(tblGrpId, tblPermissionId);

    -- ensure index on tblPermissionId
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tblGrpPermissions') AND name = 'IX_tblGrpPermissions_tblPermissionId')
        CREATE INDEX IX_tblGrpPermissions_tblPermissionId ON dbo.tblGrpPermissions(tblPermissionId);
END
");

            migrationBuilder.Sql(@"
-- ============================================
-- Fix tblMenuPermissions.Id -> IDENTITY(1,1)
-- ============================================
IF OBJECT_ID('dbo.tblMenuPermissions') IS NOT NULL
BEGIN
    DECLARE @isIdentityMenu INT = CONVERT(INT, COLUMNPROPERTY(OBJECT_ID('dbo.tblMenuPermissions'),'Id','IsIdentity'));
    IF (@isIdentityMenu = 0)
    BEGIN
        -- drop PK
        DECLARE @pkMenu SYSNAME;
        SELECT @pkMenu = kc.name
        FROM sys.key_constraints kc
        WHERE kc.parent_object_id = OBJECT_ID('dbo.tblMenuPermissions') AND kc.type = 'PK';
        IF @pkMenu IS NOT NULL
            EXEC('ALTER TABLE dbo.tblMenuPermissions DROP CONSTRAINT [' + @pkMenu + ']');

        -- drop default constraint on Id (if any)
        DECLARE @dfMenu SYSNAME;
        SELECT @dfMenu = dc.name
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID('dbo.tblMenuPermissions') AND c.name = 'Id';
        IF @dfMenu IS NOT NULL
            EXEC('ALTER TABLE dbo.tblMenuPermissions DROP CONSTRAINT [' + @dfMenu + ']');

        -- drop Id and recreate as identity
        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tblMenuPermissions') AND name = 'Id')
            ALTER TABLE dbo.tblMenuPermissions DROP COLUMN Id;

        ALTER TABLE dbo.tblMenuPermissions ADD Id bigint IDENTITY(1,1) NOT NULL;

        -- add PK
        ALTER TABLE dbo.tblMenuPermissions ADD CONSTRAINT PK_tblMenuPermissions PRIMARY KEY (Id);
    END

    -- ensure unique index on (tblMenuId, tblPermissionId)
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tblMenuPermissions') AND name = 'IX_tblMenuPermissions_tblMenuId_tblPermissionId')
        CREATE UNIQUE INDEX IX_tblMenuPermissions_tblMenuId_tblPermissionId ON dbo.tblMenuPermissions(tblMenuId, tblPermissionId);

    -- ensure index on tblPermissionId
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.tblMenuPermissions') AND name = 'IX_tblMenuPermissions_tblPermissionId')
        CREATE INDEX IX_tblMenuPermissions_tblPermissionId ON dbo.tblMenuPermissions(tblPermissionId);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op (not safe to revert identity change automatically).
        }
    }
}


