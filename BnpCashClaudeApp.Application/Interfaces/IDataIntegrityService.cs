using BnpCashClaudeApp.Application.DTOs;
using System;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس بررسی صحت داده‌های ذخیره شده
    /// پیاده‌سازی الزام FDP_SDI.2.1 و FDP_SDI.2.2 از استاندارد ISO 15408
    /// 
    /// این سرویس از HMAC-SHA256 برای محاسبه و بررسی Integrity Hash استفاده می‌کند.
    /// Integrity Hash برای تشخیص تغییرات غیرمجاز در داده‌های حساس استفاده می‌شود.
    /// </summary>
    public interface IDataIntegrityService
    {
        /// <summary>
        /// محاسبه Integrity Hash برای یک Entity
        /// از HMAC-SHA256 با کلید مخفی استفاده می‌کند
        /// </summary>
        /// <param name="entity">Entity که باید Integrity Hash آن محاسبه شود</param>
        /// <param name="sensitiveFields">فیلدهای حساس که باید در Hash لحاظ شوند</param>
        /// <returns>Integrity Hash به صورت Base64</returns>
        string ComputeIntegrityHash(object entity, string[] sensitiveFields);

        /// <summary>
        /// بررسی صحت Integrity Hash یک Entity
        /// </summary>
        /// <param name="entity">Entity که باید بررسی شود</param>
        /// <param name="storedHash">Hash ذخیره شده</param>
        /// <param name="sensitiveFields">فیلدهای حساس</param>
        /// <returns>true اگر Hash معتبر باشد، false در غیر این صورت</returns>
        bool VerifyIntegrityHash(object entity, string storedHash, string[] sensitiveFields);

        /// <summary>
        /// بررسی صحت تمام Entityهای حساس در دیتابیس
        /// برای Periodic Integrity Verification (FDP_SDI.2.2)
        /// </summary>
        /// <returns>نتیجه بررسی شامل تعداد کل و تفکیک بر اساس EntityType</returns>
        Task<DataIntegrityVerificationResult> VerifyAllEntitiesIntegrityAsync();

        /// <summary>
        /// ثبت رویداد Integrity Violation در Audit Log
        /// </summary>
        Task LogIntegrityViolationAsync(string entityType, string entityId, string reason);

        /// <summary>
        /// محاسبه و به‌روزرسانی Integrity Hash برای تمام Entityهای موجود
        /// این متد برای داده‌های قدیمی که Hash ندارند استفاده می‌شود
        /// </summary>
        /// <returns>تعداد Entityهایی که Hash آن‌ها به‌روزرسانی شد</returns>
        Task<int> ComputeHashForExistingEntitiesAsync();

        /// <summary>
        /// تولید کلید Integrity امن برای استفاده در Configuration
        /// </summary>
        /// <returns>کلید Base64 encoded</returns>
        string GenerateSecureIntegrityKey();
    }
}

