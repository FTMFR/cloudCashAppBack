using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParentIdTotblGrp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ParentId",
                table: "tblGrps",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblGrps_ParentId",
                table: "tblGrps",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_tblGrps_tblGrps_ParentId",
                table: "tblGrps",
                column: "ParentId",
                principalTable: "tblGrps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tblGrps_tblGrps_ParentId",
                table: "tblGrps");

            migrationBuilder.DropIndex(
                name: "IX_tblGrps_ParentId",
                table: "tblGrps");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "tblGrps");
        }
    }
}
