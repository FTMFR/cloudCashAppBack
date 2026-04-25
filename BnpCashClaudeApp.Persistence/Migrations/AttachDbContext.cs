using BnpCashClaudeApp.Domain.Entities.AttachSubsystem;
using Microsoft.EntityFrameworkCore;
using System;

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <summary>
    /// DbContext برای دیتابیس فایل‌ها و تصاویر
    /// دیتابیس: BnpAttachCloudDB
    /// ============================================
    /// این Context برای مدیریت جداول مربوط به:
    /// - فایل‌های پیوست
    /// - تصاویر
    /// - اسناد
    /// - امضاهای دیجیتال
    /// و سایر فایل‌های ضمیمه استفاده می‌شود
    /// ============================================
    /// </summary>
    public class AttachDbContext : DbContext
    {
        public AttachDbContext(DbContextOptions<AttachDbContext> options) : base(options) { }

        // ============================================
        // جداول فایل‌ها و پیوست‌ها
        // ============================================
        public DbSet<tblAttachment> tblAttachments { get; set; }

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
            // tblAttachment Configuration
            // پیاده‌سازی الزامات ISO 15408:
            // - FDP_ITC.2: ورود داده با مشخصه امنیتی
            // - FDP_ETC.2: خروج داده با مشخصه امنیتی
            // - FDP_SDI.2: صحت داده ذخیره شده
            // ============================================
            modelBuilder.Entity<tblAttachment>(entity =>
            {
                entity.ToTable("tblAttachment");
                entity.HasKey(e => e.Id);

                // ============================================
                // اطلاعات فایل
                // ============================================
                entity.Property(e => e.OriginalFileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.StoredFileName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.FileExtension)
                    .HasMaxLength(20);

                entity.Property(e => e.ContentType)
                    .HasMaxLength(100);

                entity.Property(e => e.StoragePath)
                    .IsRequired()
                    .HasMaxLength(500);

                // ============================================
                // طبقه‌بندی و دسته‌بندی
                // ============================================
                entity.Property(e => e.Category)
                    .HasMaxLength(100);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                // ============================================
                // ارتباط با موجودیت‌ها (Polymorphic)
                // ============================================
                entity.Property(e => e.EntityType)
                    .HasMaxLength(100);

                // ============================================
                // مشخصات امنیتی (FDP_ITC.2 / FDP_ETC.2)
                // ============================================
                entity.Property(e => e.SecurityClassification)
                    .HasMaxLength(100);

                entity.Property(e => e.SecurityLabels)
                    .HasMaxLength(500);

                entity.Property(e => e.EncryptionAlgorithm)
                    .HasMaxLength(50);

                entity.Property(e => e.EncryptionIV)
                    .HasMaxLength(100);

                // ============================================
                // صحت داده (FDP_SDI.2)
                // ============================================
                entity.Property(e => e.ContentHash)
                    .HasMaxLength(64);

                entity.Property(e => e.HashAlgorithm)
                    .HasMaxLength(20);

                entity.Property(e => e.DigitalSignature)
                    .HasMaxLength(500);

                // ============================================
                // تاریخ‌های شمسی
                // ============================================
                entity.Property(e => e.ZamanInsert)
                    .HasMaxLength(PersianDateLength);

                entity.Property(e => e.ZamanLastEdit)
                    .HasMaxLength(PersianDateLength);

                entity.Property(e => e.LastIntegrityCheckAt)
                    .HasMaxLength(PersianDateLength);

                entity.Property(e => e.VirusScannedAt)
                    .HasMaxLength(PersianDateLength);

                entity.Property(e => e.LastAccessedAt)
                    .HasMaxLength(PersianDateLength);

                entity.Property(e => e.ExpiresAt)
                    .HasMaxLength(PersianDateLength);

                entity.Property(e => e.AutoDeleteAt)
                    .HasMaxLength(PersianDateLength);

                // ============================================
                // سایر فیلدها
                // ============================================
                entity.Property(e => e.LastAccessedFromIp)
                    .HasMaxLength(45);

                entity.Property(e => e.IntegrityHash)
                    .HasMaxLength(128);

                // ============================================
                // Index ها برای جستجوی سریع
                // ============================================
                entity.HasIndex(e => e.PublicId)
                    .IsUnique();

                entity.HasIndex(e => e.StoredFileName);

                entity.HasIndex(e => e.ContentHash);

                entity.HasIndex(e => new { e.EntityType, e.EntityId });

                entity.HasIndex(e => e.EntityPublicId);

                entity.HasIndex(e => e.Status);

                entity.HasIndex(e => e.AttachmentType);

                entity.HasIndex(e => e.Category);

                entity.HasIndex(e => e.tblCustomerId);

                entity.HasIndex(e => e.tblShobeId);

                entity.HasIndex(e => e.SensitivityLevel);

                entity.HasIndex(e => e.ZamanInsert);
            });
        }
    }
}
