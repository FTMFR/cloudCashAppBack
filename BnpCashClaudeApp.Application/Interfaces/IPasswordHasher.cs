namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس برای Hash و Verify کردن پسورد
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hash کردن پسورد
        /// </summary>
        string HashPassword(string password);

        /// <summary>
        /// بررسی صحت پسورد با Hash ذخیره شده
        /// </summary>
        /// <returns>true اگر پسورد صحیح باشد، false در غیر این صورت</returns>
        bool VerifyPassword(string hashedPassword, string providedPassword);
    }
}

