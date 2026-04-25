using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Entities.SecuritySubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// سرویس مدیریت تاریخچه رمز عبور
    /// ============================================
    /// الزام FDP (User Data Protection) از ISO 15408
    /// جلوگیری از استفاده مجدد رمزهای قبلی
    /// ============================================
    /// </summary>
    public class PasswordHistoryService : IPasswordHistoryService
    {
        private readonly NavigationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public PasswordHistoryService(
            NavigationDbContext context,
            IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        /// <summary>
        /// افزودن رمز عبور به تاریخچه
        /// </summary>
        public async Task AddToHistoryAsync(long userId, string passwordHash, string? ipAddress = null)
        {
            var persianCalendar = new PersianCalendar();
            var now = DateTime.UtcNow;
            var persianDate = $"{persianCalendar.GetYear(now):0000}/{persianCalendar.GetMonth(now):00}/{persianCalendar.GetDayOfMonth(now):00} {now:HH:mm:ss}";

            var history = new PasswordHistory
            {
                UserId = userId,
                PasswordHash = passwordHash,
                SetAt = persianDate,
                IpAddress = ipAddress
            };

            _context.PasswordHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// بررسی اینکه آیا رمز عبور در تاریخچه وجود دارد
        /// </summary>
        public async Task<bool> IsPasswordInHistoryAsync(long userId, string plainPassword, int historyCount)
        {
            if (historyCount <= 0)
                return false;

            // دریافت آخرین رمزهای عبور کاربر
            var recentPasswords = await _context.PasswordHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.Id)
                .Take(historyCount)
                .Select(h => h.PasswordHash)
                .ToListAsync();

            // بررسی هر Hash با رمز عبور جدید
            foreach (var storedHash in recentPasswords)
            {
                if (_passwordHasher.VerifyPassword(storedHash, plainPassword))
                {
                    return true; // رمز عبور تکراری است
                }
            }

            return false;
        }

        /// <summary>
        /// پاکسازی تاریخچه قدیمی (نگهداری فقط تعداد مشخص)
        /// </summary>
        public async Task CleanupOldHistoryAsync(long userId, int keepCount)
        {
            if (keepCount <= 0)
                return;

            // دریافت شناسه‌هایی که باید نگه داشته شوند
            var keepIds = await _context.PasswordHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.Id)
                .Take(keepCount)
                .Select(h => h.Id)
                .ToListAsync();

            // حذف رکوردهای قدیمی
            var toDelete = await _context.PasswordHistories
                .Where(h => h.UserId == userId && !keepIds.Contains(h.Id))
                .ToListAsync();

            if (toDelete.Any())
            {
                _context.PasswordHistories.RemoveRange(toDelete);
                await _context.SaveChangesAsync();
            }
        }
    }
}
