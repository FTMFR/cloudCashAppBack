using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations.LogMigrations
{
    /// <inheritdoc />
    public partial class InitialAuditLogCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ZamanInsert = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TblUserGrpIdInsert = table.Column<int>(type: "int", nullable: false),
                    ZamanLastEdit = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TblUserGrpIdLastEdit = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogDetail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuditLogMasterId = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ZamanInsert = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TblUserGrpIdInsert = table.Column<int>(type: "int", nullable: false),
                    ZamanLastEdit = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TblUserGrpIdLastEdit = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogDetail_AuditLogMaster_AuditLogMasterId",
                        column: x => x.AuditLogMasterId,
                        principalTable: "AuditLogMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogDetail_AuditLogMasterId",
                table: "AuditLogDetail",
                column: "AuditLogMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogDetail_FieldName",
                table: "AuditLogDetail",
                column: "FieldName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogMaster_EntityType",
                table: "AuditLogMaster",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogMaster_EntityType_EntityId",
                table: "AuditLogMaster",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogMaster_EventDateTime",
                table: "AuditLogMaster",
                column: "EventDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogMaster_EventType",
                table: "AuditLogMaster",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogMaster_UserId",
                table: "AuditLogMaster",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogDetail");

            migrationBuilder.DropTable(
                name: "AuditLogMaster");
        }
    }
}
