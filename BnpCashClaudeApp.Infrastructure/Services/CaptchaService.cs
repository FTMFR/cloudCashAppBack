using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BnpCashClaudeApp.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// سرویس تولید و اعتبارسنجی CAPTCHA
    /// الزام FIA_UAU.5 - لایه امنیتی اضافی برای MFA
    /// ============================================
    /// - تنظیمات از SecuritySettings خوانده می‌شود
    /// - اضافه کردن خطوط نویز برای جلوگیری از OCR
    /// - ذخیره کد در Memory Cache با انقضای قابل تنظیم
    /// ============================================
    /// </summary>
    public class CaptchaService : ICaptchaService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CaptchaService> _logger;
        private readonly ISecuritySettingsService _securitySettingsService;
        private readonly Random _random = new Random();

        // ذخیره آخرین CAPTCHA برای دسترسی سریع
        private CaptchaResult? _lastCaptcha;

        public CaptchaService(
            IMemoryCache cache,
            ILogger<CaptchaService> logger,
            ISecuritySettingsService securitySettingsService)
        {
            _cache = cache;
            _logger = logger;
            _securitySettingsService = securitySettingsService;
        }

        /// <summary>
        /// تولید CAPTCHA جدید
        /// </summary>
        public async Task<string> GenerateCaptchaAsync(CancellationToken ct = default)
        {
            // ============================================
            // دریافت تنظیمات از SecuritySettings
            // ============================================
            var settings = await _securitySettingsService.GetCaptchaSettingsAsync(ct);

            // ============================================
            // تولید کد تصادفی
            // ============================================
            var chars = "0123456789";
            var captchaCode = new string(
                Enumerable.Range(0, settings.CodeLength)
                    .Select(_ => chars[_random.Next(chars.Length)])
                    .ToArray()
            );

            // ============================================
            // ایجاد تصویر
            // ============================================
            using var bmp = new Bitmap(settings.ImageWidth, settings.ImageHeight);
            using var g = Graphics.FromImage(bmp);
            
            // پس‌زمینه
            g.Clear(Color.FromArgb(240, 240, 240));

            // تنظیمات رندر برای کیفیت بهتر
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // ============================================
            // نوشتن کد با فونت‌های متنوع
            // ============================================
            var fonts = new[] { "Arial", "Verdana", "Tahoma", "Georgia" };
            var fontSizes = new[] { 18, 20, 22 };
            var colors = new[] { Color.DarkBlue, Color.DarkRed, Color.DarkGreen, Color.Black, Color.DarkMagenta };

            int xPosition = 15;
            foreach (char c in captchaCode)
            {
                var fontName = fonts[_random.Next(fonts.Length)];
                var fontSize = fontSizes[_random.Next(fontSizes.Length)];
                var color = colors[_random.Next(colors.Length)];
                var yOffset = _random.Next(-3, 4);

                using var font = new Font(fontName, fontSize, FontStyle.Bold);
                using var brush = new SolidBrush(color);
                
                g.DrawString(c.ToString(), font, brush, xPosition, 5 + yOffset);
                xPosition += 20 + _random.Next(-2, 3);
            }

            // ============================================
            // اضافه کردن خطوط نویز
            // ============================================
            for (int i = 0; i < settings.NoiseLineCount; i++)
            {
                var lineColor = Color.FromArgb(
                    _random.Next(100, 180),
                    _random.Next(100, 180),
                    _random.Next(100, 180)
                );
                using var pen = new Pen(lineColor, 1);
                
                g.DrawLine(pen,
                    _random.Next(0, settings.ImageWidth),
                    _random.Next(0, settings.ImageHeight),
                    _random.Next(0, settings.ImageWidth),
                    _random.Next(0, settings.ImageHeight));
            }

            // ============================================
            // اضافه کردن نقاط نویز
            // ============================================
            for (int i = 0; i < settings.NoiseDotCount; i++)
            {
                var dotColor = Color.FromArgb(
                    _random.Next(100, 200),
                    _random.Next(100, 200),
                    _random.Next(100, 200)
                );
                bmp.SetPixel(
                    _random.Next(settings.ImageWidth),
                    _random.Next(settings.ImageHeight),
                    dotColor);
            }

            // ============================================
            // تبدیل به Base64
            // ============================================
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            var base64Image = $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";

            // ============================================
            // تولید شناسه یکتا و ذخیره در کش
            // ============================================
            var captchaId = Guid.NewGuid().ToString();
            _cache.Set(captchaId, captchaCode, TimeSpan.FromMinutes(settings.ExpiryMinutes));

            // ذخیره برای دسترسی سریع
            _lastCaptcha = new CaptchaResult
            {
                CaptchaId = captchaId,
                ImageBase64 = base64Image
            };

            _logger.LogDebug("CAPTCHA generated with ID: {CaptchaId}, Length: {Length}", captchaId, settings.CodeLength);

            return captchaCode;
        }

        /// <summary>
        /// دریافت آخرین CAPTCHA تولید شده
        /// </summary>
        public CaptchaResult? GetLastCaptcha()
        {
            return _lastCaptcha;
        }

        /// <summary>
        /// اعتبارسنجی ورودی کاربر
        /// </summary>
        public bool ValidateCaptcha(string captchaId, string userInput, out string message)
        {
            // ============================================
            // بررسی ورودی‌های خالی
            // ============================================
            if (string.IsNullOrEmpty(captchaId) || string.IsNullOrEmpty(userInput))
            {
                message = "اطلاعات کپچا ناقص است.";
                _logger.LogWarning("CAPTCHA validation failed: Empty input");
                return false;
            }

            // ============================================
            // بررسی وجود کد در کش
            // ============================================
            if (!_cache.TryGetValue(captchaId, out string? storedCode))
            {
                message = "کپچا منقضی شده است. لطفاً یک کپچای جدید دریافت کنید.";
                _logger.LogWarning("CAPTCHA validation failed: Expired or not found. ID: {CaptchaId}", captchaId);
                return false;
            }

            // ============================================
            // مقایسه کد
            // ============================================
            if (string.Equals(storedCode, userInput.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                // حذف کد پس از استفاده موفق (هر کد فقط یک‌بار قابل استفاده)
                _cache.Remove(captchaId);
                message = "کپچا صحیح است.";
                _logger.LogDebug("CAPTCHA validated successfully. ID: {CaptchaId}", captchaId);
                return true;
            }

            // ============================================
            // کد اشتباه - حذف از کش
            // ============================================
            _cache.Remove(captchaId);
            message = "کد کپچا اشتباه است.";
            _logger.LogWarning("CAPTCHA validation failed: Wrong code. ID: {CaptchaId}", captchaId);
            return false;
        }

        /// <summary>
        /// آیا CAPTCHA فعال است؟
        /// </summary>
        public async Task<bool> IsEnabledAsync(CancellationToken ct = default)
        {
            var settings = await _securitySettingsService.GetCaptchaSettingsAsync(ct);
            return settings.IsEnabled;
        }

        /// <summary>
        /// آیا در MFA نیاز به CAPTCHA است؟
        /// </summary>
        public async Task<bool> IsRequiredOnMfaAsync(CancellationToken ct = default)
        {
            var settings = await _securitySettingsService.GetCaptchaSettingsAsync(ct);
            return settings.IsEnabled && settings.RequireOnMfa;
        }
    }
}
