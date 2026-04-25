using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMfaFieldsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMfaEnabled",
                table: "tblUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MfaEnabledAt",
                table: "tblUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MfaLastUsedAt",
                table: "tblUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MfaRecoveryCodes",
                table: "tblUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MfaSecretKey",
                table: "tblUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PasswordHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SetAt = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<int>(type: "int", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistory_UserId",
                table: "PasswordHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistory_UserId_PasswordHash",
                table: "PasswordHistory",
                columns: new[] { "UserId", "PasswordHash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordHistory");

            migrationBuilder.DropColumn(
                name: "IsMfaEnabled",
                table: "tblUsers");

            migrationBuilder.DropColumn(
                name: "MfaEnabledAt",
                table: "tblUsers");

            migrationBuilder.DropColumn(
                name: "MfaLastUsedAt",
                table: "tblUsers");

            migrationBuilder.DropColumn(
                name: "MfaRecoveryCodes",
                table: "tblUsers");

            migrationBuilder.DropColumn(
                name: "MfaSecretKey",
                table: "tblUsers");
        }
    }
}
