using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations.LogMigrations
{
    /// <inheritdoc />
    public partial class AddIntegrityHashColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                table: "AuditLogMaster",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegrityHash",
                table: "AuditLogDetail",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                table: "AuditLogMaster");

            migrationBuilder.DropColumn(
                name: "IntegrityHash",
                table: "AuditLogDetail");
        }
    }
}
