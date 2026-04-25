using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations.CashMigrations
{
    /// <inheritdoc />
    public partial class InitialCashMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblTafsiliTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tblShobeId = table.Column<long>(type: "bigint", nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CodeTafsiliType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblTafsiliTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblTafsiliTypes_tblTafsiliTypes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "tblTafsiliTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblAzaNoes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tblShobeId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CodeHoze = table.Column<int>(type: "int", nullable: false),
                    PishFarz = table.Column<bool>(type: "bit", nullable: false),
                    tblTafsiliTypeId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblAzaNoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblAzaNoes_tblTafsiliTypes_tblTafsiliTypeId",
                        column: x => x.tblTafsiliTypeId,
                        principalTable: "tblTafsiliTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblAzaNoes_IsDeleted_IsActive",
                table: "tblAzaNoes",
                columns: new[] { "IsDeleted", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_tblAzaNoes_PublicId",
                table: "tblAzaNoes",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblAzaNoes_tblShobeId",
                table: "tblAzaNoes",
                column: "tblShobeId");

            migrationBuilder.CreateIndex(
                name: "IX_tblAzaNoes_tblShobeId_CodeHoze",
                table: "tblAzaNoes",
                columns: new[] { "tblShobeId", "CodeHoze" });

            migrationBuilder.CreateIndex(
                name: "IX_tblAzaNoes_tblTafsiliTypeId",
                table: "tblAzaNoes",
                column: "tblTafsiliTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tblTafsiliTypes_CodeTafsiliType",
                table: "tblTafsiliTypes",
                column: "CodeTafsiliType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblTafsiliTypes_IsDeleted_IsActive",
                table: "tblTafsiliTypes",
                columns: new[] { "IsDeleted", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_tblTafsiliTypes_ParentId",
                table: "tblTafsiliTypes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_tblTafsiliTypes_PublicId",
                table: "tblTafsiliTypes",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblTafsiliTypes_tblShobeId",
                table: "tblTafsiliTypes",
                column: "tblShobeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblAzaNoes");

            migrationBuilder.DropTable(
                name: "tblTafsiliTypes");
        }
    }
}
