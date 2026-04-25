using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations.CashMigrations
{
    /// <inheritdoc />
    public partial class AddTblSarfaslAndProtocol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================
            // ایجاد جدول tblSarfaslProtocols
            // ============================================
            migrationBuilder.CreateTable(
                name: "tblSarfaslProtocols",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultSarfaslJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblSarfaslProtocols", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblSarfaslProtocols_PublicId",
                table: "tblSarfaslProtocols",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblSarfaslProtocols_Code",
                table: "tblSarfaslProtocols",
                column: "Code",
                unique: true);

            // ============================================
            // ایجاد جدول tblSarfasls
            // ============================================
            migrationBuilder.CreateTable(
                name: "tblSarfasls",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tblShobeId = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    tblSarfaslTypeId = table.Column<long>(type: "bigint", nullable: true),
                    tblSarfaslProtocolId = table.Column<long>(type: "bigint", nullable: true),
                    CodeSarfasl = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WithJoze = table.Column<bool>(type: "bit", nullable: false),
                    tblComboIdVazeiatZirGrp = table.Column<long>(type: "bigint", nullable: true),
                    TedadArghamZirGrp = table.Column<int>(type: "int", nullable: true),
                    MizanEtebarBedehkar = table.Column<decimal>(type: "decimal(18,0)", nullable: false, defaultValue: 0m),
                    MizanEtebarBestankar = table.Column<decimal>(type: "decimal(18,0)", nullable: false, defaultValue: 0m),
                    tblComboIdControlAmaliat = table.Column<long>(type: "bigint", nullable: true),
                    NotShowInTaraz = table.Column<bool>(type: "bit", nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblSarfasls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tblSarfasls_tblSarfasls_ParentId",
                        column: x => x.ParentId,
                        principalTable: "tblSarfasls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblSarfasls_tblSarfaslTypes_tblSarfaslTypeId",
                        column: x => x.tblSarfaslTypeId,
                        principalTable: "tblSarfaslTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblSarfasls_tblSarfaslProtocols_tblSarfaslProtocolId",
                        column: x => x.tblSarfaslProtocolId,
                        principalTable: "tblSarfaslProtocols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblSarfasls_PublicId",
                table: "tblSarfasls",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblSarfasls_tblShobeId",
                table: "tblSarfasls",
                column: "tblShobeId");

            migrationBuilder.CreateIndex(
                name: "IX_tblSarfasls_CodeSarfasl",
                table: "tblSarfasls",
                column: "CodeSarfasl");

            migrationBuilder.CreateIndex(
                name: "IX_tblSarfasls_ParentId",
                table: "tblSarfasls",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_tblSarfasls_tblSarfaslTypeId",
                table: "tblSarfasls",
                column: "tblSarfaslTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tblSarfasls_tblSarfaslProtocolId",
                table: "tblSarfasls",
                column: "tblSarfaslProtocolId");

            // ============================================
            // داده‌های اولیه پروتکل پیش‌فرض
            // ============================================
            var now = "1404/11/15 12:00:00";

            migrationBuilder.InsertData("tblSarfaslProtocols", 
                new[] { "Title", "Code", "Description", "IsDefault", "IsActive", "ZamanInsert", "TblUserGrpIdInsert" }, 
                new object[] { "پروتکل پیش‌فرض سیستم", "DEFAULT", "سرفصل‌های پیش‌فرض سیستم قرض‌الحسنه", true, true, now, 1L });

            // ============================================
            // داده‌های اولیه سرفصل‌ها - سطح 1 (ریشه‌ها)
            // ============================================
            // توجه: Id ها دستی تنظیم شده‌اند تا روابط ParentId درست باشد
            
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT tblSarfasls ON;

DECLARE @now NVARCHAR(25) = '1404/11/15 12:00:00';
DECLARE @protocolId BIGINT = 1;

-- سطح 1 - سرفصل‌های اصلی
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100311, 1, NULL, 15, @protocolId, '11', N'دارائيها', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100312, 1, NULL, 16, @protocolId, '12', N'بدهيها', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100313, 1, NULL, 8, @protocolId, '13', N'درآمد ها', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100314, 1, NULL, 9, @protocolId, '14', N'هزينه ها', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100315, 1, NULL, NULL, @protocolId, '15', N'سود و زيان', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100316, 1, NULL, NULL, @protocolId, '16', N'سرمايه', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100317, 1, NULL, NULL, @protocolId, '17', N'حسابهاي انتظامي', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 2 - زیرمجموعه دارایی‌ها (11)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100318, 1, 100311, NULL, @protocolId, '1101', N'موجودي نقد', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100319, 1, 100311, NULL, @protocolId, '1102', N'بانكها', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100320, 1, 100311, NULL, @protocolId, '1103', N'بدهكاران', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100321, 1, 100311, NULL, @protocolId, '1104', N'اموال', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100322, 1, 100311, NULL, @protocolId, '1105', N'اسناد درجريان وصول', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100323, 1, 100311, NULL, @protocolId, '1106', N'دارايي هاي نامشهود', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100324, 1, 100311, NULL, @protocolId, '1107', N'پيش پرداخت', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 2 - زیرمجموعه بدهی‌ها (12)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100327, 1, 100312, NULL, @protocolId, '1201', N'پس انداز ها', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100328, 1, 100312, NULL, @protocolId, '1202', N'جاري ها', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100325, 1, 100312, NULL, @protocolId, '1203', N'بستانكاران', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100326, 1, 100312, NULL, @protocolId, '1204', N'ذخاير', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100414, 1, 100312, NULL, @protocolId, '1205', N'اسناد', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100339, 1, 100312, NULL, @protocolId, '1206', N'تسهيلات دريافتي ازبانكها', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100418, 1, 100312, 23, @protocolId, '1207', N'كارمزد اقساط تحقق نيافته', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 2 - زیرمجموعه درآمدها (13)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100329, 1, 100313, NULL, @protocolId, '1301', N'درآمد عملياتي', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100330, 1, 100313, NULL, @protocolId, '1302', N'درآمدهاي غير عملياتي', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 2 - زیرمجموعه هزینه‌ها (14)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100331, 1, 100314, NULL, @protocolId, '1401', N'هزينه حقوق و دستمزد', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100332, 1, 100314, NULL, @protocolId, '1402', N'هزينه هاي عمومي', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100333, 1, 100314, NULL, @protocolId, '1403', N'ماليات و بيمه', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 2 - زیرمجموعه سود و زیان (15)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100334, 1, 100315, 10, @protocolId, '1501', N'سود و زيان جاري', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100335, 1, 100315, 43, @protocolId, '1502', N'سود و زيان انباشته', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 2 - زیرمجموعه سرمایه (16)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100336, 1, 100316, NULL, @protocolId, '1601', N'سرمايه اوليه', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 2 - زیرمجموعه انتظامی (17)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100337, 1, 100317, 11, @protocolId, '1701', N'حسابهاي انتظامي', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1),
(100338, 1, 100317, NULL, @protocolId, '1702', N'طرف حساب انتظامي', N'', 0, 4047, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه موجودی نقد (1101)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100340, 1, 100318, 3, @protocolId, '110101', N'صندوق', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100413, 1, 100318, 7, @protocolId, '110102', N'باجه 1', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(101403, 1, 100318, NULL, @protocolId, '110103', N'اوراق بهادار مشاركتي', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه بانک‌ها (1102)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100341, 1, 100319, 4, @protocolId, '110201', N'بانك ملي', NULL, 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(102295, 1, 100319, 4, @protocolId, '110202', N'بانك ملت', NULL, 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(102297, 1, 100319, 4, @protocolId, '110203', N'بانك صادرات', NULL, 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه بدهکاران (1103)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100342, 1, 100320, 1, @protocolId, '110301', N'وام عادي', N'', 1, 4046, 2, 0, 0, 4052, 0, @now, 1),
(101405, 1, 100320, 1, @protocolId, '110302', N'مطالبات سر رسيد گذشته', N'', 1, 4046, 2, 0, 0, 4052, 0, @now, 1),
(101434, 1, 100320, 1, @protocolId, '110303', N'مطالبات مشكوك الوصول', N'', 1, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه اموال (1104)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100344, 1, 100321, NULL, @protocolId, '110401', N'زمين', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100343, 1, 100321, NULL, @protocolId, '110402', N'اثاثيه و منصوبات', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100345, 1, 100321, NULL, @protocolId, '110403', N'ساختمان', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100346, 1, 100321, NULL, @protocolId, '110404', N'وسائط نقليه', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه اسناد درجریان وصول (1105)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100384, 1, 100322, NULL, @protocolId, '110501', N'وصول كننده-شخص', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100385, 1, 100322, NULL, @protocolId, '110508', N'راننده اژانس', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100415, 1, 100322, 12, @protocolId, '110509', N'اسناد دريافتني', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(101301, 1, 100322, 29, @protocolId, '110510', N'در جريان وصول', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه دارایی‌های نامشهود (1106)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100386, 1, 100323, NULL, @protocolId, '110601', N'حق الامتيازاب وفاضلاب', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100387, 1, 100323, NULL, @protocolId, '110602', N'حق الامتياز برق', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100388, 1, 100323, NULL, @protocolId, '110603', N'حق الامتياز گاز', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100389, 1, 100323, NULL, @protocolId, '110604', N'حق الامتياز تلفن ثابت', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100390, 1, 100323, NULL, @protocolId, '110605', N'حق الامتياز تلفن همراه', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100391, 1, 100323, NULL, @protocolId, '110606', N'سهام بانك صادرات', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه پیش پرداخت (1107)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100392, 1, 100324, NULL, @protocolId, '110701', N'موسسه حسابرسي يك', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100393, 1, 100324, NULL, @protocolId, '110702', N'موسسه دو', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100394, 1, 100324, NULL, @protocolId, '110703', N'پيش پرداخت خريد', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه پس‌انداز (1201)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100411, 1, 100327, 2, @protocolId, '120102', N'پس انداز', N'', 1, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه جاری (1202)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100412, 1, 100328, 13, @protocolId, '120201', N'جاري', N'', 1, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه بستانکاران (1203)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100347, 1, 100325, NULL, @protocolId, '120301', N'بستانكاران خريد دارايي', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100348, 1, 100325, NULL, @protocolId, '120302', N'بستانكاران تامين اجتماعي', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100349, 1, 100325, NULL, @protocolId, '120303', N'بستانكاران اداره دارايي', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100350, 1, 100325, NULL, @protocolId, '120304', N'ساير بستانكاران', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100353, 1, 100325, NULL, @protocolId, '120305', N'بستانكاران بانكي (مربوط به كانون حسنات', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100351, 1, 100325, NULL, @protocolId, '120306', N'استهلاك انباشته', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100352, 1, 100325, NULL, @protocolId, '120307', N'هيئت مديره صندوق', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه ذخایر (1204)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100398, 1, 100326, NULL, @protocolId, '120401', N'ذخيره مطالبات مشكوك الوصول', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100399, 1, 100326, NULL, @protocolId, '120402', N'ذخيره استهلاك اثاثيه', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100400, 1, 100326, NULL, @protocolId, '120403', N'استهلاك انباشته ساختمان', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100401, 1, 100326, NULL, @protocolId, '120404', N'استهاك انباشته اثاثيه و منصوبات', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100402, 1, 100326, NULL, @protocolId, '120405', N'استهلاك انباشته وسايل نقليه', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100403, 1, 100326, NULL, @protocolId, '120406', N'ذخيره هزينه هاي تعلق گرفته پرداخت نشده', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100404, 1, 100326, NULL, @protocolId, '120407', N'ذخيره ماليات عملكرد', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه اسناد (1205)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100417, 1, 100414, 22, @protocolId, '120502', N'چكهاي واگذاري', N'', 1, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100416, 1, 100414, 21, @protocolId, '120503', N'اسناد پرداختني', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه تسهیلات دریافتی (1206)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100407, 1, 100339, NULL, @protocolId, '120601', N'تسهيلات دريافتي از بانك', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه درآمد عملیاتی (1301)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100354, 1, 100329, 5, @protocolId, '130102', N'كارمزدوامها', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100355, 1, 100329, NULL, @protocolId, '130103', N'سود سپرده بانكي', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه درآمد غیرعملیاتی (1302)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100356, 1, 100330, NULL, @protocolId, '130201', N'درامد اجاره', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100357, 1, 100330, NULL, @protocolId, '130202', N'درامد متفرقه', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه هزینه حقوق و دستمزد (1401)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100358, 1, 100331, NULL, @protocolId, '140101', N'هزينه حقوق كارمندان', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100359, 1, 100331, NULL, @protocolId, '140102', N'هزينه بيمه حقوق', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100360, 1, 100331, NULL, @protocolId, '140103', N'هزينه ماليات حقوق', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100361, 1, 100331, NULL, @protocolId, '140104', N'هزينه سنوات كارمندان', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه هزینه‌های عمومی (1402)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100362, 1, 100332, NULL, @protocolId, '140201', N'هزينه آب وبرق وتلفن وگاز', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100365, 1, 100332, NULL, @protocolId, '140202', N'هزينه تعمير و نگهداري دارايي', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100366, 1, 100332, NULL, @protocolId, '140203', N'هزينه استهلاك', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100363, 1, 100332, NULL, @protocolId, '140204', N'هزينه پذيرايي و تشريفات', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100367, 1, 100332, NULL, @protocolId, '140205', N'هزينه اياب و ذهاب', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100364, 1, 100332, NULL, @protocolId, '140206', N'هزينه ملزومات مصرفي', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100368, 1, 100332, NULL, @protocolId, '140207', N'هزينه چاپ و تكثير', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100369, 1, 100332, NULL, @protocolId, '140208', N'هزينه مطالبات سوخت شده', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100370, 1, 100332, NULL, @protocolId, '140209', N'هزينه سوخت', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100371, 1, 100332, NULL, @protocolId, '140210', N'هزينه نظافت و بهداشت', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100372, 1, 100332, NULL, @protocolId, '140211', N'هزينه انعام و كمك هاي نقدي', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100373, 1, 100332, NULL, @protocolId, '140212', N'هزينه كارمزد خدمات بانكي', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100374, 1, 100332, NULL, @protocolId, '140213', N'هزينه اينترنت و شبكه', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100375, 1, 100332, NULL, @protocolId, '140214', N'هزينه متفرقه', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100376, 1, 100332, NULL, @protocolId, '140215', N'هزينه اموزشي علمي و تحقيقاتي', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100377, 1, 100332, NULL, @protocolId, '140216', N'اضافه كاري وصندوق داري كارمندان', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100379, 1, 100332, NULL, @protocolId, '140217', N'هزينه مطالبات مشكوك الوصول', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100380, 1, 100332, NULL, @protocolId, '140218', N'هزينه ماليات', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100381, 1, 100332, NULL, @protocolId, '140219', N'هزينه حسابرسي', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100378, 1, 100332, NULL, @protocolId, '140220', N'هزينه خدماتي', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(201413, 1, 100332, NULL, @protocolId, '140221', N'هزينه پيامك', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه مالیات و بیمه (1403)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100383, 1, 100333, NULL, @protocolId, '140301', N'بيمه هاي صندوق', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100382, 1, 100333, NULL, @protocolId, '140303', N'هزينه بيمه كارمندان', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه سود و زیان جاری (1501)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100395, 1, 100334, NULL, @protocolId, '150101', N'سود وزيان جاري', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه سود و زیان انباشته (1502)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100396, 1, 100335, NULL, @protocolId, '150201', N'سود و زيان انباشته', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه سرمایه اولیه (1601)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100397, 1, 100336, NULL, @protocolId, '160101', N'سرمايه اوليه', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه حسابهای انتظامی (1701)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100405, 1, 100337, 19, @protocolId, '170101', N'حسابهاي انتظامي ديگران نزدما', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100406, 1, 100337, 20, @protocolId, '170102', N'حسابهاي انتظامي مانزدديگران', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

-- سطح 3 - زیرمجموعه طرف حساب انتظامی (1702)
INSERT INTO tblSarfasls (Id, tblShobeId, ParentId, tblSarfaslTypeId, tblSarfaslProtocolId, CodeSarfasl, Title, Description, WithJoze, tblComboIdVazeiatZirGrp, TedadArghamZirGrp, MizanEtebarBedehkar, MizanEtebarBestankar, tblComboIdControlAmaliat, NotShowInTaraz, ZamanInsert, TblUserGrpIdInsert)
VALUES
(100409, 1, 100338, 18, @protocolId, '170201', N'طرف حساب انتظامي ديگران نزد ما', N'', 1, 4046, 2, 0, 0, 4052, 0, @now, 1),
(100410, 1, 100338, NULL, @protocolId, '170202', N'طرف حساب انتظامي مانزد ديگران', N'', 0, 4046, 2, 0, 0, 4052, 0, @now, 1);

SET IDENTITY_INSERT tblSarfasls OFF;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblSarfasls");

            migrationBuilder.DropTable(
                name: "tblSarfaslProtocols");
        }
    }
}
