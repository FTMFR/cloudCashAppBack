using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrityHashColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                table: "tblUsers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                table: "tblUserGrps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                table: "tblPermissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                table: "tblMenus",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                table: "tblMenuPermissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                table: "tblGrps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                table: "tblGrpPermissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                table: "SecuritySettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                table: "RefreshTokens",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                table: "PasswordHistory",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                table: "CryptographicKeys",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                table: "tblUsers");

            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                table: "tblUserGrps");

            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                table: "tblPermissions");

            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                table: "tblMenus");

            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                table: "tblMenuPermissions");

            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                table: "tblGrps");

            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                table: "tblGrpPermissions");

            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                table: "SecuritySettings");

            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                table: "PasswordHistory");

            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                table: "CryptographicKeys");
        }
    }
}
