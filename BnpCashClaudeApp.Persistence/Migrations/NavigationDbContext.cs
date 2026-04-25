using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.ManagementSubsystem;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Entities.SecuritySubsystem;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Persistence.Migrations
{
    /// <summary>
    /// DbContext اصلی برنامه برای کاربران و منوها
    /// </summary>
    public class NavigationDbContext : DbContext
    {
        private readonly IServiceProvider? _serviceProvider;

        public NavigationDbContext(DbContextOptions<NavigationDbContext> options) : base(options) { }

        public NavigationDbContext(
            DbContextOptions<NavigationDbContext> options,
            IServiceProvider serviceProvider) : base(options)
        {
            _serviceProvider = serviceProvider;
        }

        // ============================================
        // Navigation Entities
        // ============================================
        public DbSet<tblMenu> tblMenus { get; set; }
        public DbSet<tblGrp> tblGrps { get; set; }
        public DbSet<tblUser> tblUsers { get; set; }
        public DbSet<tblUserGrp> tblUserGrps { get; set; }
        public DbSet<tblShobe> tblShobes { get; set; }
        public DbSet<tblShobeSetting> tblShobeSettings { get; set; }

        // ============================================
        // Permission Entities (FDP_ACF)
        // ============================================
        public DbSet<tblPermission> tblPermissions { get; set; }
        public DbSet<tblGrpPermission> tblGrpPermissions { get; set; }
        public DbSet<tblMenuPermission> tblMenuPermissions { get; set; }

        // ============================================
        // Security Entities
        // ============================================
        public DbSet<PasswordHistory> PasswordHistories { get; set; }
        public DbSet<SecuritySetting> SecuritySettings { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        // ============================================
        // Cryptographic Key Entities (FCS_CKM)
        // ============================================
        public DbSet<CryptographicKeyEntity> CryptographicKeys { get; set; }

        // ============================================
        // Management Entities (راهبری سیستم)
        // ============================================
        public DbSet<tblSoftware> tblSoftwares { get; set; }
        public DbSet<tblPlan> tblPlans { get; set; }
        public DbSet<tblCustomer> tblCustomers { get; set; }
        public DbSet<tblCustomerContact> tblCustomerContacts { get; set; }
        public DbSet<tblCustomerSoftware> tblCustomerSoftwares { get; set; }
        public DbSet<tblDb> tblDbs { get; set; }
        public DbSet<tblCustomerSoftwareDb> tblCustomerSoftwareDbs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================
            // تنظیم Collation فارسی برای پشتیبانی صحیح از حروف ی و ک
            // Persian_100_CI_AS: Case Insensitive, Accent Sensitive
            // این تنظیم باعث می‌شود:
            // - ی فارسی (U+06CC) و ي عربی (U+064A) به درستی مدیریت شوند
            // - ک فارسی (U+06A9) و ك عربی (U+0643) به درستی مدیریت شوند
            // - مرتب‌سازی بر اساس قواعد زبان فارسی انجام شود
            // ============================================
            modelBuilder.UseCollation("Persian_100_CI_AS");

            // ============================================
            // تنظیم طول ستون‌های تاریخ شمسی
            // فرمت: 1403/09/26 12:30:00 = 19 کاراکتر
            // طول انتخابی: 25 (برای اطمینان)
            // ============================================
            const int PersianDateLength = 25;

            // ============================================
            // تنظیمات مشترک برای همه Entity ها (BaseEntity)
            // Id: bigint (long) - Identity
            // PublicId: Guid - Unique Index
            // ============================================
            
            // تنظیم PublicId برای همه جداول
            modelBuilder.Entity<tblMenu>()
                .HasIndex(e => e.PublicId)
                .IsUnique()
                .HasDatabaseName("IX_tblMenus_PublicId");
            modelBuilder.Entity<tblMenu>()
                .Property(e => e.PublicId)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<tblGrp>()
                .HasIndex(e => e.PublicId)
                .IsUnique()
                .HasDatabaseName("IX_tblGrps_PublicId");
            modelBuilder.Entity<tblGrp>()
                .Property(e => e.PublicId)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<tblUser>()
                .HasIndex(e => e.PublicId)
                .IsUnique()
                .HasDatabaseName("IX_tblUsers_PublicId");
            modelBuilder.Entity<tblUser>()
                .Property(e => e.PublicId)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<tblPermission>()
                .HasIndex(e => e.PublicId)
                .IsUnique()
                .HasDatabaseName("IX_tblPermissions_PublicId");
            modelBuilder.Entity<tblPermission>()
                .Property(e => e.PublicId)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<SecuritySetting>()
                .HasIndex(e => e.PublicId)
                .IsUnique()
                .HasDatabaseName("IX_SecuritySettings_PublicId");
            modelBuilder.Entity<SecuritySetting>()
                .Property(e => e.PublicId)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(e => e.PublicId)
                .IsUnique()
                .HasDatabaseName("IX_RefreshTokens_PublicId");
            modelBuilder.Entity<RefreshToken>()
                .Property(e => e.PublicId)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<PasswordHistory>()
                .HasIndex(e => e.PublicId)
                .IsUnique()
                .HasDatabaseName("IX_PasswordHistory_PublicId");
            modelBuilder.Entity<PasswordHistory>()
                .Property(e => e.PublicId)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<tblShobe>()
                .HasIndex(e => e.PublicId)
                .IsUnique()
                .HasDatabaseName("IX_tblShobes_PublicId");
            modelBuilder.Entity<tblShobe>()
                .Property(e => e.PublicId)
                .HasDefaultValueSql("NEWID()");

            // ============================================
            // روابط tblUser با tblCustomer و tblShobe
            // ============================================
            modelBuilder.Entity<tblUser>(entity =>
            {
                // رابطه با مشتری
                entity.HasOne(u => u.Customer)
                    .WithMany(c => c.Users)
                    .HasForeignKey(u => u.tblCustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // رابطه با شعبه
                entity.HasOne(u => u.Shobe)
                    .WithMany(s => s.Users)
                    .HasForeignKey(u => u.tblShobeId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Index برای جستجوی سریع کاربران هر مشتری
                entity.HasIndex(u => u.tblCustomerId)
                    .HasDatabaseName("IX_tblUsers_tblCustomerId");

                // Index برای جستجوی سریع کاربران هر شعبه
                entity.HasIndex(u => u.tblShobeId)
                    .HasDatabaseName("IX_tblUsers_tblShobeId");

                // UserName یکتا در سطح جدول
                entity.HasIndex(u => u.UserName)
                    .IsUnique()
                    .HasDatabaseName("IX_tblUsers_UserName");
            });

            // ============================================
            // روابط tblShobe با tblCustomer
            // ============================================
            modelBuilder.Entity<tblShobe>(entity =>
            {
                // رابطه با مشتری
                entity.HasOne(s => s.Customer)
                    .WithMany(c => c.Shobes)
                    .HasForeignKey(s => s.tblCustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Index برای جستجوی سریع شعب هر مشتری
                entity.HasIndex(s => s.tblCustomerId)
                    .HasDatabaseName("IX_tblShobes_tblCustomerId");
            });

            // ============================================
            // تنظیمات tblMenuPermission
            // ارتباط بین منو و Permission
            // ============================================
            modelBuilder.Entity<tblMenuPermission>(entity =>
            {
                entity.ToTable("tblMenuPermissions");
                
                // کلید اصلی - Id با Identity
                entity.HasKey(mp => mp.Id);
                
                // Id به صورت خودکار توسط دیتابیس تولید می‌شود (Identity)
                entity.Property(mp => mp.Id)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
                
                // PublicId با مقدار پیش‌فرض
                entity.Property(mp => mp.PublicId)
                    .HasDefaultValueSql("NEWID()");

                entity.Property(mp => mp.Notes).HasMaxLength(500);
                entity.Property(mp => mp.ZamanInsert).HasMaxLength(PersianDateLength);
                entity.Property(mp => mp.ZamanLastEdit).HasMaxLength(PersianDateLength);

                // Index یکتا برای ترکیب MenuId و PermissionId
                entity.HasIndex(mp => new { mp.tblMenuId, mp.tblPermissionId })
                    .IsUnique()
                    .HasDatabaseName("IX_tblMenuPermissions_tblMenuId_tblPermissionId");

                // روابط
                entity.HasOne(mp => mp.tblMenu)
                    .WithMany(m => m.MenuPermissions)
                    .HasForeignKey(mp => mp.tblMenuId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mp => mp.tblPermission)
                    .WithMany(p => p.MenuPermissions)
                    .HasForeignKey(mp => mp.tblPermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================
            // tblUserGrp Configuration
            // ============================================
            modelBuilder.Entity<tblUserGrp>(entity =>
            {
                entity.HasKey(ug => ug.Id);
                
                // Id به صورت خودکار توسط دیتابیس تولید می‌شود (Identity)
                entity.Property(ug => ug.Id)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
                
                // PublicId با مقدار پیش‌فرض
                entity.Property(ug => ug.PublicId)
                    .HasDefaultValueSql("NEWID()");

                entity.HasOne(ug => ug.tblUser)
                    .WithMany()
                    .HasForeignKey(ug => ug.tblUserId);

                entity.HasOne(ug => ug.tblGrp)
                    .WithMany(g => g.UserGroups)
                    .HasForeignKey(ug => ug.tblGrpId);

                entity.HasIndex(ug => new { ug.tblUserId, ug.tblGrpId })
                    .IsUnique();

                entity.Property(ug => ug.ZamanInsert)
                    .HasMaxLength(PersianDateLength);
                entity.Property(ug => ug.ZamanLastEdit)
                    .HasMaxLength(PersianDateLength);
                entity.Property(ug => ug.AssignmentDate)
                    .HasMaxLength(PersianDateLength);
            });

            // ============================================
            // tblMenu Self-Referencing
            // ============================================
            modelBuilder.Entity<tblMenu>()
                .HasOne(m => m.Parent)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // tblMenu -> tblSoftware Relationship
            modelBuilder.Entity<tblMenu>()
                .HasOne(m => m.Software)
                .WithMany(s => s.Menus)
                .HasForeignKey(m => m.tblSoftwareId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<tblMenu>()
                .HasIndex(m => m.tblSoftwareId)
                .HasDatabaseName("IX_tblMenus_tblSoftwareId");

            modelBuilder.Entity<tblMenu>()
                .Property(m => m.ZamanInsert)
                .HasMaxLength(PersianDateLength);
            modelBuilder.Entity<tblMenu>()
                .Property(m => m.ZamanLastEdit)
                .HasMaxLength(PersianDateLength);

            // ============================================
            // tblUser Configuration
            // ============================================
            modelBuilder.Entity<tblUser>()
                .Property(u => u.ZamanInsert)
                .HasMaxLength(PersianDateLength);
            modelBuilder.Entity<tblUser>()
                .Property(u => u.ZamanLastEdit)
                .HasMaxLength(PersianDateLength);
            modelBuilder.Entity<tblUser>()
                .Property(u => u.PasswordLastChangedAt)
                .HasMaxLength(PersianDateLength);
            modelBuilder.Entity<tblUser>()
                .Property(u => u.LastLoginAt)
                .HasMaxLength(PersianDateLength);

            // ============================================
            // tblGrp Configuration
            // ============================================
            modelBuilder.Entity<tblGrp>()
                .Property(g => g.ZamanInsert)
                .HasMaxLength(PersianDateLength);
            modelBuilder.Entity<tblGrp>()
                .Property(g => g.ZamanLastEdit)
                .HasMaxLength(PersianDateLength);

            // رابطه tblGrp با tblShobe
            // اگر tblShobeId نال باشد، گروه برای همه شعبات است
            modelBuilder.Entity<tblGrp>(entity =>
            {
                entity.HasOne(g => g.Shobe)
                    .WithMany(s => s.Groups)
                    .HasForeignKey(g => g.tblShobeId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Index برای جستجوی سریع گروه‌های هر شعبه
                entity.HasIndex(g => g.tblShobeId)
                    .HasDatabaseName("IX_tblGrps_tblShobeId");
            });

            // ============================================
            // PasswordHistory Configuration
            // الزام FDP (User Data Protection) از ISO 15408
            // ============================================
            modelBuilder.Entity<PasswordHistory>(entity =>
            {
                entity.ToTable("PasswordHistory");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
                entity.Property(e => e.SetAt).HasMaxLength(PersianDateLength);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.ZamanInsert).HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit).HasMaxLength(PersianDateLength);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.PasswordHash });
            });

            // ============================================
            // SecuritySetting Configuration
            // ============================================
            modelBuilder.Entity<SecuritySetting>(entity =>
            {
                entity.ToTable("SecuritySettings");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SettingKey).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SettingName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.SettingValue).HasColumnType("nvarchar(max)");
                entity.Property(e => e.ZamanInsert).HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit).HasMaxLength(PersianDateLength);

                entity.HasIndex(e => e.SettingKey).IsUnique();
            });

            // ============================================
            // RefreshToken Configuration
            // ============================================
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.OperatingSystem).HasMaxLength(100);
                entity.Property(e => e.RevokedReason).HasMaxLength(200);
                entity.Property(e => e.ReplacedByToken).HasMaxLength(500);
                entity.Property(e => e.ZamanInsert).HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit).HasMaxLength(PersianDateLength);

                // Indexes برای بهبود Performance
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => new { e.UserId, e.IsRevoked, e.IsUsed });
            });

            // ============================================
            // tblPermission Configuration
            // پیاده‌سازی الزام FDP_ACF از ISO 15408
            // ============================================
            modelBuilder.Entity<tblPermission>(entity =>
            {
                entity.ToTable("tblPermissions");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Resource).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ZamanInsert).HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit).HasMaxLength(PersianDateLength);

                // Index یکتا روی نام Permission
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => new { e.Resource, e.Action });
            });

            // ============================================
            // CryptographicKeyEntity Configuration (FCS_CKM)
            // مدیریت کلیدهای رمزنگاری
            // ============================================
            modelBuilder.Entity<CryptographicKeyEntity>(entity =>
            {
                entity.ToTable("CryptographicKeys");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                entity.Property(e => e.PublicId)
                    .HasDefaultValueSql("NEWID()");

                entity.Property(e => e.KeyId)
                    .IsRequired();

                entity.Property(e => e.Purpose)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.EncryptedKeyValue)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(e => e.EncryptionIV)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.EncryptionMAC)
                    .HasMaxLength(100);

                entity.Property(e => e.Algorithm)
                    .HasMaxLength(50);

                entity.Property(e => e.KeyHash)
                    .HasMaxLength(100);

                entity.Property(e => e.DeactivationReason)
                    .HasMaxLength(500);

                entity.Property(e => e.DestructionReason)
                    .HasMaxLength(500);

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.ZamanInsert)
                    .HasMaxLength(PersianDateLength);

                entity.Property(e => e.ZamanLastEdit)
                    .HasMaxLength(PersianDateLength);

                // Indexes
                entity.HasIndex(e => e.KeyId)
                    .IsUnique()
                    .HasDatabaseName("IX_CryptographicKeys_KeyId");

                entity.HasIndex(e => e.Purpose)
                    .HasDatabaseName("IX_CryptographicKeys_Purpose");

                entity.HasIndex(e => new { e.Purpose, e.Status })
                    .HasDatabaseName("IX_CryptographicKeys_Purpose_Status");

                entity.HasIndex(e => e.PublicId)
                    .IsUnique()
                    .HasDatabaseName("IX_CryptographicKeys_PublicId");
            });

            // ============================================
            // tblGrpPermission Configuration
            // ارتباط بین گروه و Permission
            // ============================================
            modelBuilder.Entity<tblGrpPermission>(entity =>
            {
                entity.ToTable("tblGrpPermissions");
                
                // کلید اصلی - Id با Identity
                entity.HasKey(e => e.Id);
                
                // Id به صورت خودکار توسط دیتابیس تولید می‌شود (Identity)
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
                
                // PublicId با مقدار پیش‌فرض
                entity.Property(e => e.PublicId)
                    .HasDefaultValueSql("NEWID()");

                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.ZamanInsert).HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit).HasMaxLength(PersianDateLength);

                // Index یکتا برای ترکیب GrpId و PermissionId
                entity.HasIndex(e => new { e.tblGrpId, e.tblPermissionId })
                    .IsUnique()
                    .HasDatabaseName("IX_tblGrpPermissions_tblGrpId_tblPermissionId");

                // روابط
                entity.HasOne(e => e.tblGrp)
                    .WithMany(g => g.GroupPermissions)
                    .HasForeignKey(e => e.tblGrpId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.tblPermission)
                    .WithMany(p => p.GroupPermissions)
                    .HasForeignKey(e => e.tblPermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================
            // بروزرسانی تنظیمات tblGrp برای روابط جدید
            // ============================================
            modelBuilder.Entity<tblGrp>()
                .Property(g => g.Description)
                .HasMaxLength(500);

            // ============================================
            // تنظیم IntegrityHash برای تمام Entityها
            // پیاده‌سازی الزام FDP_SDI.2.1 از ISO 15408
            // ============================================
            modelBuilder.Entity<tblUser>()
                .Property(e => e.IntegrityHash)
                .HasMaxLength(500);

            modelBuilder.Entity<SecuritySetting>()
                .Property(e => e.IntegrityHash)
                .HasMaxLength(500);

            modelBuilder.Entity<CryptographicKeyEntity>()
                .Property(e => e.IntegrityHash)
                .HasMaxLength(500);

            modelBuilder.Entity<tblPermission>()
                .Property(e => e.IntegrityHash)
                .HasMaxLength(500);

            modelBuilder.Entity<PasswordHistory>()
                .Property(e => e.IntegrityHash)
                .HasMaxLength(500);

            // ============================================
            // tblShobe Configuration
            // ============================================
            modelBuilder.Entity<tblShobe>(entity =>
            {
                entity.ToTable("tblShobes");
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
                    .HasDatabaseName("IX_tblShobes_PublicId");

                // Index یکتا برای ShobeCode
                entity.HasIndex(e => e.ShobeCode)
                    .IsUnique()
                    .HasDatabaseName("IX_tblShobes_ShobeCode");

                // تنظیم MaxLength برای فیلدها
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Address)
                    .HasMaxLength(500);

                entity.Property(e => e.Phone)
                    .HasMaxLength(50);

                entity.Property(e => e.PostalCode)
                    .HasMaxLength(20);

                entity.Property(e => e.Description)
                    .HasMaxLength(1000);

                // تنظیم MaxLength برای تاریخ‌های شمسی
                entity.Property(e => e.ZamanInsert)
                    .HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit)
                    .HasMaxLength(PersianDateLength);

                // تنظیم IntegrityHash
                entity.Property(e => e.IntegrityHash)
                    .HasMaxLength(500);

                // رابطه سلسله‌مراتبی (Self-Referencing)
                entity.HasOne(s => s.Parent)
                    .WithMany(s => s.Children)
                    .HasForeignKey(s => s.ParentId)
                    .OnDelete(DeleteBehavior.Restrict); // جلوگیری از حذف در صورت وجود زیرشعب

                // Index برای بهبود Performance
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.ParentId);
                entity.HasIndex(e => new { e.IsActive, e.DisplayOrder });
            });

            // ============================================
            // tblShobeSetting Configuration
            // ============================================
            modelBuilder.Entity<tblShobeSetting>(entity =>
            {
                entity.ToTable("tblShobeSettings");
                entity.HasKey(e => e.Id);

                // Id به صورت خودکار توسط دیتابیس تولید می‌شود (Identity)
                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                // PublicId با مقدار پیش‌فرض
                entity.Property(e => e.PublicId)
                    .HasDefaultValueSql("NEWID()");

                entity.Property(e => e.SettingKey).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SettingName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.SettingValue).HasColumnType("nvarchar(max)");
                entity.Property(e => e.ZamanInsert).HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit).HasMaxLength(PersianDateLength);

                // Index یکتا برای SettingKey و TblShobeId (ترکیبی)
                // این اجازه می‌دهد که هر شعبه تنظیمات یکتای خود را داشته باشد
                entity.HasIndex(e => new { e.SettingKey, e.TblShobeId })
                    .IsUnique()
                    .HasDatabaseName("IX_tblShobeSettings_SettingKey_TblShobeId");

                // Index برای TblShobeId برای جستجوی سریع
                entity.HasIndex(e => e.TblShobeId)
                    .HasDatabaseName("IX_tblShobeSettings_TblShobeId");

                // رابطه با tblShobe
                entity.HasOne(s => s.TblShobe)
                    .WithMany()
                    .HasForeignKey(s => s.TblShobeId)
                    .OnDelete(DeleteBehavior.Cascade); // در صورت حذف شعبه، تنظیمات آن هم حذف می‌شود
            });

            modelBuilder.Entity<tblGrp>()
                .HasOne(s => s.Parent)
                    .WithMany(s => s.Children)
                    .HasForeignKey(s => s.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<tblGrp>()
                .HasIndex(e => e.ParentId);

            // ============================================
            // Management Entities Configuration (راهبری سیستم)
            // ============================================

            // tblSoftware Configuration
            modelBuilder.Entity<tblSoftware>(entity =>
            {
                entity.ToTable("tblSoftwares");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityColumn();

                entity.HasIndex(e => e.PublicId).IsUnique();
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.PublicId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CurrentVersion).HasMaxLength(20);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Icon).HasMaxLength(200);
                entity.Property(e => e.WebsiteUrl).HasMaxLength(500);
                entity.Property(e => e.DownloadUrl).HasMaxLength(500);
                entity.Property(e => e.ZamanInsert).HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit).HasMaxLength(PersianDateLength);
            });

            // tblPlan Configuration
            modelBuilder.Entity<tblPlan>(entity =>
            {
                entity.ToTable("tblPlans");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityColumn();

                entity.HasIndex(e => e.PublicId).IsUnique();
                entity.HasIndex(e => new { e.tblSoftwareId, e.Code }).IsUnique();
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.PublicId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.FeaturesJson).HasMaxLength(2000);
                entity.Property(e => e.BasePrice).HasColumnType("decimal(18,0)");
                entity.Property(e => e.MonthlyPrice).HasColumnType("decimal(18,0)");
                entity.Property(e => e.YearlyPrice).HasColumnType("decimal(18,0)");
                entity.Property(e => e.ZamanInsert).HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit).HasMaxLength(PersianDateLength);

                entity.HasOne(e => e.Software)
                    .WithMany(s => s.Plans)
                    .HasForeignKey(e => e.tblSoftwareId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // tblCustomer Configuration
            modelBuilder.Entity<tblCustomer>(entity =>
            {
                entity.ToTable("tblCustomers");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityColumn();

                entity.HasIndex(e => e.PublicId).IsUnique();
                entity.HasIndex(e => e.CustomerCode).IsUnique();
                entity.HasIndex(e => e.NationalId);
                entity.HasIndex(e => e.CompanyNationalId);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.PublicId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(300);
                entity.Property(e => e.CustomerCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.NationalId).HasMaxLength(10);
                entity.Property(e => e.RegistrationNumber).HasMaxLength(20);
                entity.Property(e => e.CompanyNationalId).HasMaxLength(11);
                entity.Property(e => e.EconomicCode).HasMaxLength(20);
                entity.Property(e => e.ManagerName).HasMaxLength(200);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Mobile).HasMaxLength(15);
                entity.Property(e => e.Fax).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.Website).HasMaxLength(300);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.PostalCode).HasMaxLength(10);
                entity.Property(e => e.Province).HasMaxLength(100);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.LogoPath).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.MembershipDate).HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanInsert).HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit).HasMaxLength(PersianDateLength);
            });

            // tblCustomerContact Configuration
            modelBuilder.Entity<tblCustomerContact>(entity =>
            {
                entity.ToTable("tblCustomerContacts");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityColumn();

                entity.HasIndex(e => e.PublicId).IsUnique();
                entity.HasIndex(e => e.tblCustomerId);
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.PublicId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.JobTitle).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Mobile).HasMaxLength(15);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.Messenger).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ZamanInsert).HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit).HasMaxLength(PersianDateLength);

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Contacts)
                    .HasForeignKey(e => e.tblCustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // tblCustomerSoftware Configuration
            modelBuilder.Entity<tblCustomerSoftware>(entity =>
            {
                entity.ToTable("tblCustomerSoftwares");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityColumn();

                entity.HasIndex(e => e.PublicId).IsUnique();
                entity.HasIndex(e => e.LicenseKey).IsUnique();
                entity.HasIndex(e => new { e.tblCustomerId, e.tblSoftwareId });
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.PublicId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.LicenseKey).IsRequired().HasMaxLength(100);
                entity.Property(e => e.StartDate).HasMaxLength(PersianDateLength);
                entity.Property(e => e.EndDate).HasMaxLength(PersianDateLength);
                entity.Property(e => e.InstalledVersion).HasMaxLength(20);
                entity.Property(e => e.LastActivationDate).HasMaxLength(PersianDateLength);
                entity.Property(e => e.LastActivationIp).HasMaxLength(50);
                entity.Property(e => e.CustomSettingsJson).HasMaxLength(4000);
                entity.Property(e => e.Notes).HasMaxLength(1000);
                entity.Property(e => e.PaidAmount).HasColumnType("decimal(18,0)");
                entity.Property(e => e.ZamanInsert).HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit).HasMaxLength(PersianDateLength);

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.CustomerSoftwares)
                    .HasForeignKey(e => e.tblCustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Software)
                    .WithMany(s => s.CustomerSoftwares)
                    .HasForeignKey(e => e.tblSoftwareId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Plan)
                    .WithMany(p => p.CustomerSoftwares)
                    .HasForeignKey(e => e.tblPlanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // tblDb Configuration
            modelBuilder.Entity<tblDb>(entity =>
            {
                entity.ToTable("tblDbs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityColumn();

                entity.HasIndex(e => e.PublicId).IsUnique();
                entity.HasIndex(e => e.DbCode).IsUnique();
                entity.HasIndex(e => e.tblCustomerId);
                entity.HasIndex(e => e.tblSoftwareId);
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.PublicId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.DbCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ServerName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.DatabaseName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Username).HasMaxLength(100);
                entity.Property(e => e.EncryptedPassword).HasMaxLength(500);
                entity.Property(e => e.EncryptedConnectionString).HasMaxLength(2000);
                entity.Property(e => e.TenantId).HasMaxLength(50);
                entity.Property(e => e.LastBackupDate).HasMaxLength(PersianDateLength);
                entity.Property(e => e.LastConnectionTestDate).HasMaxLength(PersianDateLength);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ZamanInsert).HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit).HasMaxLength(PersianDateLength);

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Databases)
                    .HasForeignKey(e => e.tblCustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Software)
                    .WithMany(s => s.Databases)
                    .HasForeignKey(e => e.tblSoftwareId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // tblCustomerSoftwareDb Configuration
            modelBuilder.Entity<tblCustomerSoftwareDb>(entity =>
            {
                entity.ToTable("tblCustomerSoftwareDbs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityColumn();

                entity.HasIndex(e => e.PublicId).IsUnique();
                entity.HasIndex(e => new { e.tblCustomerSoftwareId, e.tblDbId }).IsUnique();
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.PublicId).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.ConnectedDate).HasMaxLength(PersianDateLength);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.ZamanInsert).HasMaxLength(PersianDateLength);
                entity.Property(e => e.ZamanLastEdit).HasMaxLength(PersianDateLength);

                entity.HasOne(e => e.CustomerSoftware)
                    .WithMany(cs => cs.CustomerSoftwareDbs)
                    .HasForeignKey(e => e.tblCustomerSoftwareId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Db)
                    .WithMany(d => d.CustomerSoftwareDbs)
                    .HasForeignKey(e => e.tblDbId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        }

        /// <summary>
        /// Override SaveChangesAsync برای محاسبه Integrity Hash
        /// پیاده‌سازی الزام FDP_SDI.2.1 از ISO 15408
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // محاسبه Integrity Hash برای Entityهای حساس قبل از ذخیره
            IDataIntegrityService? integrityService = null;
            if (_serviceProvider != null)
            {
                integrityService = _serviceProvider.GetService<IDataIntegrityService>();
            }
            
            if (integrityService != null)
            {
                var entries = ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                    .ToList();

                foreach (var entry in entries)
                {
                    var entity = entry.Entity;
                    
                    // فقط Entityهایی که از BaseEntity ارث‌بری می‌کنند
                    if (entity is BaseEntity baseEntity)
                    {
                        var sensitiveFields = GetSensitiveFieldsForEntity(entity.GetType());

                        if (sensitiveFields != null && sensitiveFields.Length > 0)
                        {
                            var hash = integrityService.ComputeIntegrityHash(entity, sensitiveFields);
                            baseEntity.IntegrityHash = hash;
                        }
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Override SaveChanges برای محاسبه Integrity Hash
        /// </summary>
        public override int SaveChanges()
        {
            // محاسبه Integrity Hash برای Entityهای حساس قبل از ذخیره
            IDataIntegrityService? integrityService = null;
            if (_serviceProvider != null)
            {
                integrityService = _serviceProvider.GetService<IDataIntegrityService>();
            }
            
            if (integrityService != null)
            {
                var entries = ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                    .ToList();

                foreach (var entry in entries)
                {
                    var entity = entry.Entity;
                    
                    if (entity is BaseEntity baseEntity)
                    {
                        var sensitiveFields = GetSensitiveFieldsForEntity(entity.GetType());

                        if (sensitiveFields != null && sensitiveFields.Length > 0)
                        {
                            var hash = integrityService.ComputeIntegrityHash(entity, sensitiveFields);
                            baseEntity.IntegrityHash = hash;
                        }
                    }
                }
            }

            return base.SaveChanges();
        }

        /// <summary>
        /// تعیین فیلدهای حساس برای هر Entity
        /// </summary>
        private string[]? GetSensitiveFieldsForEntity(Type entityType)
        {
            var entityName = entityType.Name;

            return entityName switch
            {
                nameof(tblUser) => new[] { "UserName", "Password", "Email", "MobileNumber", "IsMfaEnabled", "MfaSecretKey" },
                nameof(SecuritySetting) => new[] { "SettingKey", "SettingValue" },
                nameof(CryptographicKeyEntity) => new[] { "EncryptedKeyValue", "EncryptionIV", "Purpose" },
                nameof(tblPermission) => new[] { "Name", "Resource", "Action" },
                nameof(PasswordHistory) => new[] { "PasswordHash", "UserId" },
                // Management Entities
                nameof(tblCustomer) => new[] { "CustomerCode", "Name", "NationalId", "CompanyNationalId" },
                nameof(tblCustomerSoftware) => new[] { "LicenseKey", "tblCustomerId", "tblSoftwareId", "tblPlanId", "LicenseCount" },
                nameof(tblDb) => new[] { "DbCode", "ServerName", "DatabaseName", "tblCustomerId" },
                _ => null // Entityهای دیگر نیاز به Integrity Check ندارند
            };
        }
    }
}
