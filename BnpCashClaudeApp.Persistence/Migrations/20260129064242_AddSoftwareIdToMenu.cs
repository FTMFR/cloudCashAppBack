using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftwareIdToMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "tblSoftwareId",
                table: "tblMenus",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblMenus_tblSoftwareId",
                table: "tblMenus",
                column: "tblSoftwareId");

            migrationBuilder.AddForeignKey(
                name: "FK_tblMenus_tblSoftwares_tblSoftwareId",
                table: "tblMenus",
                column: "tblSoftwareId",
                principalTable: "tblSoftwares",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tblMenus_tblSoftwares_tblSoftwareId",
                table: "tblMenus");

            migrationBuilder.DropIndex(
                name: "IX_tblMenus_tblSoftwareId",
                table: "tblMenus");

            migrationBuilder.DropColumn(
                name: "tblSoftwareId",
                table: "tblMenus");
        }
    }
}
