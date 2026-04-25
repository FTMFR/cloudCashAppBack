using BnpCashClaudeApp.Domain.Entities.CashSubsystem;
using Microsoft.EntityFrameworkCore;
using System;

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <summary>
    /// DbContext برای دیتابیس قرض‌الحسنه / تعاونی اعتبار
    /// دیتابیس: BnpCashCloudDB
    /// ============================================
    /// این Context برای مدیریت جداول مربوط به:
    /// - انواع مشتری (TafsiliType)
    /// - انواع حوزه (AzaNoe)
    /// - اعضا
    /// - حساب‌ها
    /// - تراکنش‌ها
    /// - وام‌ها
    /// - سپرده‌ها
    /// و سایر عملیات مالی قرض‌الحسنه استفاده می‌شود
    /// ============================================
    /// </summary>
    public class CashDbContext : DbContext
    {
        public CashDbContext(DbContextOptions<CashDbContext> options) : base(options) { }

        // ============================================
        // جداول اطلاعات پایه - تنظیمات اولیه
        // ============================================
        
        /// <summary>
        /// انواع مشتری (انواع تفصیلی)
        /// </summary>
        public DbSet<tblTafsiliType> tblTafsiliTypes { get; set; }
        
        /// <summary>
        /// انواع حوزه (دسته‌بندی)
        /// </summary>
        public DbSet<tblAzaNoe> tblAzaNoes { get; set; }

        /// <summary>
        /// جدول Combo - دسته‌بندی‌های عمومی
        /// </summary>
        public DbSet<tblCombo> tblCombos { get; set; }

        /// <summary>
        /// جدول نوع سرفصل
        /// </summary>
        public DbSet<tblSarfaslType> tblSarfaslTypes { get; set; }

        /// <summary>
        /// جدول پروتکل سرفصل
        /// </summary>
        public DbSet<tblSarfaslProtocol> tblSarfaslProtocols { get; set; }

        /// <summary>
        /// جدول سرفصل‌های حسابداری
        /// </summary>
        public DbSet<tblSarfasl> tblSarfasls { get; set; }

        // ============================================
        // TODO: جداول بعدی قرض‌الحسنه اینجا اضافه می‌شوند
        // ============================================
        // public DbSet<tblMember> tblMembers { get; set; }
        // public DbSet<tblAccount> tblAccounts { get; set; }
        // public DbSet<tblTransaction> tblTransactions { get; set; }
        // public DbSet<tblLoan> tblLoans { get; set; }
        // public DbSet<tblDeposit> tblDeposits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================
            // تنظیم Collation فارسی برای پشتیبانی صحیح از حروف ی و ک
            // Persian_100_CI_AS: Case Insensitive, Accent Sensitive
            // ============================================
            modelBuilder.UseCollation("Persian_100_CI_AS");

            // ============================================
            // تنظیم طول ستون‌های تاریخ شمسی
            // فرمت: 1403/09/26 12:30:00 = 19 کاراکتر
            // طول انتخابی: 25 (برای اطمینان)
            // ============================================
            const int PersianDateLength = 25;

            // ============================================
            // tblTafsiliType Configuration - انواع مشتری
            // ============================================
            modelBuilder.Entity<tblTafsiliType>(entity =>
            {
                entity.ToTable("tblTafsiliTypes");
                entity.HasKey(e => e.Id);

                // Id به صورت خودکار توسط دیتابیس تولید می‌شود (Identity)
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                // PublicId با مقدار پیش‌فرض
                entity.Property(e => e.PublicId)
                    .HasDefaultValueSql("NEWID()");

                // CodeTafsiliType - دستی تولید می‌شود (نه Identity)
                // یکتا در کل سیستم - در Handler محاسبه می‌شود
                entity.Property(e => e.CodeTafsiliType)
                    .ValueGeneratedNever();

                // تنظیم MaxLength برای فیلدها
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                // تنظیم MaxLength برای تاریخ‌های شمسی
                entity.Property(e => e.ZamanInsert)
                    .HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit)
                    .HasMaxLength(PersianDateLength);

                // IntegrityHash
                entity.Property(e => e.IntegrityHash)
                    .HasMaxLength(500);

                // Index یکتا برای PublicId
                entity.HasIndex(e => e.PublicId)
                    .IsUnique()
                    .HasDatabaseName("IX_tblTafsiliTypes_PublicId");

                // Index یکتا برای CodeTafsiliType در کل سیستم
                entity.HasIndex(e => e.CodeTafsiliType)
                    .IsUnique()
                    .HasDatabaseName("IX_tblTafsiliTypes_CodeTafsiliType");

                // Index برای شعبه
                entity.HasIndex(e => e.tblShobeId)
                    .HasDatabaseName("IX_tblTafsiliTypes_tblShobeId");

                // Index برای Soft Delete و فعال بودن
                entity.HasIndex(e => new { e.IsDeleted, e.IsActive })
                    .HasDatabaseName("IX_tblTafsiliTypes_IsDeleted_IsActive");

                // رابطه سلسله‌مراتبی (Self-Referencing)
                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Query Filter برای Soft Delete
                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // ============================================
            // tblAzaNoe Configuration - انواع حوزه
            // ============================================
            modelBuilder.Entity<tblAzaNoe>(entity =>
            {
                entity.ToTable("tblAzaNoes");
                entity.HasKey(e => e.Id);

                // Id به صورت خودکار توسط دیتابیس تولید می‌شود (Identity)
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                // PublicId با مقدار پیش‌فرض
                entity.Property(e => e.PublicId)
                    .HasDefaultValueSql("NEWID()");

                // تنظیم MaxLength برای فیلدها
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                // تنظیم MaxLength برای تاریخ‌های شمسی
                entity.Property(e => e.ZamanInsert)
                    .HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit)
                    .HasMaxLength(PersianDateLength);

                // IntegrityHash
                entity.Property(e => e.IntegrityHash)
                    .HasMaxLength(500);

                // Index یکتا برای PublicId
                entity.HasIndex(e => e.PublicId)
                    .IsUnique()
                    .HasDatabaseName("IX_tblAzaNoes_PublicId");

                // Index برای شعبه
                entity.HasIndex(e => e.tblShobeId)
                    .HasDatabaseName("IX_tblAzaNoes_tblShobeId");

                // Index برای CodeHoze در هر شعبه
                entity.HasIndex(e => new { e.tblShobeId, e.CodeHoze })
                    .HasDatabaseName("IX_tblAzaNoes_tblShobeId_CodeHoze");

                // Index برای نوع مشتری
                entity.HasIndex(e => e.tblTafsiliTypeId)
                    .HasDatabaseName("IX_tblAzaNoes_tblTafsiliTypeId");

                // Index برای Soft Delete و فعال بودن
                entity.HasIndex(e => new { e.IsDeleted, e.IsActive })
                    .HasDatabaseName("IX_tblAzaNoes_IsDeleted_IsActive");

                // رابطه با tblTafsiliType (Many-to-One)
                // چند حوزه می‌توانند یک نوع مشتری داشته باشند
                entity.HasOne(e => e.TafsiliType)
                    .WithMany(t => t.AzaNoeList)
                    .HasForeignKey(e => e.tblTafsiliTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Query Filter برای Soft Delete
                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // ============================================
            // tblCombo Configuration - جدول دسته‌بندی‌های عمومی
            // ============================================
            modelBuilder.Entity<tblCombo>(entity =>
            {
                entity.ToTable("tblCombos");
                entity.HasKey(e => e.Id);

                // Id به صورت خودکار توسط دیتابیس تولید می‌شود (Identity)
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                // PublicId با مقدار پیش‌فرض
                entity.Property(e => e.PublicId)
                    .HasDefaultValueSql("NEWID()");

                // Index یکتا برای PublicId
                entity.HasIndex(e => e.PublicId)
                    .IsUnique()
                    .HasDatabaseName("IX_tblCombos_PublicId");

                // Index برای GrpCode (برای جستجوی سریع بر اساس گروه)
                entity.HasIndex(e => e.GrpCode)
                    .HasDatabaseName("IX_tblCombos_GrpCode");

                // تنظیم MaxLength برای فیلدها
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                // تنظیم MaxLength برای تاریخ‌های شمسی
                entity.Property(e => e.ZamanInsert)
                    .HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit)
                    .HasMaxLength(PersianDateLength);

                // IntegrityHash
                entity.Property(e => e.IntegrityHash)
                    .HasMaxLength(500);
            });

            // ============================================
            // tblSarfaslType Configuration - نوع سرفصل
            // ============================================
            modelBuilder.Entity<tblSarfaslType>(entity =>
            {
                entity.ToTable("tblSarfaslTypes");
                entity.HasKey(e => e.Id);

                // Id به صورت خودکار توسط دیتابیس تولید می‌شود (Identity)
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                // PublicId با مقدار پیش‌فرض
                entity.Property(e => e.PublicId)
                    .HasDefaultValueSql("NEWID()");

                // Index یکتا برای PublicId
                entity.HasIndex(e => e.PublicId)
                    .IsUnique()
                    .HasDatabaseName("IX_tblSarfaslTypes_PublicId");

                // تنظیم MaxLength برای فیلدها
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                // تنظیم MaxLength برای تاریخ‌های شمسی
                entity.Property(e => e.ZamanInsert)
                    .HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit)
                    .HasMaxLength(PersianDateLength);

                // IntegrityHash
                entity.Property(e => e.IntegrityHash)
                    .HasMaxLength(500);
            });

            // ============================================
            // tblSarfaslProtocol Configuration - پروتکل سرفصل
            // ============================================
            modelBuilder.Entity<tblSarfaslProtocol>(entity =>
            {
                entity.ToTable("tblSarfaslProtocols");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                entity.Property(e => e.PublicId)
                    .HasDefaultValueSql("NEWID()");

                entity.HasIndex(e => e.PublicId)
                    .IsUnique()
                    .HasDatabaseName("IX_tblSarfaslProtocols_PublicId");

                entity.HasIndex(e => e.Code)
                    .IsUnique()
                    .HasDatabaseName("IX_tblSarfaslProtocols_Code");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.DefaultSarfaslJson)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.ZamanInsert)
                    .HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit)
                    .HasMaxLength(PersianDateLength);

                entity.Property(e => e.IntegrityHash)
                    .HasMaxLength(500);
            });

            // ============================================
            // tblSarfasl Configuration - سرفصل حسابداری
            // ============================================
            modelBuilder.Entity<tblSarfasl>(entity =>
            {
                entity.ToTable("tblSarfasls");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                entity.Property(e => e.PublicId)
                    .HasDefaultValueSql("NEWID()");

                // Index یکتا برای PublicId
                entity.HasIndex(e => e.PublicId)
                    .IsUnique()
                    .HasDatabaseName("IX_tblSarfasls_PublicId");

                // Index برای شعبه
                entity.HasIndex(e => e.tblShobeId)
                    .HasDatabaseName("IX_tblSarfasls_tblShobeId");

                // Index برای کد سرفصل
                entity.HasIndex(e => e.CodeSarfasl)
                    .HasDatabaseName("IX_tblSarfasls_CodeSarfasl");

                // Index برای والد
                entity.HasIndex(e => e.ParentId)
                    .HasDatabaseName("IX_tblSarfasls_ParentId");

                // Index برای نوع سرفصل
                entity.HasIndex(e => e.tblSarfaslTypeId)
                    .HasDatabaseName("IX_tblSarfasls_tblSarfaslTypeId");

                // Index برای پروتکل سرفصل
                entity.HasIndex(e => e.tblSarfaslProtocolId)
                    .HasDatabaseName("IX_tblSarfasls_tblSarfaslProtocolId");

                // تنظیم پیش‌فرض برای tblShobeId
                entity.Property(e => e.tblShobeId)
                    .HasDefaultValue(1L);

                entity.Property(e => e.CodeSarfasl)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                // مبالغ بدون اعشار
                entity.Property(e => e.MizanEtebarBedehkar)
                    .HasColumnType("decimal(18,0)")
                    .HasDefaultValue(0m);

                entity.Property(e => e.MizanEtebarBestankar)
                    .HasColumnType("decimal(18,0)")
                    .HasDefaultValue(0m);

                entity.Property(e => e.ZamanInsert)
                    .HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit)
                    .HasMaxLength(PersianDateLength);

                // IntegrityHash - مهم برای این جدول
                entity.Property(e => e.IntegrityHash)
                    .HasMaxLength(500);

                // رابطه سلسله‌مراتبی (Self-Referencing)
                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // رابطه با tblSarfaslType
                entity.HasOne(e => e.SarfaslType)
                    .WithMany()
                    .HasForeignKey(e => e.tblSarfaslTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                // رابطه با tblSarfaslProtocol
                entity.HasOne(e => e.SarfaslProtocol)
                    .WithMany()
                    .HasForeignKey(e => e.tblSarfaslProtocolId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
