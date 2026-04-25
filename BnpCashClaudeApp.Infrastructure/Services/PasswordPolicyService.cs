using System;
using System.Collections.Generic;
using System.Linq;
using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// پیاده‌سازی سرویس سیاست رمز عبور
    /// پیاده‌سازی الزامات پروفایل حفاظتی برنامه‌های کاربردی تحت شبکه (ISO 15408)
    /// </summary>
    public class PasswordPolicyService : IPasswordPolicyService
    {
        private readonly PasswordPolicySettings _settings;
        private readonly ILogger<PasswordPolicyService> _logger;

        // لیست رمزهای عبور رایج و ضعیف
        private static readonly HashSet<string> CommonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "123456", "12345678", "qwerty", "abc123", "111111",
            "password1", "123456789", "1234567", "iloveyou", "admin", "welcome",
            "monkey", "login", "princess", "dragon", "passw0rd", "master",
            "letmein", "baseball", "shadow", "sunshine", "superman", "1234"
        };

        public PasswordPolicyService(IConfiguration configuration, ILogger<PasswordPolicyService> logger)
        {
            _logger = logger;
            _settings = new PasswordPolicySettings();
            
            var section = configuration.GetSection("PasswordPolicy");
            if (section.Exists())
            {
                _settings.MinimumLength = section.GetValue("MinimumLength", 8);
                _settings.MaximumLength = section.GetValue("MaximumLength", 128);
                _settings.RequireUppercase = section.GetValue("RequireUppercase", true);
                _settings.RequireLowercase = section.GetValue("RequireLowercase", true);
                _settings.RequireDigit = section.GetValue("RequireDigit", true);
                _settings.RequireSpecialCharacter = section.GetValue("RequireSpecialCharacter", true);
                _settings.SpecialCharacters = section.GetValue("SpecialCharacters", "!@#$%^&*()_+-=[]{}|;':\",./<>?") ?? "!@#$%^&*()_+-=[]{}|;':\",./<>?";
                _settings.DisallowUsername = section.GetValue("DisallowUsername", true);
                _settings.PasswordHistoryCount = section.GetValue("PasswordHistoryCount", 5);
                _settings.PasswordExpirationDays = section.GetValue("PasswordExpirationDays", 90);
            }
        }

        /// <summary>
        /// اعتبارسنجی رمز عبور بر اساس سیاست‌های تعریف شده
        /// </summary>
        public PasswordValidationResult ValidatePassword(string password, string? username = null)
        {
            var result = new PasswordValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(password))
            {
                result.IsValid = false;
                result.Errors.Add("رمز عبور نمی‌تواند خالی باشد");
                return result;
            }

            // بررسی حداقل طول
            if (password.Length < _settings.MinimumLength)
            {
                result.IsValid = false;
                result.Errors.Add($"رمز عبور باید حداقل {_settings.MinimumLength} کاراکتر باشد");
            }

            // بررسی حداکثر طول
            if (password.Length > _settings.MaximumLength)
            {
                result.IsValid = false;
                result.Errors.Add($"رمز عبور نباید بیشتر از {_settings.MaximumLength} کاراکتر باشد");
            }

            // بررسی وجود حرف بزرگ
            if (_settings.RequireUppercase && !password.Any(char.IsUpper))
            {
                result.IsValid = false;
                result.Errors.Add("رمز عبور باید حداقل یک حرف بزرگ انگلیسی (A-Z) داشته باشد");
            }

            // بررسی وجود حرف کوچک
            if (_settings.RequireLowercase && !password.Any(char.IsLower))
            {
                result.IsValid = false;
                result.Errors.Add("رمز عبور باید حداقل یک حرف کوچک انگلیسی (a-z) داشته باشد");
            }

            // بررسی وجود عدد
            if (_settings.RequireDigit && !password.Any(char.IsDigit))
            {
                result.IsValid = false;
                result.Errors.Add("رمز عبور باید حداقل یک عدد (0-9) داشته باشد");
            }

            // بررسی وجود کاراکتر خاص
            if (_settings.RequireSpecialCharacter)
            {
                bool hasSpecial = password.Any(c => _settings.SpecialCharacters.Contains(c));
                if (!hasSpecial)
                {
                    result.IsValid = false;
                    result.Errors.Add($"رمز عبور باید حداقل یک کاراکتر خاص داشته باشد ({_settings.SpecialCharacters})");
                }
            }

            // بررسی عدم تشابه با نام کاربری
            if (_settings.DisallowUsername && !string.IsNullOrEmpty(username))
            {
                if (password.Contains(username, StringComparison.OrdinalIgnoreCase) ||
                    username.Contains(password, StringComparison.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Errors.Add("رمز عبور نباید شامل نام کاربری باشد");
                }
            }

            // بررسی رمزهای عبور رایج
            if (CommonPasswords.Contains(password))
            {
                result.IsValid = false;
                result.Errors.Add("این رمز عبور بسیار رایج و ناامن است");
            }

            // بررسی کاراکترهای تکراری متوالی
            if (HasConsecutiveRepeatingCharacters(password, 3))
            {
                result.IsValid = false;
                result.Errors.Add("رمز عبور نباید شامل کاراکترهای تکراری متوالی باشد (مثل aaa یا 111)");
            }

            // محاسبه امتیاز قدرت
            result.StrengthScore = CalculatePasswordStrength(password);
            result.StrengthDescription = GetStrengthDescription(result.StrengthScore);

            return result;
        }

        /// <summary>
        /// محاسبه امتیاز قدرت رمز عبور (0-100)
        /// </summary>
        public int CalculatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return 0;

            int score = 0;

            // امتیازدهی بر اساس طول
            if (password.Length >= 8) score += 10;
            if (password.Length >= 12) score += 10;
            if (password.Length >= 16) score += 10;
            if (password.Length >= 20) score += 10;

            // امتیازدهی بر اساس تنوع کاراکترها
            if (password.Any(char.IsLower)) score += 10;
            if (password.Any(char.IsUpper)) score += 15;
            if (password.Any(char.IsDigit)) score += 15;
            if (password.Any(c => _settings.SpecialCharacters.Contains(c))) score += 20;

            // کسر امتیاز برای الگوهای ضعیف
            if (HasConsecutiveRepeatingCharacters(password, 2))
                score -= 10;

            if (HasSequentialPattern(password))
                score -= 10;

            if (CommonPasswords.Contains(password))
                score -= 30;

            return Math.Max(0, Math.Min(100, score));
        }

        /// <summary>
        /// دریافت تنظیمات سیاست رمز عبور
        /// </summary>
        public PasswordPolicySettings GetPolicySettings() => _settings;

        /// <summary>
        /// بررسی اینکه آیا رمز عبور در تاریخچه رمزهای قبلی کاربر وجود دارد
        /// </summary>
        public bool IsPasswordInHistory(int userId, string newPasswordHash)
        {
            // این متد از IPasswordHistoryService استفاده می‌شود
            // پیاده‌سازی واقعی در AuthController انجام می‌شود
            return false;
        }

        private bool HasConsecutiveRepeatingCharacters(string password, int count)
        {
            for (int i = 0; i <= password.Length - count; i++)
            {
                bool allSame = true;
                for (int j = 1; j < count; j++)
                {
                    if (password[i] != password[i + j])
                    {
                        allSame = false;
                        break;
                    }
                }
                if (allSame) return true;
            }
            return false;
        }

        private bool HasSequentialPattern(string password)
        {
            string lowerPassword = password.ToLower();
            
            string[] numericPatterns = { "012", "123", "234", "345", "456", "567", "678", "789", "890" };
            string[] letterPatterns = { "abc", "bcd", "cde", "def", "efg", "fgh", "ghi", "hij", "ijk",
                                        "jkl", "klm", "lmn", "mno", "nop", "opq", "pqr", "qrs", "rst",
                                        "stu", "tuv", "uvw", "vwx", "wxy", "xyz" };
            string[] keyboardPatterns = { "qwe", "wer", "ert", "rty", "tyu", "yui", "uio", "iop",
                                          "asd", "sdf", "dfg", "fgh", "ghj", "hjk", "jkl",
                                          "zxc", "xcv", "cvb", "vbn", "bnm" };

            foreach (var pattern in numericPatterns.Concat(letterPatterns).Concat(keyboardPatterns))
            {
                if (lowerPassword.Contains(pattern))
                    return true;
            }

            return false;
        }

        private string GetStrengthDescription(int score)
        {
            return score switch
            {
                < 20 => "بسیار ضعیف",
                < 40 => "ضعیف",
                < 60 => "متوسط",
                < 80 => "قوی",
                _ => "بسیار قوی"
            };
        }
    }
}
