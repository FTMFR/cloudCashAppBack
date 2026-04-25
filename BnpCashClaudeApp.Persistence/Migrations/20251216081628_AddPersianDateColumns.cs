using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersianDateColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ZamanInsertShamsi",
                table: "tblUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ZamanLastEditShamsi",
                table: "tblUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZamanInsertShamsi",
                table: "tblUserGrps",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ZamanLastEditShamsi",
                table: "tblUserGrps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZamanInsertShamsi",
                table: "tblMenus",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ZamanLastEditShamsi",
                table: "tblMenus",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZamanInsertShamsi",
                table: "tblGrps",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ZamanLastEditShamsi",
                table: "tblGrps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZamanInsertShamsi",
                table: "tblGrpMenus",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ZamanLastEditShamsi",
                table: "tblGrpMenus",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ZamanInsertShamsi",
                table: "tblUsers");

            migrationBuilder.DropColumn(
                name: "ZamanLastEditShamsi",
                table: "tblUsers");

            migrationBuilder.DropColumn(
                name: "ZamanInsertShamsi",
                table: "tblUserGrps");

            migrationBuilder.DropColumn(
                name: "ZamanLastEditShamsi",
                table: "tblUserGrps");

            migrationBuilder.DropColumn(
                name: "ZamanInsertShamsi",
                table: "tblMenus");

            migrationBuilder.DropColumn(
                name: "ZamanLastEditShamsi",
                table: "tblMenus");

            migrationBuilder.DropColumn(
                name: "ZamanInsertShamsi",
                table: "tblGrps");

            migrationBuilder.DropColumn(
                name: "ZamanLastEditShamsi",
                table: "tblGrps");

            migrationBuilder.DropColumn(
                name: "ZamanInsertShamsi",
                table: "tblGrpMenus");

            migrationBuilder.DropColumn(
                name: "ZamanLastEditShamsi",
                table: "tblGrpMenus");
        }
    }
}
