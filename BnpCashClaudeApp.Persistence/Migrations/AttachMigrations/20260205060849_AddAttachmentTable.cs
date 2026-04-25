using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations.AttachMigrations
{
    /// <inheritdoc />
    public partial class AddAttachmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblAttachment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StorageType = table.Column<int>(type: "int", nullable: false),
                    FileData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    AttachmentType = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<long>(type: "bigint", nullable: true),
                    EntityPublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SensitivityLevel = table.Column<int>(type: "int", nullable: false),
                    SecurityClassification = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SecurityLabels = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    EncryptionAlgorithm = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EncryptionKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EncryptionIV = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HashAlgorithm = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DigitalSignature = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastIntegrityCheckAt = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    LastIntegrityCheckResult = table.Column<bool>(type: "bit", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsVirusScanned = table.Column<bool>(type: "bit", nullable: false),
                    VirusScanResult = table.Column<bool>(type: "bit", nullable: true),
                    VirusScannedAt = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    DownloadCount = table.Column<int>(type: "int", nullable: false),
                    LastAccessedAt = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    LastAccessedFromIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    LastAccessedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ExpiresAt = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    AutoDeleteAt = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    IsDeletable = table.Column<bool>(type: "bit", nullable: false),
                    tblCustomerId = table.Column<long>(type: "bigint", nullable: true),
                    tblShobeId = table.Column<long>(type: "bigint", nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblAttachment", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachment_AttachmentType",
                table: "tblAttachment",
                column: "AttachmentType");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachment_Category",
                table: "tblAttachment",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachment_ContentHash",
                table: "tblAttachment",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachment_EntityPublicId",
                table: "tblAttachment",
                column: "EntityPublicId");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachment_EntityType_EntityId",
                table: "tblAttachment",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachment_PublicId",
                table: "tblAttachment",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachment_SensitivityLevel",
                table: "tblAttachment",
                column: "SensitivityLevel");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachment_Status",
                table: "tblAttachment",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachment_StoredFileName",
                table: "tblAttachment",
                column: "StoredFileName");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachment_tblCustomerId",
                table: "tblAttachment",
                column: "tblCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachment_tblShobeId",
                table: "tblAttachment",
                column: "tblShobeId");

            migrationBuilder.CreateIndex(
                name: "IX_tblAttachment_ZamanInsert",
                table: "tblAttachment",
                column: "ZamanInsert");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblAttachment");
        }
    }
}
