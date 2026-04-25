using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BnpCashClaudeApp.Domain.Common
{
    /// <summary>
    /// کلاس پایه برای تمام Entity ها
    /// ============================================
    /// تاریخ‌ها به صورت شمسی (string) در دیتابیس ذخیره می‌شوند
    /// در صورت نیاز به محاسبات، از متد ToGregorian استفاده کنید
    /// ============================================
    /// </summary>
    public abstract class BaseEntity
    {
        private static readonly PersianCalendar _persianCalendar = new PersianCalendar();
        private static TimeZoneInfo _tehranTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); 

        /// <summary>
        /// شناسه داخلی - برای Join و Index (Identity در SQL Server - bigint)
        /// فقط برای استفاده داخلی در دیتابیس و Join ها
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// شناسه عمومی - برای API و ارتباط با کاربران (GUID)
        /// این شناسه در Response های API استفاده می‌شود و قابل حدس زدن نیست
        /// Index شده برای جستجوی سریع
        /// </summary>
        public Guid PublicId { get; set; } = Guid.NewGuid();

        // ============================================
        // تاریخ ایجاد - به صورت شمسی ذخیره می‌شود
        // فرمت: 1403/09/26 11:30:00
        // ============================================
        public string ZamanInsert { get; set; } = string.Empty;

        public long TblUserGrpIdInsert { get; set; }

        // ============================================
        // تاریخ آخرین ویرایش - به صورت شمسی ذخیره می‌شود
        // ============================================
        public string? ZamanLastEdit { get; set; }

        public long? TblUserGrpIdLastEdit { get; set; }

        /// <summary>
        /// Integrity Hash برای بررسی صحت داده‌های حساس
        /// پیاده‌سازی الزام FDP_SDI.2.1 از استاندارد ISO 15408
        /// محاسبه شده با HMAC-SHA256 برای فیلدهای حساس Entity
        /// </summary>
        public string? IntegrityHash { get; set; }

        /// <summary>
        /// تنظیم تاریخ ایجاد از DateTime میلادی (تبدیل به شمسی)
        /// </summary>
        public void SetZamanInsert(DateTime dateTime)
        {
            ZamanInsert = ToPersianDateTime(dateTime);
        }

        /// <summary>
        /// تنظیم تاریخ ویرایش از DateTime میلادی (تبدیل به شمسی)
        /// </summary>
        public void SetZamanLastEdit(DateTime dateTime)
        {
            ZamanLastEdit = ToPersianDateTime(dateTime);
        }

        /// <summary>
        /// تنظیم تاریخ ایجاد به صورت مستقیم (شمسی)
        /// </summary>
        public void SetZamanInsertShamsi(string persianDateTime)
        {
            ZamanInsert = persianDateTime;
        }

        /// <summary>
        /// تنظیم تاریخ ویرایش به صورت مستقیم (شمسی)
        /// </summary>
        public void SetZamanLastEditShamsi(string persianDateTime)
        {
            ZamanLastEdit = persianDateTime;
        }

        // ============================================
        // متدهای استاتیک برای تبدیل تاریخ
        // ============================================

        /// <summary>
        /// تبدیل تاریخ میلادی به شمسی (با زمان)
        /// </summary>
        public static string ToPersianDateTime(DateTime dateTime)
        {
            DateTime utcDateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

            DateTime localTehranTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _tehranTimeZone);

            int year = _persianCalendar.GetYear(localTehranTime);
            int month = _persianCalendar.GetMonth(localTehranTime);
            int day = _persianCalendar.GetDayOfMonth(localTehranTime);

            int hour = localTehranTime.Hour;
            int minute = localTehranTime.Minute;
            int second = localTehranTime.Second;

            string persianDate = $"{year:0000}/{month:00}/{day:00}";

            string localTime = $"{hour:00}:{minute:00}:{second:00}";

            return $"{persianDate} {localTime}";
        }

        /// <summary>
        /// تبدیل تاریخ میلادی به شمسی (فقط تاریخ)
        /// </summary>
        public static string ToPersianDate(DateTime dateTime)
        {
            int year = _persianCalendar.GetYear(dateTime);
            int month = _persianCalendar.GetMonth(dateTime);
            int day = _persianCalendar.GetDayOfMonth(dateTime);

            return $"{year:0000}/{month:00}/{day:00}";
        }

        /// <summary>
        /// تبدیل تاریخ شمسی به میلادی (برای محاسبات)
        /// </summary>
        public static DateTime ToGregorianDateTime(string persianDateTime)
        {
            if (string.IsNullOrWhiteSpace(persianDateTime))
                return DateTime.MinValue;

            // الگو: 1403/09/26 11:30:00 یا 1403/09/26
            var match = Regex.Match(persianDateTime.Trim(), 
                @"^(\d{4})[\/\-](\d{1,2})[\/\-](\d{1,2})(?:\s+(\d{1,2}):(\d{1,2}):?(\d{1,2})?)?$");

            if (!match.Success)
                throw new FormatException($"فرمت تاریخ شمسی نامعتبر: {persianDateTime}");

            int year = int.Parse(match.Groups[1].Value);
            int month = int.Parse(match.Groups[2].Value);
            int day = int.Parse(match.Groups[3].Value);
            int hour = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;
            int minute = match.Groups[5].Success ? int.Parse(match.Groups[5].Value) : 0;
            int second = match.Groups[6].Success ? int.Parse(match.Groups[6].Value) : 0;

            return _persianCalendar.ToDateTime(year, month, day, hour, minute, second, 0);
        }

        /// <summary>
        /// دریافت تاریخ ایجاد به صورت میلادی (برای محاسبات)
        /// </summary>
        public DateTime GetZamanInsertAsGregorian()
        {
            return ToGregorianDateTime(ZamanInsert);
        }

        /// <summary>
        /// دریافت تاریخ ویرایش به صورت میلادی (برای محاسبات)
        /// </summary>
        public DateTime? GetZamanLastEditAsGregorian()
        {
            if (string.IsNullOrWhiteSpace(ZamanLastEdit))
                return null;
            return ToGregorianDateTime(ZamanLastEdit);
        }

        /// <summary>
        /// دریافت تاریخ و زمان فعلی به شمسی
        /// </summary>
        public static string GetNowPersian()
        {
            return ToPersianDateTime(DateTime.Now);
        }

        /// <summary>
        /// دریافت تاریخ امروز به شمسی
        /// </summary>
        public static string GetTodayPersian()
        {
            return ToPersianDate(DateTime.Now);
        }
    }
}
