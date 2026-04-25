using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <summary>
    /// Migration برای اضافه کردن تنظیم پیش‌فرض MaxConcurrentSessions
    /// ============================================
    /// FTA_MCS.1 - محدودیت نشست همزمان
    /// مقدار پیش‌فرض: 3 نشست همزمان
    /// ============================================
    /// </summary>
    public partial class AddMaxConcurrentSessionsSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================
            // اضافه کردن تنظیم MaxConcurrentSessions
            // ============================================
            var now = DateTime.UtcNow;
            var nowString = now.ToString("yyyy-MM-dd HH:mm:ss");

            migrationBuilder.Sql($@"
                -- بررسی اینکه تنظیم از قبل وجود ندارد
                IF NOT EXISTS (SELECT 1 FROM SecuritySettings WHERE SettingKey = 'MaxConcurrentSessions')
                BEGIN
                    INSERT INTO SecuritySettings (
                        SettingKey,
                        SettingName,
                        SettingValue,
                        SettingType,
                        Description,
                        IsActive,
                        IsEditable,
                        DisplayOrder,
                        ZamanInsert,
                        TblUserGrpIdInsert
                    )
                    VALUES (
                        'MaxConcurrentSessions',
                        N'حداکثر نشست‌های همزمان',
                        '3',
                        4, -- Session type
                        N'حداکثر تعداد نشست‌های همزمان برای هر کاربر (FTA_MCS.1)',
                        1,
                        1,
                        10,
                        '{nowString}',
                        1
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ============================================
            // حذف تنظیم MaxConcurrentSessions
            // ============================================
            migrationBuilder.Sql(@"
                DELETE FROM SecuritySettings
                WHERE SettingKey = 'MaxConcurrentSessions';
            ");
        }
    }
}
