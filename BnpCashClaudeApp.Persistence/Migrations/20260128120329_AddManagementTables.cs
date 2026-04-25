using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManagementTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblCustomers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CustomerCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerType = table.Column<int>(type: "int", nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompanyNationalId = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    EconomicCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ManagerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Fax = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MembershipDate = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LoyaltyPoints = table.Column<int>(type: "int", nullable: true),
                    CustomerLevel = table.Column<int>(type: "int", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCustomers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tblSoftwares",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DownloadUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblSoftwares", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tblCustomerContacts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tblCustomerId = table.Column<long>(type: "bigint", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactType = table.Column<int>(type: "int", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Messenger = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCustomerContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblCustomerContacts_tblCustomers_tblCustomerId",
                        column: x => x.tblCustomerId,
                        principalTable: "tblCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblDbs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tblCustomerId = table.Column<long>(type: "bigint", nullable: true),
                    tblSoftwareId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DbCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ServerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: true),
                    DatabaseName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EncryptedPassword = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EncryptedConnectionString = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DbType = table.Column<int>(type: "int", nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: false),
                    MaxSizeMB = table.Column<int>(type: "int", nullable: true),
                    CurrentSizeMB = table.Column<int>(type: "int", nullable: true),
                    LastBackupDate = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    LastConnectionTestDate = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    LastConnectionTestResult = table.Column<bool>(type: "bit", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblDbs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblDbs_tblCustomers_tblCustomerId",
                        column: x => x.tblCustomerId,
                        principalTable: "tblCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tblDbs_tblSoftwares_tblSoftwareId",
                        column: x => x.tblSoftwareId,
                        principalTable: "tblSoftwares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblPlans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tblSoftwareId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MaxMemberCount = table.Column<int>(type: "int", nullable: true),
                    MaxUserCount = table.Column<int>(type: "int", nullable: true),
                    MaxBranchCount = table.Column<int>(type: "int", nullable: true),
                    MaxDbSizeMB = table.Column<int>(type: "int", nullable: true),
                    MaxDailyTransactions = table.Column<int>(type: "int", nullable: true),
                    FeaturesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BasePrice = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    YearlyPrice = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    PlanType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblPlans_tblSoftwares_tblSoftwareId",
                        column: x => x.tblSoftwareId,
                        principalTable: "tblSoftwares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblCustomerSoftwares",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tblCustomerId = table.Column<long>(type: "bigint", nullable: false),
                    tblSoftwareId = table.Column<long>(type: "bigint", nullable: false),
                    tblPlanId = table.Column<long>(type: "bigint", nullable: false),
                    LicenseKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LicenseCount = table.Column<int>(type: "int", nullable: false),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    EndDate = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    SubscriptionType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InstalledVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LastActivationDate = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    LastActivationIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ActivationCount = table.Column<int>(type: "int", nullable: false),
                    MaxActivations = table.Column<int>(type: "int", nullable: true),
                    CustomSettingsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    DiscountPercent = table.Column<int>(type: "int", nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCustomerSoftwares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblCustomerSoftwares_tblCustomers_tblCustomerId",
                        column: x => x.tblCustomerId,
                        principalTable: "tblCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblCustomerSoftwares_tblPlans_tblPlanId",
                        column: x => x.tblPlanId,
                        principalTable: "tblPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblCustomerSoftwares_tblSoftwares_tblSoftwareId",
                        column: x => x.tblSoftwareId,
                        principalTable: "tblSoftwares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblCustomerSoftwareDbs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tblCustomerSoftwareId = table.Column<long>(type: "bigint", nullable: false),
                    tblDbId = table.Column<long>(type: "bigint", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    UsageType = table.Column<int>(type: "int", nullable: false),
                    ConnectedDate = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCustomerSoftwareDbs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblCustomerSoftwareDbs_tblCustomerSoftwares_tblCustomerSoftwareId",
                        column: x => x.tblCustomerSoftwareId,
                        principalTable: "tblCustomerSoftwares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblCustomerSoftwareDbs_tblDbs_tblDbId",
                        column: x => x.tblDbId,
                        principalTable: "tblDbs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerContacts_IsActive",
                table: "tblCustomerContacts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerContacts_PublicId",
                table: "tblCustomerContacts",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerContacts_tblCustomerId",
                table: "tblCustomerContacts",
                column: "tblCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomers_CompanyNationalId",
                table: "tblCustomers",
                column: "CompanyNationalId");

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomers_CustomerCode",
                table: "tblCustomers",
                column: "CustomerCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomers_Name",
                table: "tblCustomers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomers_NationalId",
                table: "tblCustomers",
                column: "NationalId");

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomers_PublicId",
                table: "tblCustomers",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomers_Status",
                table: "tblCustomers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerSoftwareDbs_IsActive",
                table: "tblCustomerSoftwareDbs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerSoftwareDbs_PublicId",
                table: "tblCustomerSoftwareDbs",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerSoftwareDbs_tblCustomerSoftwareId_tblDbId",
                table: "tblCustomerSoftwareDbs",
                columns: new[] { "tblCustomerSoftwareId", "tblDbId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerSoftwareDbs_tblDbId",
                table: "tblCustomerSoftwareDbs",
                column: "tblDbId");

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerSoftwares_LicenseKey",
                table: "tblCustomerSoftwares",
                column: "LicenseKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerSoftwares_PublicId",
                table: "tblCustomerSoftwares",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerSoftwares_Status",
                table: "tblCustomerSoftwares",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerSoftwares_tblCustomerId_tblSoftwareId",
                table: "tblCustomerSoftwares",
                columns: new[] { "tblCustomerId", "tblSoftwareId" });

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerSoftwares_tblPlanId",
                table: "tblCustomerSoftwares",
                column: "tblPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomerSoftwares_tblSoftwareId",
                table: "tblCustomerSoftwares",
                column: "tblSoftwareId");

            migrationBuilder.CreateIndex(
                name: "IX_tblDbs_DbCode",
                table: "tblDbs",
                column: "DbCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblDbs_PublicId",
                table: "tblDbs",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblDbs_Status",
                table: "tblDbs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tblDbs_tblCustomerId",
                table: "tblDbs",
                column: "tblCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_tblDbs_tblSoftwareId",
                table: "tblDbs",
                column: "tblSoftwareId");

            migrationBuilder.CreateIndex(
                name: "IX_tblPlans_IsActive",
                table: "tblPlans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_tblPlans_PublicId",
                table: "tblPlans",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblPlans_tblSoftwareId_Code",
                table: "tblPlans",
                columns: new[] { "tblSoftwareId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblSoftwares_Code",
                table: "tblSoftwares",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblSoftwares_IsActive",
                table: "tblSoftwares",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_tblSoftwares_Name",
                table: "tblSoftwares",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_tblSoftwares_PublicId",
                table: "tblSoftwares",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblCustomerContacts");

            migrationBuilder.DropTable(
                name: "tblCustomerSoftwareDbs");

            migrationBuilder.DropTable(
                name: "tblCustomerSoftwares");

            migrationBuilder.DropTable(
                name: "tblDbs");

            migrationBuilder.DropTable(
                name: "tblPlans");

            migrationBuilder.DropTable(
                name: "tblCustomers");

            migrationBuilder.DropTable(
                name: "tblSoftwares");
        }
    }
}
