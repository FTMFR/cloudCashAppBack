using System;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTblShobeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblShobes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShobeCode = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblShobes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblShobes_tblShobes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "tblShobes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblShobes_IsActive",
                table: "tblShobes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_tblShobes_IsActive_DisplayOrder",
                table: "tblShobes",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_tblShobes_ParentId",
                table: "tblShobes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_tblShobes_PublicId",
                table: "tblShobes",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblShobes_ShobeCode",
                table: "tblShobes",
                column: "ShobeCode",
                unique: true);

            // ============================================
            // اضافه کردن شعبه پیش‌فرض "مرکزی"
            // ============================================
            var now = DateTime.UtcNow;
            var persianCalendar = new PersianCalendar();
            int year = persianCalendar.GetYear(now);
            int month = persianCalendar.GetMonth(now);
            int day = persianCalendar.GetDayOfMonth(now);
            int hour = now.Hour;
            int minute = now.Minute;
            int second = now.Second;
            var persianDate = $"{year:0000}/{month:00}/{day:00} {hour:00}:{minute:00}:{second:00}";
            
            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM tblShobes WHERE ShobeCode = 1)
                BEGIN
                    INSERT INTO tblShobes (
                        Title, 
                        ShobeCode, 
                        IsActive, 
                        DisplayOrder, 
                        ZamanInsert, 
                        TblUserGrpIdInsert
                    )
                    VALUES (
                        N'مرکزی', 
                        1, 
                        1, 
                        1, 
                        N'{persianDate}', 
                        1
                    )
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblShobes");
        }
    }
}
