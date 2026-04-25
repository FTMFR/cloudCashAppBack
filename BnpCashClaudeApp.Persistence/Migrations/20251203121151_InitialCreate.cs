using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblGrps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GrpCode = table.Column<int>(type: "int", nullable: false),
                    ZamanInsert = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TblUserGrpIdInsert = table.Column<int>(type: "int", nullable: false),
                    ZamanLastEdit = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TblUserGrpIdLastEdit = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblGrps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tblMenus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    IsMenu = table.Column<bool>(type: "bit", nullable: false),
                    ZamanInsert = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TblUserGrpIdInsert = table.Column<int>(type: "int", nullable: false),
                    ZamanLastEdit = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TblUserGrpIdLastEdit = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblMenus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblMenus_tblMenus_ParentId",
                        column: x => x.ParentId,
                        principalTable: "tblMenus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MobileNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserCode = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ZamanInsert = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TblUserGrpIdInsert = table.Column<int>(type: "int", nullable: false),
                    ZamanLastEdit = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TblUserGrpIdLastEdit = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tblGrpMenus",
                columns: table => new
                {
                    tblGrpId = table.Column<int>(type: "int", nullable: false),
                    tblMenuId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    ZamanInsert = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TblUserGrpIdInsert = table.Column<int>(type: "int", nullable: false),
                    ZamanLastEdit = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TblUserGrpIdLastEdit = table.Column<int>(type: "int", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "tblUserGrps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tblUserId = table.Column<int>(type: "int", nullable: false),
                    tblGrpId = table.Column<int>(type: "int", nullable: false),
                    AssignmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ZamanInsert = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TblUserGrpIdInsert = table.Column<int>(type: "int", nullable: false),
                    ZamanLastEdit = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TblUserGrpIdLastEdit = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblUserGrps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblUserGrps_tblGrps_tblGrpId",
                        column: x => x.tblGrpId,
                        principalTable: "tblGrps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblUserGrps_tblUsers_tblUserId",
                        column: x => x.tblUserId,
                        principalTable: "tblUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblGrpMenus_tblMenuId",
                table: "tblGrpMenus",
                column: "tblMenuId");

            migrationBuilder.CreateIndex(
                name: "IX_tblMenus_ParentId",
                table: "tblMenus",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_tblUserGrps_tblGrpId",
                table: "tblUserGrps",
                column: "tblGrpId");

            migrationBuilder.CreateIndex(
                name: "IX_tblUserGrps_tblUserId_tblGrpId",
                table: "tblUserGrps",
                columns: new[] { "tblUserId", "tblGrpId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblGrpMenus");

            migrationBuilder.DropTable(
                name: "tblUserGrps");

            migrationBuilder.DropTable(
                name: "tblMenus");

            migrationBuilder.DropTable(
                name: "tblGrps");

            migrationBuilder.DropTable(
                name: "tblUsers");
        }
    }
}
