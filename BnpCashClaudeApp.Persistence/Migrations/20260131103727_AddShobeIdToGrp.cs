using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShobeIdToGrp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "tblShobeId",
                table: "tblGrps",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblGrps_tblShobeId",
                table: "tblGrps",
                column: "tblShobeId");

            migrationBuilder.AddForeignKey(
                name: "FK_tblGrps_tblShobes_tblShobeId",
                table: "tblGrps",
                column: "tblShobeId",
                principalTable: "tblShobes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tblGrps_tblShobes_tblShobeId",
                table: "tblGrps");

            migrationBuilder.DropIndex(
                name: "IX_tblGrps_tblShobeId",
                table: "tblGrps");

            migrationBuilder.DropColumn(
                name: "tblShobeId",
                table: "tblGrps");
        }
    }
}
