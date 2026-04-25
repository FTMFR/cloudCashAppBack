using BnpCashClaudeApp.Domain.Entities.AuditLogSubsystem;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <summary>
    /// DbContext جداگانه برای دیتابیس Audit Log
    /// </summary>
    public class LogDbContext : DbContext
    {
        public LogDbContext(DbContextOptions<LogDbContext> options) : base(options) { }

        public DbSet<AuditLogMaster> AuditLogMasters { get; set; }
        public DbSet<AuditLogDetail> AuditLogDetails { get; set; }
        public DbSet<tblAttachmentAccessLog> tblAttachmentAccessLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================
            // تنظیم Collation فارسی برای پشتیبانی صحیح از حروف ی و ک
            // Persian_100_CI_AS: Case Insensitive, Accent Sensitive
            // ============================================
            modelBuilder.UseCollation("Persian_100_CI_AS");

            // AuditLogMaster Configuration
            modelBuilder.Entity<AuditLogMaster>(entity =>
            {
                entity.ToTable("AuditLogMaster");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EntityType).HasMaxLength(200);
                entity.Property(e => e.EntityId).HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.UserName).HasMaxLength(100);
                entity.Property(e => e.OperatingSystem).HasMaxLength(200);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
                entity.Property(e => e.Description).HasMaxLength(2000);

                // ============================================
                // تنظیمات ستون‌های تاریخ شمسی (از BaseEntity)
                // ============================================
                entity.Property(e => e.ZamanInsert).HasMaxLength(25);
                entity.Property(e => e.ZamanLastEdit).HasMaxLength(25);

                // Index برای جستجوی سریع‌تر
                entity.HasIndex(e => e.EventDateTime);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => e.EntityType);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
            });

            // AuditLogDetail Configuration
            modelBuilder.Entity<AuditLogDetail>(entity =>
            {
                entity.ToTable("AuditLogDetail");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FieldName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.OldValue).HasColumnType("nvarchar(max)");
                entity.Property(e => e.NewValue).HasColumnType("nvarchar(max)");
                entity.Property(e => e.DataType).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(2000);

                // ============================================
                // تنظیمات ستون‌های تاریخ شمسی (از BaseEntity)
                // ============================================
                entity.Property(e => e.ZamanInsert).HasMaxLength(25);
                entity.Property(e => e.ZamanLastEdit).HasMaxLength(25);

                // Foreign Key به AuditLogMaster
                entity.HasOne(d => d.AuditLogMaster)
                    .WithMany(m => m.Details)
                    .HasForeignKey(d => d.AuditLogMasterId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Index
                entity.HasIndex(e => e.AuditLogMasterId);
                entity.HasIndex(e => e.FieldName);
            });

            // ============================================
            // tblAttachmentAccessLog Configuration
            // پیاده‌سازی الزامات ISO 15408:
            // - FAU_GEN.1: تولید داده ممیزی
            // - FAU_GEN.2: مرتبط نمودن هویت کاربر
            // - FTA_TAH.1: سوابق دسترسی به محصول
            // ============================================
            modelBuilder.Entity<tblAttachmentAccessLog>(entity =>
            {
                entity.ToTable("tblAttachmentAccessLog");
                entity.HasKey(e => e.Id);

                // ============================================
                // اطلاعات فایل
                // ============================================
                entity.Property(e => e.FileName)
                    .HasMaxLength(255);

                entity.Property(e => e.FileType)
                    .HasMaxLength(100);

                // ============================================
                // اطلاعات کاربر
                // ============================================
                entity.Property(e => e.UserName)
                    .HasMaxLength(100);

                entity.Property(e => e.UserGroupName)
                    .HasMaxLength(100);

                // ============================================
                // اطلاعات دستگاه و شبکه
                // ============================================
                entity.Property(e => e.IpAddress)
                    .HasMaxLength(45);

                entity.Property(e => e.UserAgent)
                    .HasMaxLength(500);

                entity.Property(e => e.Browser)
                    .HasMaxLength(100);

                entity.Property(e => e.BrowserVersion)
                    .HasMaxLength(50);

                entity.Property(e => e.OperatingSystem)
                    .HasMaxLength(100);

                entity.Property(e => e.DeviceType)
                    .HasMaxLength(50);

                // ============================================
                // توضیحات و خطاها
                // ============================================
                entity.Property(e => e.AccessDescription)
                    .HasMaxLength(500);

                entity.Property(e => e.ErrorMessage)
                    .HasMaxLength(2000);

                entity.Property(e => e.AccessDeniedReason)
                    .HasMaxLength(500);

                // ============================================
                // مشخصات امنیتی
                // ============================================
                entity.Property(e => e.FileSecurityClassification)
                    .HasMaxLength(100);

                // ============================================
                // اطلاعات تکمیلی
                // ============================================
                entity.Property(e => e.RequestId)
                    .HasMaxLength(50);

                entity.Property(e => e.SessionId)
                    .HasMaxLength(100);

                entity.Property(e => e.AdditionalInfo)
                    .HasColumnType("nvarchar(max)");

                // ============================================
                // تاریخ‌های شمسی (از BaseEntity)
                // ============================================
                entity.Property(e => e.ZamanInsert)
                    .HasMaxLength(25);

                entity.Property(e => e.ZamanLastEdit)
                    .HasMaxLength(25);

                entity.Property(e => e.AccessDateTimePersian)
                    .HasMaxLength(25);

                entity.Property(e => e.IntegrityHash)
                    .HasMaxLength(128);

                // ============================================
                // Index ها برای جستجوی سریع
                // ============================================
                entity.HasIndex(e => e.PublicId)
                    .IsUnique();

                entity.HasIndex(e => e.AttachmentId);

                entity.HasIndex(e => e.AttachmentPublicId);

                entity.HasIndex(e => e.AccessDateTime);

                entity.HasIndex(e => e.AccessType);

                entity.HasIndex(e => e.UserId);

                entity.HasIndex(e => e.UserName);

                entity.HasIndex(e => e.IpAddress);

                entity.HasIndex(e => e.IsSuccess);

                entity.HasIndex(e => e.tblCustomerId);

                entity.HasIndex(e => e.tblShobeId);

                entity.HasIndex(e => new { e.AttachmentId, e.AccessDateTime });

                entity.HasIndex(e => new { e.UserId, e.AccessDateTime });
            });
        }
    }
}

