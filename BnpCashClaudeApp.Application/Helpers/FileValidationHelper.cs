using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BnpCashClaudeApp.Application.Helpers
{
    /// <summary>
    /// کمکی برای اعتبارسنجی فایل‌ها
    /// ============================================
    /// پیاده‌سازی الزامات امنیتی ISO 15408:
    /// - FDP_ITC.2: ورود داده با مشخصه امنیتی
    /// - جلوگیری از آپلود فایل‌های مخرب
    /// ============================================
    /// </summary>
    public static class FileValidationHelper
    {
        /// <summary>
        /// امضاهای Magic Bytes برای فرمت‌های مختلف
        /// </summary>
        private static readonly Dictionary<string, byte[][]> MagicBytesSignatures = new Dictionary<string, byte[][]>
        {
            // تصاویر
            { ".jpg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
            { ".gif", new[] { new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, // GIF87a
                              new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 } } }, // GIF89a
            { ".webp", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } }, // RIFF (needs additional check)
            { ".bmp", new[] { new byte[] { 0x42, 0x4D } } },
            { ".ico", new[] { new byte[] { 0x00, 0x00, 0x01, 0x00 } } },
            { ".tiff", new[] { new byte[] { 0x49, 0x49, 0x2A, 0x00 }, // Little-endian
                               new byte[] { 0x4D, 0x4D, 0x00, 0x2A } } }, // Big-endian
            { ".tif", new[] { new byte[] { 0x49, 0x49, 0x2A, 0x00 },
                              new byte[] { 0x4D, 0x4D, 0x00, 0x2A } } },

            // اسناد
            { ".pdf", new[] { new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D } } }, // %PDF-
            { ".doc", new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } }, // OLE
            { ".xls", new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } }, // OLE
            { ".ppt", new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } } }, // OLE
            { ".docx", new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } }, // ZIP (Office Open XML)
            { ".xlsx", new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } }, // ZIP
            { ".pptx", new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } }, // ZIP

            // فشرده
            { ".zip", new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 },
                              new byte[] { 0x50, 0x4B, 0x05, 0x06 },
                              new byte[] { 0x50, 0x4B, 0x07, 0x08 } } },
            { ".rar", new[] { new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 } } },
            { ".7z", new[] { new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C } } },
            { ".gz", new[] { new byte[] { 0x1F, 0x8B } } },
            { ".tar", new[] { new byte[] { 0x75, 0x73, 0x74, 0x61, 0x72 } } }, // at offset 257

            // صوتی/تصویری
            { ".mp3", new[] { new byte[] { 0xFF, 0xFB },
                              new byte[] { 0xFF, 0xFA },
                              new byte[] { 0x49, 0x44, 0x33 } } }, // ID3
            { ".mp4", new[] { new byte[] { 0x00, 0x00, 0x00 } } }, // needs ftyp check
            { ".wav", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } }, // RIFF
            
            // متنی (اختیاری - معمولاً بدون Magic Bytes)
            { ".txt", new[] { new byte[] { 0xEF, 0xBB, 0xBF }, // UTF-8 BOM
                              new byte[] { 0xFF, 0xFE }, // UTF-16 LE BOM
                              new byte[] { 0xFE, 0xFF } } }, // UTF-16 BE BOM
            { ".xml", new[] { new byte[] { 0x3C, 0x3F, 0x78, 0x6D, 0x6C } } }, // <?xml
            { ".json", new[] { new byte[] { 0x7B }, new byte[] { 0x5B } } }, // { or [
        };

        /// <summary>
        /// فرمت‌هایی که نیاز به بررسی اضافی دارند
        /// </summary>
        private static readonly HashSet<string> FormatsNeedingAdditionalCheck = new HashSet<string>
        {
            ".webp", ".mp4", ".wav"
        };

        /// <summary>
        /// فرمت‌های متنی که Magic Bytes اختیاری است
        /// </summary>
        private static readonly HashSet<string> TextBasedFormats = new HashSet<string>
        {
            ".txt", ".csv", ".json", ".xml", ".html", ".css", ".js"
        };

        /// <summary>
        /// نتیجه اعتبارسنجی فایل
        /// </summary>
        public class FileValidationResult
        {
            public bool IsValid { get; set; }
            public string? ErrorMessage { get; set; }
            public string? DetectedFormat { get; set; }
            public bool MagicBytesMatch { get; set; }

            public static FileValidationResult Success(string? detectedFormat = null)
            {
                return new FileValidationResult { IsValid = true, MagicBytesMatch = true, DetectedFormat = detectedFormat };
            }

            public static FileValidationResult Failure(string errorMessage)
            {
                return new FileValidationResult { IsValid = false, ErrorMessage = errorMessage };
            }
        }

        /// <summary>
        /// اعتبارسنجی کامل فایل
        /// </summary>
        /// <param name="stream">استریم فایل</param>
        /// <param name="fileName">نام فایل</param>
        /// <param name="contentType">نوع محتوا</param>
        /// <param name="allowedExtensions">پسوندهای مجاز</param>
        /// <param name="maxSizeBytes">حداکثر حجم (بایت)</param>
        /// <param name="validateMagicBytes">آیا Magic Bytes بررسی شود</param>
        /// <returns>نتیجه اعتبارسنجی</returns>
        public static FileValidationResult ValidateFile(
            Stream stream,
            string fileName,
            string? contentType,
            string[] allowedExtensions,
            long maxSizeBytes,
            bool validateMagicBytes = true)
        {
            // 1. بررسی وجود فایل
            if (stream == null || stream.Length == 0)
            {
                return FileValidationResult.Failure("فایلی انتخاب نشده است");
            }

            // 2. بررسی حجم فایل
            if (stream.Length > maxSizeBytes)
            {
                var maxSizeMB = maxSizeBytes / (1024 * 1024);
                return FileValidationResult.Failure($"حجم فایل نباید بیشتر از {maxSizeMB} مگابایت باشد");
            }

            // 3. بررسی پسوند فایل
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
            {
                return FileValidationResult.Failure("فایل باید دارای پسوند باشد");
            }

            var normalizedAllowed = allowedExtensions
                .Select(e => e.TrimStart('.').ToLowerInvariant())
                .ToArray();

            if (!normalizedAllowed.Contains(extension.TrimStart('.')))
            {
                return FileValidationResult.Failure($"پسوند فایل مجاز نیست. پسوندهای مجاز: {string.Join(", ", allowedExtensions)}");
            }

            // 4. بررسی Magic Bytes
            if (validateMagicBytes)
            {
                var magicResult = ValidateMagicBytes(stream, extension);
                if (!magicResult.IsValid)
                {
                    return magicResult;
                }
            }

            return FileValidationResult.Success(extension);
        }

        /// <summary>
        /// اعتبارسنجی Magic Bytes فایل
        /// </summary>
        public static FileValidationResult ValidateMagicBytes(Stream stream, string extension)
        {
            if (stream == null || stream.Length < 2)
            {
                return FileValidationResult.Failure("فایل خیلی کوچک است");
            }

            extension = extension.ToLowerInvariant();
            if (!extension.StartsWith("."))
                extension = "." + extension;

            // فرمت‌های متنی - Magic Bytes اختیاری
            if (TextBasedFormats.Contains(extension))
            {
                return FileValidationResult.Success(extension);
            }

            // بررسی وجود امضا برای این فرمت
            if (!MagicBytesSignatures.TryGetValue(extension, out var signatures))
            {
                // فرمت ناشناخته - اجازه می‌دهیم
                return FileValidationResult.Success(extension);
            }

            try
            {
                var originalPosition = stream.Position;
                stream.Position = 0;

                var maxLength = signatures.Max(s => s.Length);
                var buffer = new byte[Math.Min(maxLength + 12, stream.Length)]; // +12 برای بررسی‌های اضافی
                var bytesRead = stream.Read(buffer, 0, buffer.Length);

                stream.Position = originalPosition;

                if (bytesRead < 2)
                {
                    return FileValidationResult.Failure("فایل خیلی کوچک است");
                }

                // بررسی تطابق با امضاها
                foreach (var signature in signatures)
                {
                    if (bytesRead >= signature.Length &&
                        buffer.Take(signature.Length).SequenceEqual(signature))
                    {
                        // بررسی‌های اضافی برای فرمت‌های خاص
                        if (FormatsNeedingAdditionalCheck.Contains(extension))
                        {
                            if (!PerformAdditionalCheck(buffer, bytesRead, extension))
                            {
                                continue; // امضا تطابق دارد اما بررسی اضافی ناموفق
                            }
                        }

                        return FileValidationResult.Success(extension);
                    }
                }

                return FileValidationResult.Failure("محتوای فایل با فرمت اعلام شده مطابقت ندارد. ممکن است فایل دستکاری شده باشد.");
            }
            catch (Exception ex)
            {
                return FileValidationResult.Failure($"خطا در بررسی فایل: {ex.Message}");
            }
        }

        /// <summary>
        /// بررسی‌های اضافی برای فرمت‌های خاص
        /// </summary>
        private static bool PerformAdditionalCheck(byte[] buffer, int bytesRead, string extension)
        {
            switch (extension)
            {
                case ".webp":
                    // WebP: RIFF + size + WEBP
                    if (bytesRead >= 12)
                    {
                        // بایت‌های 8-11 باید "WEBP" باشند
                        return buffer[8] == 0x57 && // W
                               buffer[9] == 0x45 && // E
                               buffer[10] == 0x42 && // B
                               buffer[11] == 0x50;  // P
                    }
                    return false;

                case ".wav":
                    // WAV: RIFF + size + WAVE
                    if (bytesRead >= 12)
                    {
                        return buffer[8] == 0x57 && // W
                               buffer[9] == 0x41 && // A
                               buffer[10] == 0x56 && // V
                               buffer[11] == 0x45;  // E
                    }
                    return false;

                case ".mp4":
                    // MP4: باید ftyp در offset 4 یا 0 باشد
                    if (bytesRead >= 8)
                    {
                        // ftyp در offset 4
                        return (buffer[4] == 0x66 && buffer[5] == 0x74 && 
                                buffer[6] == 0x79 && buffer[7] == 0x70);
                    }
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// بررسی اینکه آیا فایل تصویر معتبر است
        /// </summary>
        public static FileValidationResult ValidateImage(
            Stream stream,
            string fileName,
            string? contentType,
            long maxSizeBytes)
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".ico", ".tiff", ".tif" };
            return ValidateFile(stream, fileName, contentType, imageExtensions, maxSizeBytes, validateMagicBytes: true);
        }

        /// <summary>
        /// تشخیص فرمت واقعی فایل از روی Magic Bytes
        /// </summary>
        public static string? DetectFileFormat(Stream stream)
        {
            if (stream == null || stream.Length < 2)
                return null;

            try
            {
                var originalPosition = stream.Position;
                stream.Position = 0;

                var buffer = new byte[Math.Min(16, stream.Length)];
                stream.Read(buffer, 0, buffer.Length);

                stream.Position = originalPosition;

                foreach (var kvp in MagicBytesSignatures)
                {
                    foreach (var signature in kvp.Value)
                    {
                        if (buffer.Length >= signature.Length &&
                            buffer.Take(signature.Length).SequenceEqual(signature))
                        {
                            return kvp.Key;
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
