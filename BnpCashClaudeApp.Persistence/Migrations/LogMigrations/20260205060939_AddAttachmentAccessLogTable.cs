using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations.LogMigrations
{
    /// <inheritdoc />
    public partial class AddAttachmentAccessLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblAttachmentAccessLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttachmentId = table.Column<long>(type: "bigint", nullable: false),
                    AttachmentPublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    AccessType = table.Column<int>(type: "int", nullable: false),
                    AccessDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserGroupId = table.Column<long>(type: "bigint", nullable: true),
                    UserGroupName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Browser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BrowserVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccessDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccessDateTimePersian = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AccessDeniedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    BytesTransferred = table.Column<long>(type: "bigint", nullable: true),
                    WasEncrypted = table.Column<bool>(type: "bit", nullable: true),
                    IntegrityVerified = table.Column<bool>(type: "bit", nullable: true),
                    IntegrityCheckResult = table.Column<bool>(type: "bit", nullable: true),
                    FileSensitivityLevel = table.Column<int>(type: "int", nullable: true),
                    FileSecurityClassification = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    tblCustomerId = table.Column<long>(type: "bigint", nullable: true),
                    tblShobeId = table.Column<long>(type: "bigint", nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AdditionalInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccessTypeEnum = table.Column<int>(type: "int", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblAttachmentAccessLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachmentAccessLog_AccessDateTime",
                table: "tblAttachmentAccessLog",
                column: "AccessDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachmentAccessLog_AccessType",
                table: "tblAttachmentAccessLog",
                column: "AccessType");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachmentAccessLog_AttachmentId",
                table: "tblAttachmentAccessLog",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachmentAccessLog_AttachmentId_AccessDateTime",
                table: "tblAttachmentAccessLog",
                columns: new[] { "AttachmentId", "AccessDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachmentAccessLog_AttachmentPublicId",
                table: "tblAttachmentAccessLog",
                column: "AttachmentPublicId");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachmentAccessLog_IpAddress",
                table: "tblAttachmentAccessLog",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachmentAccessLog_IsSuccess",
                table: "tblAttachmentAccessLog",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachmentAccessLog_PublicId",
                table: "tblAttachmentAccessLog",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachmentAccessLog_tblCustomerId",
                table: "tblAttachmentAccessLog",
                column: "tblCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachmentAccessLog_tblShobeId",
                table: "tblAttachmentAccessLog",
                column: "tblShobeId");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachmentAccessLog_UserId",
                table: "tblAttachmentAccessLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachmentAccessLog_UserId_AccessDateTime",
                table: "tblAttachmentAccessLog",
                columns: new[] { "UserId", "AccessDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachmentAccessLog_UserName",
                table: "tblAttachmentAccessLog",
                column: "UserName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblAttachmentAccessLog");
        }
    }
}
