namespace BnpCashClaudeApp.Domain.Common
{
    /// <summary>
    /// نرمال‌سازی متن فارسی
    /// ============================================
    /// تبدیل کاراکترهای عربی به معادل فارسی
    /// این کلاس برای یکدست‌سازی حروف فارسی و عربی استفاده می‌شود
    /// تا مشکلات جستجو و مقایسه رشته‌ها برطرف شود
    /// ============================================
    /// کاراکترهای مشکل‌ساز:
    /// - ي عربی (U+064A) → ی فارسی (U+06CC)
    /// - ك عربی (U+0643) → ک فارسی (U+06A9)
    /// - ٤ عربی (U+0664) → ۴ فارسی (U+06F4)
    /// - ٥ عربی (U+0665) → ۵ فارسی (U+06F5)
    /// - ٦ عربی (U+0666) → ۶ فارسی (U+06F6)
    /// ============================================
    /// </summary>
    public static class PersianTextNormalizer
    {
        /// <summary>
        /// نرمال‌سازی کامل متن فارسی
        /// تبدیل حروف عربی ی و ک و اعداد عربی به معادل فارسی
        /// </summary>
        /// <param name="input">متن ورودی</param>
        /// <returns>متن نرمال‌سازی شده</returns>
        public static string NormalizePersianText(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input ?? string.Empty;

            return input
                // حروف
                .Replace('\u064A', '\u06CC')  // ي عربی → ی فارسی
                .Replace('\u0643', '\u06A9')  // ك عربی → ک فارسی
                // اعداد عربی → فارسی
                .Replace('\u0660', '\u06F0')  // ٠ → ۰
                .Replace('\u0661', '\u06F1')  // ١ → ۱
                .Replace('\u0662', '\u06F2')  // ٢ → ۲
                .Replace('\u0663', '\u06F3')  // ٣ → ۳
                .Replace('\u0664', '\u06F4')  // ٤ → ۴
                .Replace('\u0665', '\u06F5')  // ٥ → ۵
                .Replace('\u0666', '\u06F6')  // ٦ → ۶
                .Replace('\u0667', '\u06F7')  // ٧ → ۷
                .Replace('\u0668', '\u06F8')  // ٨ → ۸
                .Replace('\u0669', '\u06F9'); // ٩ → ۹
        }

        /// <summary>
        /// نرمال‌سازی فقط حروف ی و ک
        /// برای مواردی که فقط مشکل حروف وجود دارد
        /// </summary>
        /// <param name="input">متن ورودی</param>
        /// <returns>متن نرمال‌سازی شده</returns>
        public static string NormalizeYeKe(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input ?? string.Empty;

            return input
                .Replace('\u064A', '\u06CC')  // ي عربی → ی فارسی
                .Replace('\u0643', '\u06A9'); // ك عربی → ک فارسی
        }
    }
}
