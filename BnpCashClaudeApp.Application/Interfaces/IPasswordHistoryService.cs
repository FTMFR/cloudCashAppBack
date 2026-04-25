using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس مدیریت تاریخچه رمز عبور
    /// ============================================
    /// الزام FDP (User Data Protection) از ISO 15408
    /// جلوگیری از استفاده مجدد رمزهای قبلی
    /// ============================================
    /// </summary>
    public interface IPasswordHistoryService
    {
        /// <summary>
        /// افزودن رمز عبور به تاریخچه
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="passwordHash">Hash رمز عبور</param>
        /// <param name="ipAddress">آدرس IP</param>
        Task AddToHistoryAsync(long userId, string passwordHash, string? ipAddress = null);

        /// <summary>
        /// بررسی اینکه آیا رمز عبور در تاریخچه وجود دارد
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="plainPassword">رمز عبور جدید (متن ساده)</param>
        /// <param name="historyCount">تعداد رمزهای قبلی که باید بررسی شوند</param>
        /// <returns>آیا رمز عبور تکراری است</returns>
        Task<bool> IsPasswordInHistoryAsync(long userId, string plainPassword, int historyCount);

        /// <summary>
        /// پاکسازی تاریخچه قدیمی (نگهداری فقط تعداد مشخص)
        /// </summary>
        /// <param name="userId">شناسه کاربر</param>
        /// <param name="keepCount">تعداد رکوردهایی که باید نگه داشته شوند</param>
        Task CleanupOldHistoryAsync(long userId, int keepCount);
    }
}
