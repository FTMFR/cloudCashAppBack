using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerShobeUserRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "tblCustomerId",
                table: "tblUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "tblShobeId",
                table: "tblUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "tblCustomerId",
                table: "tblShobes",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblUsers_tblCustomerId",
                table: "tblUsers",
                column: "tblCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblUsers_tblShobeId",
                table: "tblUsers",
                column: "tblShobeId");

            migrationBuilder.CreateIndex(
                name: "IX_tblShobes_tblCustomerId",
                table: "tblShobes",
                column: "tblCustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_tblShobes_tblCustomers_tblCustomerId",
                table: "tblShobes",
                column: "tblCustomerId",
                principalTable: "tblCustomers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblUsers_tblCustomers_tblCustomerId",
                table: "tblUsers",
                column: "tblCustomerId",
                principalTable: "tblCustomers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tblUsers_tblShobes_tblShobeId",
                table: "tblUsers",
                column: "tblShobeId",
                principalTable: "tblShobes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tblShobes_tblCustomers_tblCustomerId",
                table: "tblShobes");

            migrationBuilder.DropForeignKey(
                name: "FK_tblUsers_tblCustomers_tblCustomerId",
                table: "tblUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_tblUsers_tblShobes_tblShobeId",
                table: "tblUsers");

            migrationBuilder.DropIndex(
                name: "IX_tblUsers_tblCustomerId",
                table: "tblUsers");

            migrationBuilder.DropIndex(
                name: "IX_tblUsers_tblShobeId",
                table: "tblUsers");

            migrationBuilder.DropIndex(
                name: "IX_tblShobes_tblCustomerId",
                table: "tblShobes");

            migrationBuilder.DropColumn(
                name: "tblCustomerId",
                table: "tblUsers");

            migrationBuilder.DropColumn(
                name: "tblShobeId",
                table: "tblUsers");

            migrationBuilder.DropColumn(
                name: "tblCustomerId",
                table: "tblShobes");
        }
    }
}
