using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations.LogMigrations
{
    /// <inheritdoc />
    public partial class UpdateIdToLongAndAddPublicId_Log : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================
            // مرحله 1: حذف Foreign Keys
            // ============================================
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AuditLogDetail_AuditLogMaster_AuditLogMasterId')
                    ALTER TABLE [AuditLogDetail] DROP CONSTRAINT [FK_AuditLogDetail_AuditLogMaster_AuditLogMasterId];
            ");

            // ============================================
            // مرحله 2: حذف Indexes
            // ============================================
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLogDetail_AuditLogMasterId')
                    DROP INDEX [IX_AuditLogDetail_AuditLogMasterId] ON [AuditLogDetail];
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLogMaster_UserId')
                    DROP INDEX [IX_AuditLogMaster_UserId] ON [AuditLogMaster];
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLogMaster_EventDateTime')
                    DROP INDEX [IX_AuditLogMaster_EventDateTime] ON [AuditLogMaster];
            ");

            // ============================================
            // مرحله 3: حذف Primary Keys
            // ============================================
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_AuditLogMaster')
                    ALTER TABLE [AuditLogMaster] DROP CONSTRAINT [PK_AuditLogMaster];
            ");
            
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_AuditLogDetail')
                    ALTER TABLE [AuditLogDetail] DROP CONSTRAINT [PK_AuditLogDetail];
            ");

            // ============================================
            // مرحله 4: تغییر نوع ستون‌ها از int به bigint
            // ============================================
            
            // AuditLogMaster
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogMaster] ALTER COLUMN [Id] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogMaster] ALTER COLUMN [UserId] bigint NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogMaster] ALTER COLUMN [TblUserGrpIdInsert] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogMaster] ALTER COLUMN [TblUserGrpIdLastEdit] bigint NULL;");
            
            // AuditLogDetail
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogDetail] ALTER COLUMN [Id] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogDetail] ALTER COLUMN [AuditLogMasterId] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogDetail] ALTER COLUMN [TblUserGrpIdInsert] bigint NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogDetail] ALTER COLUMN [TblUserGrpIdLastEdit] bigint NULL;");

            // ============================================
            // مرحله 5: اضافه کردن ستون PublicId
            // ============================================
            
            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "AuditLogMaster",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "AuditLogDetail",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            // ============================================
            // مرحله 6: ایجاد مجدد Primary Keys
            // ============================================
            
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogMaster] ADD CONSTRAINT [PK_AuditLogMaster] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogDetail] ADD CONSTRAINT [PK_AuditLogDetail] PRIMARY KEY ([Id]);");

            // ============================================
            // مرحله 7: ایجاد مجدد Foreign Keys
            // ============================================
            
            migrationBuilder.Sql(@"
                ALTER TABLE [AuditLogDetail] ADD CONSTRAINT [FK_AuditLogDetail_AuditLogMaster_AuditLogMasterId] 
                    FOREIGN KEY ([AuditLogMasterId]) REFERENCES [AuditLogMaster] ([Id]) ON DELETE CASCADE;
            ");

            // ============================================
            // مرحله 8: ایجاد مجدد Indexes
            // ============================================
            
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_AuditLogDetail_AuditLogMasterId] ON [AuditLogDetail] ([AuditLogMasterId]);
            ");
            
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_AuditLogMaster_UserId] ON [AuditLogMaster] ([UserId]);
            ");
            
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_AuditLogMaster_EventDateTime] ON [AuditLogMaster] ([EventDateTime]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // حذف ستون‌های PublicId
            migrationBuilder.DropColumn(name: "PublicId", table: "AuditLogMaster");
            migrationBuilder.DropColumn(name: "PublicId", table: "AuditLogDetail");

            // حذف Foreign Keys
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AuditLogDetail_AuditLogMaster_AuditLogMasterId')
                    ALTER TABLE [AuditLogDetail] DROP CONSTRAINT [FK_AuditLogDetail_AuditLogMaster_AuditLogMasterId];
            ");

            // حذف Indexes
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLogDetail_AuditLogMasterId')
                    DROP INDEX [IX_AuditLogDetail_AuditLogMasterId] ON [AuditLogDetail];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLogMaster_UserId')
                    DROP INDEX [IX_AuditLogMaster_UserId] ON [AuditLogMaster];
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLogMaster_EventDateTime')
                    DROP INDEX [IX_AuditLogMaster_EventDateTime] ON [AuditLogMaster];
            ");

            // حذف Primary Keys
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogMaster] DROP CONSTRAINT [PK_AuditLogMaster];");
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogDetail] DROP CONSTRAINT [PK_AuditLogDetail];");

            // بازگرداندن نوع ستون‌ها به int
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogMaster] ALTER COLUMN [Id] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogMaster] ALTER COLUMN [UserId] int NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogMaster] ALTER COLUMN [TblUserGrpIdInsert] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogMaster] ALTER COLUMN [TblUserGrpIdLastEdit] int NULL;");
            
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogDetail] ALTER COLUMN [Id] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogDetail] ALTER COLUMN [AuditLogMasterId] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogDetail] ALTER COLUMN [TblUserGrpIdInsert] int NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogDetail] ALTER COLUMN [TblUserGrpIdLastEdit] int NULL;");

            // بازسازی Primary Keys
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogMaster] ADD CONSTRAINT [PK_AuditLogMaster] PRIMARY KEY ([Id]);");
            migrationBuilder.Sql(@"ALTER TABLE [AuditLogDetail] ADD CONSTRAINT [PK_AuditLogDetail] PRIMARY KEY ([Id]);");

            // بازسازی Foreign Keys
            migrationBuilder.Sql(@"
                ALTER TABLE [AuditLogDetail] ADD CONSTRAINT [FK_AuditLogDetail_AuditLogMaster_AuditLogMasterId] 
                    FOREIGN KEY ([AuditLogMasterId]) REFERENCES [AuditLogMaster] ([Id]) ON DELETE CASCADE;
            ");

            // بازسازی Indexes
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_AuditLogDetail_AuditLogMasterId] ON [AuditLogDetail] ([AuditLogMasterId]);
            ");
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_AuditLogMaster_UserId] ON [AuditLogMaster] ([UserId]);
            ");
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_AuditLogMaster_EventDateTime] ON [AuditLogMaster] ([EventDateTime]);
            ");
        }
    }
}
