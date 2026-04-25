using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس مدیریت چرخه حیات کلید رمزنگاری
    /// پیاده‌سازی الزامات FCS_CKM از استاندارد ISO 15408
    /// 
    /// الزامات پیاده‌سازی شده:
    /// - FCS_CKM.1: تولید کلید
    /// - FCS_CKM.2: توزیع کلید
    /// - FCS_CKM.3: دسترسی به کلید
    /// - FCS_CKM.4: تخریب کلید
    /// 
    /// قابلیت‌ها:
    /// - ذخیره‌سازی امن کلیدها
    /// - Rotation خودکار کلیدها
    /// - تخریب امن کلیدهای منقضی
    /// - مدیریت چرخه حیات کلید
    /// </summary>
    public interface IKeyManagementService
    {
        #region Key Storage & Retrieval

        /// <summary>
        /// ذخیره کلید جدید
        /// </summary>
        /// <param name="keyPurpose">هدف استفاده از کلید (مثلاً JWT، Encryption)</param>
        /// <param name="keyValue">مقدار کلید (رمزنگاری شده ذخیره می‌شود)</param>
        /// <param name="expiresAt">زمان انقضا (null = بدون انقضا)</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>شناسه کلید</returns>
        Task<Guid> StoreKeyAsync(
            string keyPurpose,
            byte[] keyValue,
            DateTime? expiresAt = null,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت کلید فعال بر اساس هدف
        /// </summary>
        /// <param name="keyPurpose">هدف استفاده از کلید</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>کلید فعال یا null</returns>
        Task<CryptographicKey?> GetActiveKeyAsync(
            string keyPurpose,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت کلید بر اساس شناسه
        /// </summary>
        /// <param name="keyId">شناسه کلید</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>کلید یا null</returns>
        Task<CryptographicKey?> GetKeyByIdAsync(
            Guid keyId,
            CancellationToken ct = default);

        /// <summary>
        /// دریافت تمام کلیدهای یک هدف
        /// </summary>
        /// <param name="keyPurpose">هدف استفاده از کلید</param>
        /// <param name="includeExpired">شامل کلیدهای منقضی</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>لیست کلیدها</returns>
        Task<List<CryptographicKey>> GetKeysByPurposeAsync(
            string keyPurpose,
            bool includeExpired = false,
            CancellationToken ct = default);

        #endregion

        #region Key Rotation

        /// <summary>
        /// چرخش کلید (ایجاد کلید جدید و غیرفعال کردن کلید قبلی)
        /// </summary>
        /// <param name="keyPurpose">هدف استفاده از کلید</param>
        /// <param name="newKeyValue">مقدار کلید جدید</param>
        /// <param name="gracePeriodMinutes">دوره انتقال (دقیقه) - کلید قبلی در این مدت معتبر می‌ماند</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>شناسه کلید جدید</returns>
        Task<Guid> RotateKeyAsync(
            string keyPurpose,
            byte[] newKeyValue,
            int gracePeriodMinutes = 60,
            CancellationToken ct = default);

        /// <summary>
        /// چرخش خودکار کلید (تولید کلید جدید)
        /// </summary>
        /// <param name="keyPurpose">هدف استفاده از کلید</param>
        /// <param name="keyLengthBits">طول کلید به بیت</param>
        /// <param name="gracePeriodMinutes">دوره انتقال (دقیقه)</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>شناسه کلید جدید</returns>
        Task<Guid> AutoRotateKeyAsync(
            string keyPurpose,
            int keyLengthBits = 256,
            int gracePeriodMinutes = 60,
            CancellationToken ct = default);

        /// <summary>
        /// بررسی نیاز به چرخش کلید
        /// </summary>
        /// <param name="keyPurpose">هدف استفاده از کلید</param>
        /// <param name="maxAgeDays">حداکثر سن کلید (روز)</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>true اگر نیاز به چرخش باشد</returns>
        Task<bool> NeedsRotationAsync(
            string keyPurpose,
            int maxAgeDays = 90,
            CancellationToken ct = default);

        #endregion

        #region Key Destruction (FCS_CKM.4)

        /// <summary>
        /// تخریب امن کلید
        /// </summary>
        /// <param name="keyId">شناسه کلید</param>
        /// <param name="reason">دلیل تخریب</param>
        /// <param name="ct">CancellationToken</param>
        Task DestroyKeyAsync(
            Guid keyId,
            string reason,
            CancellationToken ct = default);

        /// <summary>
        /// تخریب امن تمام کلیدهای منقضی
        /// </summary>
        /// <param name="ct">CancellationToken</param>
        /// <returns>تعداد کلیدهای تخریب شده</returns>
        Task<int> DestroyExpiredKeysAsync(CancellationToken ct = default);

        /// <summary>
        /// تخریب امن تمام کلیدهای یک هدف
        /// </summary>
        /// <param name="keyPurpose">هدف استفاده از کلید</param>
        /// <param name="reason">دلیل تخریب</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>تعداد کلیدهای تخریب شده</returns>
        Task<int> DestroyKeysByPurposeAsync(
            string keyPurpose,
            string reason,
            CancellationToken ct = default);

        #endregion

        #region Key Lifecycle

        /// <summary>
        /// فعال‌سازی کلید
        /// </summary>
        /// <param name="keyId">شناسه کلید</param>
        /// <param name="ct">CancellationToken</param>
        Task ActivateKeyAsync(Guid keyId, CancellationToken ct = default);

        /// <summary>
        /// غیرفعال‌سازی کلید
        /// </summary>
        /// <param name="keyId">شناسه کلید</param>
        /// <param name="reason">دلیل غیرفعال‌سازی</param>
        /// <param name="ct">CancellationToken</param>
        Task DeactivateKeyAsync(Guid keyId, string reason, CancellationToken ct = default);

        /// <summary>
        /// دریافت وضعیت کلید
        /// </summary>
        /// <param name="keyId">شناسه کلید</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>وضعیت کلید</returns>
        Task<KeyStatus?> GetKeyStatusAsync(Guid keyId, CancellationToken ct = default);

        /// <summary>
        /// دریافت آمار کلیدها
        /// </summary>
        /// <param name="ct">CancellationToken</param>
        /// <returns>آمار کلیدها</returns>
        Task<KeyStatistics> GetKeyStatisticsAsync(CancellationToken ct = default);

        #endregion
    }

    /// <summary>
    /// مدل کلید رمزنگاری
    /// </summary>
    public class CryptographicKey
    {
        /// <summary>
        /// شناسه یکتای کلید
        /// </summary>
        public Guid KeyId { get; set; }

        /// <summary>
        /// هدف استفاده از کلید
        /// </summary>
        public string Purpose { get; set; } = string.Empty;

        /// <summary>
        /// مقدار کلید (رمزگشایی شده)
        /// </summary>
        public byte[] KeyValue { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// وضعیت کلید
        /// </summary>
        public KeyStatus Status { get; set; }

        /// <summary>
        /// زمان ایجاد
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// زمان انقضا
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// زمان آخرین استفاده
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// طول کلید به بیت
        /// </summary>
        public int KeyLengthBits => KeyValue.Length * 8;
    }

    /// <summary>
    /// وضعیت کلید
    /// </summary>
    public enum KeyStatus
    {
        /// <summary>
        /// در انتظار فعال‌سازی
        /// </summary>
        Pending = 0,

        /// <summary>
        /// فعال و قابل استفاده
        /// </summary>
        Active = 1,

        /// <summary>
        /// غیرفعال (در دوره انتقال)
        /// </summary>
        Inactive = 2,

        /// <summary>
        /// منقضی شده
        /// </summary>
        Expired = 3,

        /// <summary>
        /// تخریب شده
        /// </summary>
        Destroyed = 4,

        /// <summary>
        /// لغو شده (به دلیل امنیتی)
        /// </summary>
        Revoked = 5
    }

    /// <summary>
    /// آمار کلیدها
    /// </summary>
    public class KeyStatistics
    {
        public int TotalKeys { get; set; }
        public int ActiveKeys { get; set; }
        public int ExpiredKeys { get; set; }
        public int DestroyedKeys { get; set; }
        public Dictionary<string, int> KeysByPurpose { get; set; } = new Dictionary<string, int>();
        public DateTime? OldestActiveKeyDate { get; set; }
        public DateTime? NewestKeyDate { get; set; }
    }
}

