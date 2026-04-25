using BnpCashClaudeApp.Domain.Common;
using System;

namespace BnpCashClaudeApp.Domain.Entities.SecuritySubsystem
{
    /// <summary>
    /// موجودیت کلید رمزنگاری
    /// ============================================
    /// پیاده‌سازی الزامات FCS_CKM از استاندارد ISO 15408
    /// 
    /// استفاده:
    /// - ذخیره کلیدهای JWT
    /// - ذخیره کلیدهای AES
    /// - ذخیره کلیدهای HMAC
    /// - مدیریت چرخه حیات کلیدها
    /// ============================================
    /// </summary>
    public class CryptographicKeyEntity : BaseEntity
    {
        /// <summary>
        /// شناسه یکتای کلید (GUID)
        /// برای ارجاع خارجی استفاده می‌شود
        /// </summary>
        public Guid KeyId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// هدف استفاده از کلید
        /// مثال: "JWT", "AES-Encryption", "HMAC-Signing", "API-Key"
        /// </summary>
        public string Purpose { get; set; } = string.Empty;

        /// <summary>
        /// مقدار کلید (رمزنگاری شده با Master Key)
        /// </summary>
        public string EncryptedKeyValue { get; set; } = string.Empty;

        /// <summary>
        /// IV استفاده شده برای رمزنگاری کلید
        /// </summary>
        public string EncryptionIV { get; set; } = string.Empty;

        /// <summary>
        /// HMAC-SHA256 محاسبه‌شده روی IV || Ciphertext (Encrypt-then-MAC)
        /// برای تشخیص دستکاری داده‌های رمزنگاری‌شده
        /// </summary>
        public string? EncryptionMAC { get; set; }

        /// <summary>
        /// طول کلید به بیت
        /// </summary>
        public int KeyLengthBits { get; set; }

        /// <summary>
        /// الگوریتم کلید
        /// مثال: "AES-256", "HMAC-SHA256", "RSA-2048"
        /// </summary>
        public string Algorithm { get; set; } = string.Empty;

        /// <summary>
        /// وضعیت کلید
        /// 0: Pending, 1: Active, 2: Inactive, 3: Expired, 4: Destroyed, 5: Revoked
        /// </summary>
        public int Status { get; set; } = 0;

        /// <summary>
        /// زمان فعال‌سازی (UTC)
        /// </summary>
        public DateTime? ActivatedAt { get; set; }

        /// <summary>
        /// زمان انقضا (UTC)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// زمان آخرین استفاده (UTC)
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// زمان غیرفعال‌سازی (UTC)
        /// </summary>
        public DateTime? DeactivatedAt { get; set; }

        /// <summary>
        /// دلیل غیرفعال‌سازی
        /// </summary>
        public string? DeactivationReason { get; set; }

        /// <summary>
        /// زمان تخریب (UTC)
        /// </summary>
        public DateTime? DestroyedAt { get; set; }

        /// <summary>
        /// دلیل تخریب
        /// </summary>
        public string? DestructionReason { get; set; }

        /// <summary>
        /// نسخه کلید (برای Key Rotation)
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// شناسه کلید قبلی (در صورت Rotation)
        /// </summary>
        public Guid? PreviousKeyId { get; set; }

        /// <summary>
        /// شناسه کلید جایگزین (در صورت Rotation)
        /// </summary>
        public Guid? ReplacedByKeyId { get; set; }

        /// <summary>
        /// زمان پایان دوره انتقال (Grace Period)
        /// در این مدت کلید قبلی هنوز معتبر است
        /// </summary>
        public DateTime? GracePeriodEndsAt { get; set; }

        /// <summary>
        /// هش کلید (برای اعتبارسنجی بدون رمزگشایی)
        /// </summary>
        public string KeyHash { get; set; } = string.Empty;

        /// <summary>
        /// توضیحات
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// تعداد دفعات استفاده
        /// </summary>
        public long UsageCount { get; set; } = 0;

        /// <summary>
        /// بررسی اینکه کلید فعال است یا نه
        /// </summary>
        public bool IsActive => Status == 1 && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);

        /// <summary>
        /// بررسی اینکه کلید در دوره انتقال است
        /// </summary>
        public bool IsInGracePeriod => Status == 2 && GracePeriodEndsAt.HasValue && GracePeriodEndsAt > DateTime.UtcNow;

        /// <summary>
        /// بررسی اینکه کلید قابل استفاده است (فعال یا در دوره انتقال)
        /// </summary>
        public bool IsUsable => IsActive || IsInGracePeriod;
    }
}

