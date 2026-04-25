using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations.LogMigrations
{
    /// <inheritdoc />
    public partial class OptimizeLogDateColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================
            // ابتدا ستون‌های تاریخ را به nvarchar(25) موقت تغییر دهید
            // سپس داده‌های datetime را به شمسی تبدیل کنید
            // ============================================

            // AuditLogMaster - تبدیل datetime به nvarchar
            migrationBuilder.Sql(@"
                ALTER TABLE [AuditLogMaster] ADD [ZamanInsert_temp] nvarchar(25) NULL;
                ALTER TABLE [AuditLogMaster] ADD [ZamanLastEdit_temp] nvarchar(25) NULL;
            ");

            // تبدیل تاریخ‌های میلادی به شمسی (یک تاریخ پیش‌فرض شمسی برای داده‌های موجود)
            migrationBuilder.Sql(@"
                UPDATE [AuditLogMaster] 
                SET [ZamanInsert_temp] = '1403/09/26 00:00:00'
                WHERE [ZamanInsert] IS NOT NULL;

                UPDATE [AuditLogMaster] 
                SET [ZamanLastEdit_temp] = NULL;
            ");

            // حذف ستون‌های قدیمی و تغییر نام ستون‌های جدید
            migrationBuilder.Sql(@"
                ALTER TABLE [AuditLogMaster] DROP COLUMN [ZamanInsert];
                ALTER TABLE [AuditLogMaster] DROP COLUMN [ZamanLastEdit];
                EXEC sp_rename 'AuditLogMaster.ZamanInsert_temp', 'ZamanInsert', 'COLUMN';
                EXEC sp_rename 'AuditLogMaster.ZamanLastEdit_temp', 'ZamanLastEdit', 'COLUMN';
                ALTER TABLE [AuditLogMaster] ALTER COLUMN [ZamanInsert] nvarchar(25) NOT NULL;
            ");

            // AuditLogDetail - تبدیل datetime به nvarchar
            migrationBuilder.Sql(@"
                ALTER TABLE [AuditLogDetail] ADD [ZamanInsert_temp] nvarchar(25) NULL;
                ALTER TABLE [AuditLogDetail] ADD [ZamanLastEdit_temp] nvarchar(25) NULL;
            ");

            // تبدیل تاریخ‌های میلادی به شمسی
            migrationBuilder.Sql(@"
                UPDATE [AuditLogDetail] 
                SET [ZamanInsert_temp] = '1403/09/26 00:00:00'
                WHERE [ZamanInsert] IS NOT NULL;

                UPDATE [AuditLogDetail] 
                SET [ZamanLastEdit_temp] = NULL;
            ");

            // حذف ستون‌های قدیمی و تغییر نام ستون‌های جدید
            migrationBuilder.Sql(@"
                ALTER TABLE [AuditLogDetail] DROP COLUMN [ZamanInsert];
                ALTER TABLE [AuditLogDetail] DROP COLUMN [ZamanLastEdit];
                EXEC sp_rename 'AuditLogDetail.ZamanInsert_temp', 'ZamanInsert', 'COLUMN';
                EXEC sp_rename 'AuditLogDetail.ZamanLastEdit_temp', 'ZamanLastEdit', 'COLUMN';
                ALTER TABLE [AuditLogDetail] ALTER COLUMN [ZamanInsert] nvarchar(25) NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ============================================
            // بازگشت به datetime2 (توجه: داده‌های شمسی از بین می‌روند)
            // ============================================

            // AuditLogMaster
            migrationBuilder.Sql(@"
                ALTER TABLE [AuditLogMaster] ADD [ZamanInsert_temp] datetime2 NULL;
                ALTER TABLE [AuditLogMaster] ADD [ZamanLastEdit_temp] datetime2 NULL;
                UPDATE [AuditLogMaster] SET [ZamanInsert_temp] = GETUTCDATE();
                ALTER TABLE [AuditLogMaster] DROP COLUMN [ZamanInsert];
                ALTER TABLE [AuditLogMaster] DROP COLUMN [ZamanLastEdit];
                EXEC sp_rename 'AuditLogMaster.ZamanInsert_temp', 'ZamanInsert', 'COLUMN';
                EXEC sp_rename 'AuditLogMaster.ZamanLastEdit_temp', 'ZamanLastEdit', 'COLUMN';
                ALTER TABLE [AuditLogMaster] ALTER COLUMN [ZamanInsert] datetime2 NOT NULL;
            ");

            // AuditLogDetail
            migrationBuilder.Sql(@"
                ALTER TABLE [AuditLogDetail] ADD [ZamanInsert_temp] datetime2 NULL;
                ALTER TABLE [AuditLogDetail] ADD [ZamanLastEdit_temp] datetime2 NULL;
                UPDATE [AuditLogDetail] SET [ZamanInsert_temp] = GETUTCDATE();
                ALTER TABLE [AuditLogDetail] DROP COLUMN [ZamanInsert];
                ALTER TABLE [AuditLogDetail] DROP COLUMN [ZamanLastEdit];
                EXEC sp_rename 'AuditLogDetail.ZamanInsert_temp', 'ZamanInsert', 'COLUMN';
                EXEC sp_rename 'AuditLogDetail.ZamanLastEdit_temp', 'ZamanLastEdit', 'COLUMN';
                ALTER TABLE [AuditLogDetail] ALTER COLUMN [ZamanInsert] datetime2 NOT NULL;
            ");
        }
    }
}
