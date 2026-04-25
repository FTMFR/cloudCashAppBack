using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BnpCashClaudeApp.Persistence.Migrations.CashMigrations
{
    /// <inheritdoc />
    public partial class AddTblCombo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================
            // ایجاد جدول tblCombos
            // ============================================
            migrationBuilder.CreateTable(
                name: "tblCombos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GrpCode = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ZamanInsert = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    TblUserGrpIdInsert = table.Column<long>(type: "bigint", nullable: false),
                    ZamanLastEdit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    TblUserGrpIdLastEdit = table.Column<long>(type: "bigint", nullable: true),
                    IntegrityHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblCombos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblCombos_PublicId",
                table: "tblCombos",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblCombos_GrpCode",
                table: "tblCombos",
                column: "GrpCode");

            // ============================================
            // داده‌های اولیه (Seed Data)
            // ============================================
            var now = "1404/11/15 10:00:00";

            // GrpCode = 5 - انواع سند
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند معكوس", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند اختتامیه", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند اصلاحي", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند افتتاحیه", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند بازخرید کارکنان", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند ذخیره استهلاک", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند سود و زیان", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند عادي", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "بستن باجه ها", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "باز نمودن باجه ها", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دريافت چك واگذاري", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وصول چك واگذاري", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند صدور حواله", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند انتقال باجه", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "كپي سند", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند اينترنتي", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دريافت و پرداخت", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "پرداخت تسهيلات", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "اسناد ضمانتي", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "تقسيم سود", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "واريز سود سپرده", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتقال از نرم افزار قبلي", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "قرعه كشي", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند خودكار", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "واريز گروهي", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند تسويه حساب", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتقال سرفصل", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند برداشت", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند مغايرت وجوه ", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند بستن سپرده قبل از موعد", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سود و زيان انباشته", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "CashLess", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند معكوس صدور حواله", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "پرداخت اقساط توسط ديگران", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "باجه مشتري", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "برداشت هزينه برگشت چك", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "هزينه صدور دفترچه", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وام وجوه", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وب سرويس بانك تجارت", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وب سرويس بانك ملي-ساني", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وب سرويس بانك صادرات-ساني", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وب سرويس بانك ملت-موهب", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وب سرويس بانك آينده", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وب سرويس بانك ملي", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وب سرويس بانك ملت", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وب سرويس بانك پاسارگاد", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "هزينه پيامك", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند انتقال سهام", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند انتقال پايا", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "Kiosk", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وب سرويس شناسه پرداخت", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند Excel", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "اسناد ضمانتي ما نزد ديگران", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند انتقال عضو بين شعب", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند اضافات وام", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند برگشت چك", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند خرج چك", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند عودت چك", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند آسان", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند پرداخت اقساط توسط حساب پشتيبان", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند كمك به خيريه", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "عودت جريمه", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وب سرويس بانك كشاورزي-واريز آني", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وب سرويس بانك رفاه-پايا/ساتنا", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وب سرويس سامان كيش-واريز آني", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "خريد و فروش فروشگاهي", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انتقال مانده مطالبه نشده", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند تعهدي سود دريافتني", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند دريافت هزينه تشكيل پرونده", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند دريافت هزينه ثبت ضامن", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند وجوه بين راه شعب", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "درآمد پيش دريافت كارمزد", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند خاص(غير قابل چيدمان)", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "مغايرت بدهي كارمزد", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وب سرويس شاهين كشاورزي-واريز آني", 5, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "وب سرويس ملي سداد-واريز آني", 5, now, 1L });

            // GrpCode = 6 - نحوه دریافت کارمزد
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "برداشت از حساب", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "تقسیم بین اقساط", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دريافت در شماره اقساط خاص ", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دریافت در آخرین قسط", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دریافت در اقساط استاندارد", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دریافت در اولین قسط", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "كسر از اصل وام", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "مستقل در اولين قسط ", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "نقدي", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "كارمزد سال اول كسر از اصل و ساير در اولين  قسط سال", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "تقسيم نزولي بين اقساط", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "كارمزد سال اول كسر و ساير در اقساط استاندارد", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "كارمزد سال اول كسر و ساير تقسيم بين اقساط", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دريافت در قسط اول با اقساط مساوي", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "مستقل در قسط آخر", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "كارمزد سال اول روي قسط اول و ساير روي قسط هاي اول سال", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دريافت مستقل در اولين قسط راس گيري هر سال", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "تقسيم نزولي بين 4 قسط اول", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "كارمزد سال اول كسر و ساير در فروردين هر سال", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "كارمزد سال اول كسر و ساير در آخرين قسط", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "كل مبلغ قسط از ابتدا", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "كسر كل،امسال درآمد،الباقي آتي", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "كارمزد سال اول كسر و ساير مستقل اقساط استاندارد", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "تقسيم بين اقساط امسال درآمد مابقي آتي به تفكيك سال", 6, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دريافت در اقساط استاندارد امسال درآمد مابقي آتي به تفكيك سال", 6, now, 1L });

            // GrpCode = 7 - نحوه پرداخت وام
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "چك يا حواله", 7, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "نقدی", 7, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "واریز به حساب پس انداز", 7, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "واريز به كوتاه مدت", 7, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "واريز به حساب جاري", 7, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "چك و پول", 7, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "رفاهي و خدماتي", 7, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "واريز به حساب ديگر", 7, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "واريز به سرفصل واسط", 7, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "صدور ضمانتنامه براي ذينفع", 7, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "كارتابل حواله/چك", 7, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "چك و حساب پس انداز", 7, now, 1L });

            // GrpCode = 10 - نقش در وام
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "ضامن اصلي", 10, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "ضامن فرعي", 10, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "معرف", 10, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "صاحب چك در وجه", 10, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سرپرست", 10, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "معرف مسدودي", 10, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انسداد حساب", 10, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "معرف دوم", 10, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "معرف سوم", 10, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "ضامن سوم", 10, now, 1L });

            // GrpCode = 12 - وضعیت عضویت
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "تسويه شده", 12, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "راكد", 12, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سفارشي", 12, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "غير فعال", 12, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "فعال", 12, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "بد حساب", 12, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "فوت شده", 12, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "مسدود", 12, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "مرخصي بدون حقوق", 12, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "درحال تكميل", 12, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "تعليق", 12, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "تكرار", 12, now, 1L });

            // GrpCode = 13 - جنسیت
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "زن", 13, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "مرد", 13, now, 1L });

            // GrpCode = 14 - نوع وثیقه
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "چك", 14, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سفته", 14, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "كسر حقوق/حكم كارگزيني", 14, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "مسكوكات و طلاجات", 14, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سپرده", 14, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند ملكي و مسكوني", 14, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "قرارداد", 14, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "پروانه كسب", 14, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "اسناد تضميني مرتبط ", 14, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "ارز", 14, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "اشتغال به تحصيل", 14, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند ازدواج", 14, now, 1L });

            // GrpCode = 15 - وضعیت زیرگروه
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "داراي زیرگروه", 15, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "فاقد زیر گروه", 15, now, 1L });

            // GrpCode = 16 - وضعیت گردش حساب
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "آزاد - بدون محدودیت گردش یا مانده", 16, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "فقط گردش بدهکار قابل قبول است", 16, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "فقط گردش بستانکار قابل قبول است", 16, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "مانده باید بدهکار باشد", 16, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "مانده باید بستانکار باشد", 16, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "هشدار اگر مي خواهد بستانكار شود", 16, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "هشدار اگر مي خواهد بدهكار شود", 16, now, 1L });

            // GrpCode = 17 - وضعیت پرونده
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "بایگانی", 17, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "فعال", 17, now, 1L });

            // GrpCode = 19 - وضعیت تاهل
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "متاهل", 19, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "مجرد", 19, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "حضانت", 19, now, 1L });

            // GrpCode = 20 - زمان آزادسازی وثیقه
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "پس از تسويه تسهيلات", 20, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "پس از وصول اقساط صندوق", 20, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "قسط به قسط", 20, now, 1L });

            // GrpCode = 21 - نوع فرم
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "پرداخت وام", 21, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دريافت و پرداخت", 21, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند ضمانتي دريافتي", 21, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سند مالي", 21, now, 1L });

            // GrpCode = 24 - نوع شخصیت
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "حقيقي", 24, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "حقوقي", 24, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "مشترك", 24, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "سرپرست", 24, now, 1L });

            // GrpCode = 29 - نوع پرداخت
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "نقدي", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "فيش بانكي/ كارت خوان", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "از حساب/انتقالي", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "تركيبي(نقدي و فيش بانكي)", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "فرم واريز", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "تركيبي(نقدي و انتقالي)", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "فرم سند ", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "برداشت نقدي", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "برداشت بدون دفترچه", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "برداشت توسط چك", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "برداشت انتقالي", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "برداشت اينترنتي", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "واريز نقدي", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "واريز با پوز", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "واريز بدون دفترچه", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "واريز با حواله بانكي", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "واريز انتقالي", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "واريز همراه بانك", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "واريز اينترنت بانك", 29, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "واريز ATM", 29, now, 1L });

            // GrpCode = 31 - وضعیت درخواست
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "در انتظار انجام", 31, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "در حال انجام", 31, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "انجام شده", 31, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "عودت شده", 31, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "باطل شده", 31, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "ثبت درخواست", 31, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "تأييد مشتري", 31, now, 1L });

            // GrpCode = 316 - مدرک تحصیلی
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "بيسواد", 316, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "ابتدايي", 316, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "راهنمايي", 316, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "كارشناسي", 316, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "كارشناسي ارشد", 316, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "دكترا", 316, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "ديپلم", 316, now, 1L });
            migrationBuilder.InsertData("tblCombos", new[] { "Title", "GrpCode", "ZamanInsert", "TblUserGrpIdInsert" }, new object[] { "فوق ديپلم", 316, now, 1L });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblCombos");
        }
    }
}
