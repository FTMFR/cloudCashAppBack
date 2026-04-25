using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCryptographicKeysTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "PublicId",
                table: "tblUserGrps",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "PublicId",
                table: "tblMenuPermissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "PublicId",
                table: "tblGrpPermissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateTable(
                name: "CryptographicKeys",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EncryptedKeyValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EncryptionIV = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KeyLengthBits = table.Column<int>(type: "int", nullable: false),
                    Algorithm = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeactivationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DestroyedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DestructionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    PreviousKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReplacedByKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GracePeriodEndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KeyHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UsageCount = table.Column<long>(type: "bigint", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CryptographicKeys", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CryptographicKeys_KeyId",
                table: "CryptographicKeys",
                column: "KeyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CryptographicKeys_PublicId",
                table: "CryptographicKeys",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CryptographicKeys_Purpose",
                table: "CryptographicKeys",
                column: "Purpose");

            migrationBuilder.CreateIndex(
                name: "IX_CryptographicKeys_Purpose_Status",
                table: "CryptographicKeys",
                columns: new[] { "Purpose", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CryptographicKeys");

            migrationBuilder.AlterColumn<Guid>(
                name: "PublicId",
                table: "tblUserGrps",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "NEWID()");

            migrationBuilder.AlterColumn<Guid>(
                name: "PublicId",
                table: "tblMenuPermissions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "NEWID()");

            migrationBuilder.AlterColumn<Guid>(
                name: "PublicId",
                table: "tblGrpPermissions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "NEWID()");
        }
    }
}
