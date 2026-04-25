using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceGrpMenuWithMenuPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblGrpMenus");

            migrationBuilder.CreateTable(
                name: "tblMenuPermissions",
                columns: table => new
                {
                    tblMenuId = table.Column<int>(type: "int", nullable: false),
                    tblPermissionId = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Id = table.Column<int>(type: "int", nullable: false),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<int>(type: "int", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblMenuPermissions", x => new { x.tblMenuId, x.tblPermissionId });
                    table.ForeignKey(
                        name: "FK_tblMenuPermissions_tblMenus_tblMenuId",
                        column: x => x.tblMenuId,
                        principalTable: "tblMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblMenuPermissions_tblPermissions_tblPermissionId",
                        column: x => x.tblPermissionId,
                        principalTable: "tblPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblMenuPermissions_tblPermissionId",
                table: "tblMenuPermissions",
                column: "tblPermissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblMenuPermissions");

            migrationBuilder.CreateTable(
                name: "tblGrpMenus",
                columns: table => new
                {
                    tblGrpId = table.Column<int>(type: "int", nullable: false),
                    tblMenuId = table.Column<int>(type: "int", nullable: false),
                    AccessLevel = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    TblUserGrpIdInsert = table.Column<int>(type: "int", nullable: false),
                    TblUserGrpIdLastEdit = table.Column<int>(type: "int", nullable: true),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblGrpMenus", x => new { x.tblGrpId, x.tblMenuId });
                    table.ForeignKey(
                        name: "FK_tblGrpMenus_tblGrps_tblGrpId",
                        column: x => x.tblGrpId,
                        principalTable: "tblGrps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblGrpMenus_tblMenus_tblMenuId",
                        column: x => x.tblMenuId,
                        principalTable: "tblMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblGrpMenus_tblMenuId",
                table: "tblGrpMenus",
                column: "tblMenuId");
        }
    }
}
