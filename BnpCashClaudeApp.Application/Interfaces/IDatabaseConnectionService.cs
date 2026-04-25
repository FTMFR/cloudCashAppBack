using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس مدیریت Connection String های دیتابیس
    /// ============================================
    /// این سرویس Connection String ها را از جدول tblDbs می‌خواند
    /// و امکان رمزگذاری/رمزگشایی Password را فراهم می‌کند
    /// ============================================
    /// </summary>
    public interface IDatabaseConnectionService
    {
        /// <summary>
        /// دریافت Connection String بر اساس کد دیتابیس
        /// </summary>
        /// <param name="dbCode">کد دیتابیس (مثلاً NAV-DB, LOG-DB, CASH-DB)</param>
        /// <returns>Connection String رمزگشایی شده</returns>
        Task<string?> GetConnectionStringAsync(string dbCode);

        /// <summary>
        /// دریافت Connection String بر اساس شناسه دیتابیس
        /// </summary>
        /// <param name="dbId">شناسه دیتابیس</param>
        /// <returns>Connection String رمزگشایی شده</returns>
        Task<string?> GetConnectionStringByIdAsync(long dbId);

        /// <summary>
        /// ساخت Connection String از اجزای دیتابیس
        /// </summary>
        string BuildConnectionString(string serverName, int? port, string databaseName, 
            string? username, string? password, bool integratedSecurity = false);

        /// <summary>
        /// رمزگذاری رمز عبور
        /// </summary>
        string EncryptPassword(string plainPassword);

        /// <summary>
        /// رمزگشایی رمز عبور
        /// </summary>
        string DecryptPassword(string encryptedPassword);

        /// <summary>
        /// رمزگذاری Connection String کامل
        /// </summary>
        string EncryptConnectionString(string connectionString);

        /// <summary>
        /// رمزگشایی Connection String کامل
        /// </summary>
        string DecryptConnectionString(string encryptedConnectionString);

        /// <summary>
        /// تست اتصال به دیتابیس
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> TestConnectionAsync(string connectionString);

        /// <summary>
        /// تست اتصال به دیتابیس با کد
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> TestConnectionByCodeAsync(string dbCode);
    }
}
