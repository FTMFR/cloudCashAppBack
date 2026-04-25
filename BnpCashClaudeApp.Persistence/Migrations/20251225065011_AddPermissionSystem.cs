using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "tblGrpMenus",
                newName: "AccessLevel");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "tblGrps",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "tblGrps",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "tblPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<int>(type: "int", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tblGrpPermissions",
                columns: table => new
                {
                    tblGrpId = table.Column<int>(type: "int", nullable: false),
                    tblPermissionId = table.Column<int>(type: "int", nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GrantedBy = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Id = table.Column<int>(type: "int", nullable: false),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<int>(type: "int", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblGrpPermissions", x => new { x.tblGrpId, x.tblPermissionId });
                    table.ForeignKey(
                        name: "FK_tblGrpPermissions_tblGrps_tblGrpId",
                        column: x => x.tblGrpId,
                        principalTable: "tblGrps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblGrpPermissions_tblPermissions_tblPermissionId",
                        column: x => x.tblPermissionId,
                        principalTable: "tblPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblGrpPermissions_tblPermissionId",
                table: "tblGrpPermissions",
                column: "tblPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_tblPermissions_Name",
                table: "tblPermissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblPermissions_Resource_Action",
                table: "tblPermissions",
                columns: new[] { "Resource", "Action" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblGrpPermissions");

            migrationBuilder.DropTable(
                name: "tblPermissions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "tblGrps");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "tblGrps");

            migrationBuilder.RenameColumn(
                name: "AccessLevel",
                table: "tblGrpMenus",
                newName: "Status");
        }
    }
}
