using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIdToLongAndAddPublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================
            // مرحله 1: حذف تمام Foreign Keys
            // ============================================
            
            // FK_tblUserGrps_tblUsers_tblUserId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tblUserGrps_tblUsers_tblUserId')
                    ALTER TABLE [tblUserGrps] DROP CONSTRAINT [FK_tblUserGrps_tblUsers_tblUserId];
            ");
            
            // FK_tblUserGrps_tblGrps_tblGrpId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tblUserGrps_tblGrps_tblGrpId')
                    ALTER TABLE [tblUserGrps] DROP CONSTRAINT [FK_tblUserGrps_tblGrps_tblGrpId];
            ");
            
            // FK_tblGrpPermissions_tblGrps_tblGrpId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tblGrpPermissions_tblGrps_tblGrpId')
                    ALTER TABLE [tblGrpPermissions] DROP CONSTRAINT [FK_tblGrpPermissions_tblGrps_tblGrpId];
            ");
            
            // FK_tblGrpPermissions_tblPermissions_tblPermissionId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tblGrpPermissions_tblPermissions_tblPermissionId')
                    ALTER TABLE [tblGrpPermissions] DROP CONSTRAINT [FK_tblGrpPermissions_tblPermissions_tblPermissionId];
            ");
            
            // FK_tblMenuPermissions_tblMenus_tblMenuId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tblMenuPermissions_tblMenus_tblMenuId')
                    ALTER TABLE [tblMenuPermissions] DROP CONSTRAINT [FK_tblMenuPermissions_tblMenus_tblMenuId];
            ");
            
            // FK_tblMenuPermissions_tblPermissions_tblPermissionId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tblMenuPermissions_tblPermissions_tblPermissionId')
                    ALTER TABLE [tblMenuPermissions] DROP CONSTRAINT [FK_tblMenuPermissions_tblPermissions_tblPermissionId];
            ");
            
            // FK_tblMenus_tblMenus_ParentId (Self-referencing FK)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tblMenus_tblMenus_ParentId')
                    ALTER TABLE [tblMenus] DROP CONSTRAINT [FK_tblMenus_tblMenus_ParentId];
            ");

            // ============================================
            // مرحله 1.5: حذف Indexes
            // ============================================
            
            // IX_tblUserGrps_tblUserId_tblGrpId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tblUserGrps_tblUserId_tblGrpId')
                    DROP INDEX [IX_tblUserGrps_tblUserId_tblGrpId] ON [tblUserGrps];
            ");
            
            // IX_tblUserGrps_tblGrpId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tblUserGrps_tblGrpId')
                    DROP INDEX [IX_tblUserGrps_tblGrpId] ON [tblUserGrps];
            ");
            
            // IX_tblGrpPermissions_tblGrpId_tblPermissionId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tblGrpPermissions_tblGrpId_tblPermissionId')
                    DROP INDEX [IX_tblGrpPermissions_tblGrpId_tblPermissionId] ON [tblGrpPermissions];
            ");
            
            // IX_tblGrpPermissions_tblPermissionId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tblGrpPermissions_tblPermissionId')
                    DROP INDEX [IX_tblGrpPermissions_tblPermissionId] ON [tblGrpPermissions];
            ");
            
            // IX_tblMenuPermissions_tblMenuId_tblPermissionId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tblMenuPermissions_tblMenuId_tblPermissionId')
                    DROP INDEX [IX_tblMenuPermissions_tblMenuId_tblPermissionId] ON [tblMenuPermissions];
            ");
            
            // IX_tblMenuPermissions_tblPermissionId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tblMenuPermissions_tblPermissionId')
                    DROP INDEX [IX_tblMenuPermissions_tblPermissionId] ON [tblMenuPermissions];
            ");
            
            // IX_tblMenus_ParentId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_tblMenus_ParentId')
                    DROP INDEX [IX_tblMenus_ParentId] ON [tblMenus];
            ");
            
            // IX_RefreshTokens_UserId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RefreshTokens_UserId')
                    DROP INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens];
            ");
            
            // IX_RefreshTokens_UserId_IsRevoked_IsUsed
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RefreshTokens_UserId_IsRevoked_IsUsed')
                    DROP INDEX [IX_RefreshTokens_UserId_IsRevoked_IsUsed] ON [RefreshTokens];
            ");
            
            // IX_RefreshTokens_Token
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RefreshTokens_Token')
                    DROP INDEX [IX_RefreshTokens_Token] ON [RefreshTokens];
            ");
            
            // IX_PasswordHistory_UserId
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PasswordHistory_UserId')
                    DROP INDEX [IX_PasswordHistory_UserId] ON [PasswordHistory];
            ");
            
            // IX_PasswordHistory_UserId_PasswordHash
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PasswordHistory_UserId_PasswordHash')
                    DROP INDEX [IX_PasswordHistory_UserId_PasswordHash] ON [PasswordHistory];
            ");

            // ============================================
            // مرحله 2: حذف Primary Keys
            // ============================================
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_tblUsers')
                    ALTER TABLE [tblUsers] DROP CONSTRAINT [PK_tblUsers];
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_tblGrps')
                    ALTER TABLE [tblGrps] DROP CONSTRAINT [PK_tblGrps];
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_tblUserGrps')
                    ALTER TABLE [tblUserGrps] DROP CONSTRAINT [PK_tblUserGrps];
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_tblPermissions')
                    ALTER TABLE [tblPermissions] DROP CONSTRAINT [PK_tblPermissions];
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_tblGrpPermissions')
                    ALTER TABLE [tblGrpPermissions] DROP CONSTRAINT [PK_tblGrpPermissions];
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_tblMenus')
                    ALTER TABLE [tblMenus] DROP CONSTRAINT [PK_tblMenus];
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_tblMenuPermissions')
                    ALTER TABLE [tblMenuPermissions] DROP CONSTRAINT [PK_tblMenuPermissions];
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_SecuritySettings')
                    ALTER TABLE [SecuritySettings] DROP CONSTRAINT [PK_SecuritySettings];
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_RefreshTokens')
                    ALTER TABLE [RefreshTokens] DROP CONSTRAINT [PK_RefreshTokens];
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_PasswordHistory')
                    ALTER TABLE [PasswordHistory] DROP CONSTRAINT [PK_PasswordHistory];
            ");

            // ============================================
            // مرحله 3: تغییر نوع ستون‌های Id از int به bigint
            // ============================================
            
            // tblUsers
            migrationBuilder.Sql(@"ALTER TABLE [tblUsers] ALTER COLUMN [Id] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblUsers] ALTER COLUMN [TblUserGrpIdInsert] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblUsers] ALTER COLUMN [TblUserGrpIdLastEdit] bigint NULL;");
            
            // tblGrps
            migrationBuilder.Sql(@"ALTER TABLE [tblGrps] ALTER COLUMN [Id] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrps] ALTER COLUMN [TblUserGrpIdInsert] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrps] ALTER COLUMN [TblUserGrpIdLastEdit] bigint NULL;");
            
            // tblUserGrps
            migrationBuilder.Sql(@"ALTER TABLE [tblUserGrps] ALTER COLUMN [Id] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblUserGrps] ALTER COLUMN [tblUserId] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblUserGrps] ALTER COLUMN [tblGrpId] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblUserGrps] ALTER COLUMN [TblUserGrpIdInsert] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblUserGrps] ALTER COLUMN [TblUserGrpIdLastEdit] bigint NULL;");
            
            // tblPermissions
            migrationBuilder.Sql(@"ALTER TABLE [tblPermissions] ALTER COLUMN [Id] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblPermissions] ALTER COLUMN [TblUserGrpIdInsert] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblPermissions] ALTER COLUMN [TblUserGrpIdLastEdit] bigint NULL;");
            
            // tblGrpPermissions
            migrationBuilder.Sql(@"ALTER TABLE [tblGrpPermissions] ALTER COLUMN [Id] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrpPermissions] ALTER COLUMN [tblGrpId] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrpPermissions] ALTER COLUMN [tblPermissionId] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrpPermissions] ALTER COLUMN [GrantedBy] bigint NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrpPermissions] ALTER COLUMN [TblUserGrpIdInsert] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrpPermissions] ALTER COLUMN [TblUserGrpIdLastEdit] bigint NULL;");
            
            // tblMenus
            migrationBuilder.Sql(@"ALTER TABLE [tblMenus] ALTER COLUMN [Id] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenus] ALTER COLUMN [ParentId] bigint NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenus] ALTER COLUMN [TblUserGrpIdInsert] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenus] ALTER COLUMN [TblUserGrpIdLastEdit] bigint NULL;");
            
            // tblMenuPermissions
            migrationBuilder.Sql(@"ALTER TABLE [tblMenuPermissions] ALTER COLUMN [Id] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenuPermissions] ALTER COLUMN [tblMenuId] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenuPermissions] ALTER COLUMN [tblPermissionId] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenuPermissions] ALTER COLUMN [TblUserGrpIdInsert] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenuPermissions] ALTER COLUMN [TblUserGrpIdLastEdit] bigint NULL;");
            
            // SecuritySettings
            migrationBuilder.Sql(@"ALTER TABLE [SecuritySettings] ALTER COLUMN [Id] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [SecuritySettings] ALTER COLUMN [TblUserGrpIdInsert] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [SecuritySettings] ALTER COLUMN [TblUserGrpIdLastEdit] bigint NULL;");
            
            // RefreshTokens
            migrationBuilder.Sql(@"ALTER TABLE [RefreshTokens] ALTER COLUMN [Id] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [RefreshTokens] ALTER COLUMN [UserId] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [RefreshTokens] ALTER COLUMN [TblUserGrpIdInsert] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [RefreshTokens] ALTER COLUMN [TblUserGrpIdLastEdit] bigint NULL;");
            
            // PasswordHistory
            migrationBuilder.Sql(@"ALTER TABLE [PasswordHistory] ALTER COLUMN [Id] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [PasswordHistory] ALTER COLUMN [UserId] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [PasswordHistory] ALTER COLUMN [TblUserGrpIdInsert] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [PasswordHistory] ALTER COLUMN [TblUserGrpIdLastEdit] bigint NULL;");

            // ============================================
            // مرحله 4: اضافه کردن ستون PublicId
            // ============================================

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "tblUsers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "tblUserGrps",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "tblPermissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "tblMenus",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "tblMenuPermissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "tblGrps",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "tblGrpPermissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "SecuritySettings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "RefreshTokens",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "PasswordHistory",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            // ============================================
            // مرحله 5: ایجاد مجدد Primary Keys
            // ============================================
            
            migrationBuilder.Sql(@"ALTER TABLE [tblUsers] ADD CONSTRAINT [PK_tblUsers] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrps] ADD CONSTRAINT [PK_tblGrps] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [tblUserGrps] ADD CONSTRAINT [PK_tblUserGrps] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [tblPermissions] ADD CONSTRAINT [PK_tblPermissions] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrpPermissions] ADD CONSTRAINT [PK_tblGrpPermissions] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenus] ADD CONSTRAINT [PK_tblMenus] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenuPermissions] ADD CONSTRAINT [PK_tblMenuPermissions] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [SecuritySettings] ADD CONSTRAINT [PK_SecuritySettings] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [RefreshTokens] ADD CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [PasswordHistory] ADD CONSTRAINT [PK_PasswordHistory] PRIMARY KEY ([Id]);");

            // ============================================
            // مرحله 6: ایجاد مجدد Foreign Keys
            // ============================================
            
            migrationBuilder.Sql(@"
                ALTER TABLE [tblUserGrps] ADD CONSTRAINT [FK_tblUserGrps_tblUsers_tblUserId] 
                    FOREIGN KEY ([tblUserId]) REFERENCES [tblUsers] ([Id]) ON DELETE CASCADE;
            ");
            
            migrationBuilder.Sql(@"
                ALTER TABLE [tblUserGrps] ADD CONSTRAINT [FK_tblUserGrps_tblGrps_tblGrpId] 
                    FOREIGN KEY ([tblGrpId]) REFERENCES [tblGrps] ([Id]) ON DELETE CASCADE;
            ");
            
            migrationBuilder.Sql(@"
                ALTER TABLE [tblGrpPermissions] ADD CONSTRAINT [FK_tblGrpPermissions_tblGrps_tblGrpId] 
                    FOREIGN KEY ([tblGrpId]) REFERENCES [tblGrps] ([Id]) ON DELETE CASCADE;
            ");
            
            migrationBuilder.Sql(@"
                ALTER TABLE [tblGrpPermissions] ADD CONSTRAINT [FK_tblGrpPermissions_tblPermissions_tblPermissionId] 
                    FOREIGN KEY ([tblPermissionId]) REFERENCES [tblPermissions] ([Id]) ON DELETE CASCADE;
            ");
            
            migrationBuilder.Sql(@"
                ALTER TABLE [tblMenuPermissions] ADD CONSTRAINT [FK_tblMenuPermissions_tblMenus_tblMenuId] 
                    FOREIGN KEY ([tblMenuId]) REFERENCES [tblMenus] ([Id]) ON DELETE CASCADE;
            ");
            
            migrationBuilder.Sql(@"
                ALTER TABLE [tblMenuPermissions] ADD CONSTRAINT [FK_tblMenuPermissions_tblPermissions_tblPermissionId] 
                    FOREIGN KEY ([tblPermissionId]) REFERENCES [tblPermissions] ([Id]) ON DELETE CASCADE;
            ");
            
            // FK_tblMenus_tblMenus_ParentId (Self-referencing FK)
            migrationBuilder.Sql(@"
                ALTER TABLE [tblMenus] ADD CONSTRAINT [FK_tblMenus_tblMenus_ParentId] 
                    FOREIGN KEY ([ParentId]) REFERENCES [tblMenus] ([Id]);
            ");

            // ============================================
            // مرحله 7: ایجاد Index های PublicId
            // ============================================

            migrationBuilder.CreateIndex(
                name: "IX_tblUsers_PublicId",
                table: "tblUsers",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblPermissions_PublicId",
                table: "tblPermissions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblMenus_PublicId",
                table: "tblMenus",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblGrps_PublicId",
                table: "tblGrps",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecuritySettings_PublicId",
                table: "SecuritySettings",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_PublicId",
                table: "RefreshTokens",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistory_PublicId",
                table: "PasswordHistory",
                column: "PublicId",
                unique: true);

            // ============================================
            // مرحله 8: بازسازی Indexes اصلی
            // ============================================
            
            // IX_tblUserGrps_tblUserId_tblGrpId (Unique)
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX [IX_tblUserGrps_tblUserId_tblGrpId] ON [tblUserGrps] ([tblUserId], [tblGrpId]);
            ");
            
            // IX_tblUserGrps_tblGrpId
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_tblUserGrps_tblGrpId] ON [tblUserGrps] ([tblGrpId]);
            ");
            
            // IX_tblGrpPermissions_tblGrpId_tblPermissionId (Unique)
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX [IX_tblGrpPermissions_tblGrpId_tblPermissionId] ON [tblGrpPermissions] ([tblGrpId], [tblPermissionId]);
            ");
            
            // IX_tblGrpPermissions_tblPermissionId
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_tblGrpPermissions_tblPermissionId] ON [tblGrpPermissions] ([tblPermissionId]);
            ");
            
            // IX_tblMenuPermissions_tblMenuId_tblPermissionId (Unique)
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX [IX_tblMenuPermissions_tblMenuId_tblPermissionId] ON [tblMenuPermissions] ([tblMenuId], [tblPermissionId]);
            ");
            
            // IX_tblMenuPermissions_tblPermissionId
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_tblMenuPermissions_tblPermissionId] ON [tblMenuPermissions] ([tblPermissionId]);
            ");
            
            // IX_tblMenus_ParentId
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_tblMenus_ParentId] ON [tblMenus] ([ParentId]);
            ");
            
            // IX_RefreshTokens_UserId
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
            ");
            
            // IX_RefreshTokens_UserId_IsRevoked_IsUsed
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_RefreshTokens_UserId_IsRevoked_IsUsed] ON [RefreshTokens] ([UserId], [IsRevoked], [IsUsed]);
            ");
            
            // IX_RefreshTokens_Token (Unique)
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
            ");
            
            // IX_PasswordHistory_UserId
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_PasswordHistory_UserId] ON [PasswordHistory] ([UserId]);
            ");
            
            // IX_PasswordHistory_UserId_PasswordHash
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_PasswordHistory_UserId_PasswordHash] ON [PasswordHistory] ([UserId], [PasswordHash]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // حذف Index های PublicId
            migrationBuilder.DropIndex(name: "IX_tblUsers_PublicId", table: "tblUsers");
            migrationBuilder.DropIndex(name: "IX_tblPermissions_PublicId", table: "tblPermissions");
            migrationBuilder.DropIndex(name: "IX_tblMenus_PublicId", table: "tblMenus");
            migrationBuilder.DropIndex(name: "IX_tblGrps_PublicId", table: "tblGrps");
            migrationBuilder.DropIndex(name: "IX_SecuritySettings_PublicId", table: "SecuritySettings");
            migrationBuilder.DropIndex(name: "IX_RefreshTokens_PublicId", table: "RefreshTokens");
            migrationBuilder.DropIndex(name: "IX_PasswordHistory_PublicId", table: "PasswordHistory");

            // حذف ستون‌های PublicId
            migrationBuilder.DropColumn(name: "PublicId", table: "tblUsers");
            migrationBuilder.DropColumn(name: "PublicId", table: "tblUserGrps");
            migrationBuilder.DropColumn(name: "PublicId", table: "tblPermissions");
            migrationBuilder.DropColumn(name: "PublicId", table: "tblMenus");
            migrationBuilder.DropColumn(name: "PublicId", table: "tblMenuPermissions");
            migrationBuilder.DropColumn(name: "PublicId", table: "tblGrps");
            migrationBuilder.DropColumn(name: "PublicId", table: "tblGrpPermissions");
            migrationBuilder.DropColumn(name: "PublicId", table: "SecuritySettings");
            migrationBuilder.DropColumn(name: "PublicId", table: "RefreshTokens");
            migrationBuilder.DropColumn(name: "PublicId", table: "PasswordHistory");

            // حذف Foreign Keys
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tblUserGrps_tblUsers_tblUserId')
                    ALTER TABLE [tblUserGrps] DROP CONSTRAINT [FK_tblUserGrps_tblUsers_tblUserId];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tblUserGrps_tblGrps_tblGrpId')
                    ALTER TABLE [tblUserGrps] DROP CONSTRAINT [FK_tblUserGrps_tblGrps_tblGrpId];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tblGrpPermissions_tblGrps_tblGrpId')
                    ALTER TABLE [tblGrpPermissions] DROP CONSTRAINT [FK_tblGrpPermissions_tblGrps_tblGrpId];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tblGrpPermissions_tblPermissions_tblPermissionId')
                    ALTER TABLE [tblGrpPermissions] DROP CONSTRAINT [FK_tblGrpPermissions_tblPermissions_tblPermissionId];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tblMenuPermissions_tblMenus_tblMenuId')
                    ALTER TABLE [tblMenuPermissions] DROP CONSTRAINT [FK_tblMenuPermissions_tblMenus_tblMenuId];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tblMenuPermissions_tblPermissions_tblPermissionId')
                    ALTER TABLE [tblMenuPermissions] DROP CONSTRAINT [FK_tblMenuPermissions_tblPermissions_tblPermissionId];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_tblMenus_tblMenus_ParentId')
                    ALTER TABLE [tblMenus] DROP CONSTRAINT [FK_tblMenus_tblMenus_ParentId];
            ");

            // حذف Primary Keys
            migrationBuilder.Sql(@"ALTER TABLE [tblUsers] DROP CONSTRAINT [PK_tblUsers];");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrps] DROP CONSTRAINT [PK_tblGrps];");
            migrationBuilder.Sql(@"ALTER TABLE [tblUserGrps] DROP CONSTRAINT [PK_tblUserGrps];");
            migrationBuilder.Sql(@"ALTER TABLE [tblPermissions] DROP CONSTRAINT [PK_tblPermissions];");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrpPermissions] DROP CONSTRAINT [PK_tblGrpPermissions];");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenus] DROP CONSTRAINT [PK_tblMenus];");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenuPermissions] DROP CONSTRAINT [PK_tblMenuPermissions];");
            migrationBuilder.Sql(@"ALTER TABLE [SecuritySettings] DROP CONSTRAINT [PK_SecuritySettings];");
            migrationBuilder.Sql(@"ALTER TABLE [RefreshTokens] DROP CONSTRAINT [PK_RefreshTokens];");
            migrationBuilder.Sql(@"ALTER TABLE [PasswordHistory] DROP CONSTRAINT [PK_PasswordHistory];");

            // بازگرداندن نوع ستون‌ها به int
            migrationBuilder.Sql(@"ALTER TABLE [tblUsers] ALTER COLUMN [Id] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblUsers] ALTER COLUMN [TblUserGrpIdInsert] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblUsers] ALTER COLUMN [TblUserGrpIdLastEdit] int NULL;");
            
            migrationBuilder.Sql(@"ALTER TABLE [tblGrps] ALTER COLUMN [Id] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrps] ALTER COLUMN [TblUserGrpIdInsert] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrps] ALTER COLUMN [TblUserGrpIdLastEdit] int NULL;");
            
            migrationBuilder.Sql(@"ALTER TABLE [tblUserGrps] ALTER COLUMN [Id] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblUserGrps] ALTER COLUMN [tblUserId] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblUserGrps] ALTER COLUMN [tblGrpId] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblUserGrps] ALTER COLUMN [TblUserGrpIdInsert] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblUserGrps] ALTER COLUMN [TblUserGrpIdLastEdit] int NULL;");
            
            migrationBuilder.Sql(@"ALTER TABLE [tblPermissions] ALTER COLUMN [Id] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblPermissions] ALTER COLUMN [TblUserGrpIdInsert] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblPermissions] ALTER COLUMN [TblUserGrpIdLastEdit] int NULL;");
            
            migrationBuilder.Sql(@"ALTER TABLE [tblGrpPermissions] ALTER COLUMN [Id] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrpPermissions] ALTER COLUMN [tblGrpId] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrpPermissions] ALTER COLUMN [tblPermissionId] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrpPermissions] ALTER COLUMN [GrantedBy] int NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrpPermissions] ALTER COLUMN [TblUserGrpIdInsert] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrpPermissions] ALTER COLUMN [TblUserGrpIdLastEdit] int NULL;");
            
            migrationBuilder.Sql(@"ALTER TABLE [tblMenus] ALTER COLUMN [Id] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenus] ALTER COLUMN [ParentId] int NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenus] ALTER COLUMN [TblUserGrpIdInsert] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenus] ALTER COLUMN [TblUserGrpIdLastEdit] int NULL;");
            
            migrationBuilder.Sql(@"ALTER TABLE [tblMenuPermissions] ALTER COLUMN [Id] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenuPermissions] ALTER COLUMN [tblMenuId] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenuPermissions] ALTER COLUMN [tblPermissionId] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenuPermissions] ALTER COLUMN [TblUserGrpIdInsert] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenuPermissions] ALTER COLUMN [TblUserGrpIdLastEdit] int NULL;");
            
            migrationBuilder.Sql(@"ALTER TABLE [SecuritySettings] ALTER COLUMN [Id] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [SecuritySettings] ALTER COLUMN [TblUserGrpIdInsert] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [SecuritySettings] ALTER COLUMN [TblUserGrpIdLastEdit] int NULL;");
            
            migrationBuilder.Sql(@"ALTER TABLE [RefreshTokens] ALTER COLUMN [Id] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [RefreshTokens] ALTER COLUMN [UserId] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [RefreshTokens] ALTER COLUMN [TblUserGrpIdInsert] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [RefreshTokens] ALTER COLUMN [TblUserGrpIdLastEdit] int NULL;");
            
            migrationBuilder.Sql(@"ALTER TABLE [PasswordHistory] ALTER COLUMN [Id] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [PasswordHistory] ALTER COLUMN [UserId] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [PasswordHistory] ALTER COLUMN [TblUserGrpIdInsert] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [PasswordHistory] ALTER COLUMN [TblUserGrpIdLastEdit] int NULL;");

            // بازسازی Primary Keys
            migrationBuilder.Sql(@"ALTER TABLE [tblUsers] ADD CONSTRAINT [PK_tblUsers] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrps] ADD CONSTRAINT [PK_tblGrps] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [tblUserGrps] ADD CONSTRAINT [PK_tblUserGrps] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [tblPermissions] ADD CONSTRAINT [PK_tblPermissions] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [tblGrpPermissions] ADD CONSTRAINT [PK_tblGrpPermissions] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenus] ADD CONSTRAINT [PK_tblMenus] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [tblMenuPermissions] ADD CONSTRAINT [PK_tblMenuPermissions] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [SecuritySettings] ADD CONSTRAINT [PK_SecuritySettings] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [RefreshTokens] ADD CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [PasswordHistory] ADD CONSTRAINT [PK_PasswordHistory] PRIMARY KEY ([Id]);");

            // بازسازی Foreign Keys
            migrationBuilder.Sql(@"
                ALTER TABLE [tblUserGrps] ADD CONSTRAINT [FK_tblUserGrps_tblUsers_tblUserId] 
                    FOREIGN KEY ([tblUserId]) REFERENCES [tblUsers] ([Id]) ON DELETE CASCADE;
            ");
            migrationBuilder.Sql(@"
                ALTER TABLE [tblUserGrps] ADD CONSTRAINT [FK_tblUserGrps_tblGrps_tblGrpId] 
                    FOREIGN KEY ([tblGrpId]) REFERENCES [tblGrps] ([Id]) ON DELETE CASCADE;
            ");
            migrationBuilder.Sql(@"
                ALTER TABLE [tblGrpPermissions] ADD CONSTRAINT [FK_tblGrpPermissions_tblGrps_tblGrpId] 
                    FOREIGN KEY ([tblGrpId]) REFERENCES [tblGrps] ([Id]) ON DELETE CASCADE;
            ");
            migrationBuilder.Sql(@"
                ALTER TABLE [tblGrpPermissions] ADD CONSTRAINT [FK_tblGrpPermissions_tblPermissions_tblPermissionId] 
                    FOREIGN KEY ([tblPermissionId]) REFERENCES [tblPermissions] ([Id]) ON DELETE CASCADE;
            ");
            migrationBuilder.Sql(@"
                ALTER TABLE [tblMenuPermissions] ADD CONSTRAINT [FK_tblMenuPermissions_tblMenus_tblMenuId] 
                    FOREIGN KEY ([tblMenuId]) REFERENCES [tblMenus] ([Id]) ON DELETE CASCADE;
            ");
            migrationBuilder.Sql(@"
                ALTER TABLE [tblMenuPermissions] ADD CONSTRAINT [FK_tblMenuPermissions_tblPermissions_tblPermissionId] 
                    FOREIGN KEY ([tblPermissionId]) REFERENCES [tblPermissions] ([Id]) ON DELETE CASCADE;
            ");
            migrationBuilder.Sql(@"
                ALTER TABLE [tblMenus] ADD CONSTRAINT [FK_tblMenus_tblMenus_ParentId] 
                    FOREIGN KEY ([ParentId]) REFERENCES [tblMenus] ([Id]);
            ");
        }
    }
}
