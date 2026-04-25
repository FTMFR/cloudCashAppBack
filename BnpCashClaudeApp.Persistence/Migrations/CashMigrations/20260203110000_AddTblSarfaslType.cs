using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations.CashMigrations
{
    /// <inheritdoc />
    public partial class AddTblSarfaslType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================
            // ایجاد جدول tblSarfaslTypes
            // ============================================
            migrationBuilder.CreateTable(
                name: "tblSarfaslTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblSarfaslTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblSarfaslTypes_PublicId",
                table: "tblSarfaslTypes",
                column: "PublicId",
                unique: true);

            // ============================================
            // داده‌های اولیه (Seed Data) - 80 رکورد
            // ============================================
            var now = "1404/11/15 11:00:00";

            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "تسهيلات", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "حساب پس انداز", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "صندوق", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "بانك", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "کارمزد", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سپرده", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "باجه", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "درآمد", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "هزينه", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سود و زيان", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "اسناد دريافتني", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "حساب جاری", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "حقوق صاحبان سهام", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دارائي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "بدهي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "حسابهاي كنترلي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "طرف انتظامي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي ديگران نزد ما", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي ما نزد ديگران", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "اسناد پرداختني", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "چكهاي واگذاري", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دريافت كارمزد روي اقساط", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دريافت بيمه روي اقساط", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "بين شعب", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وجوه اداره شده", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سرفصل واسط", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "بيمه", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "در جريان وصول", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "تنخواه", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "هزينه وصول چك", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "ارزش افزوده دريافتي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "ارزش افزوده آتي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي - تعدادي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي چك- تعدادي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي سفته- تعدادي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي كسر حقوق- تعدادي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي مسكوكات و طلاجات- تعدادي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي سند ملكي و مسكوني- تعدادي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي قرارداد- تعدادي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "معوقات", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "پيش پرداخت", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انباشته", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "درآمدها", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "هزينه ها", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "بستانكار مغايرت اقساط", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وام وجوه", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "پيش دريافت كارمزد", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سهام تعدادي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سهام ريالي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "هزينه شناسه قبوض", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دريافتني لاوصول", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دريافتني لاوصول نزد صندوق", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دريافتني در جريان وصول نزد سايرين", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دريافتني در جريان وصول نزد غيرعملياتي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "اسناد پرداختني غير عملياتي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "حسابهاي پرداختني غير عملياتي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "اسناد پرداختني به بستانكاران", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "مجموع سرفصل", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وجوه اداره شده كل", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "تعميم مدل پاياپاي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "ساير بستانكاران", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "اسناد انتظامي تسويه شده", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي چك الباقي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "طرف انتظامي چك الباقي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "طرف انتظامي ما نزد ديگران", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "طرف انتظامي تسويه شده", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وجوه بين راه", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "مانده مطالبه نشده", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "درآمد مانده مطالبه نشده", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "داشبورد دارايي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "داشبورد بدهي", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "داشبورد حقوق صاحبان سهام", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي- چك", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي- سفته", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي- كسر حقوق", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي- مسكوكات و طلاجات", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي- سپرده", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي- سند ملكي و مسكوني", now, 1L });
            migrationBuilder.InsertData("tblSarfaslTypes", new[] { "Title", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتظامي- قرارداد", now, 1L });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblSarfaslTypes");
        }
    }
}
