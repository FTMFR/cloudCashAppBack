using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// سرویس کنترل دسترسی مبتنی بر Context
    /// ============================================
    /// پیاده‌سازی الزام FDP_ACF.1.4 از استاندارد ISO 15408
    /// 
    /// این سرویس دسترسی را بر اساس پارامترهای Context بررسی می‌کند:
    /// - آدرس IP (Whitelist/Blacklist)
    /// - زمان دسترسی (ساعات کاری، روزهای هفته)
    /// - موقعیت جغرافیایی (کشور)
    /// - نوع دستگاه (موبایل، دسکتاپ، تبلت)
    /// - تعداد نشست‌های همزمان
    /// - ارزیابی ریسک
    /// ============================================
    /// </summary>
    public interface IContextAccessControlService
    {
        /// <summary>
        /// بررسی جامع دسترسی بر اساس تمام پارامترهای Context
        /// </summary>
        /// <param name="context">اطلاعات Context درخواست</param>
        /// <param name="userId">شناسه کاربر (اختیاری)</param>
        /// <param name="ct">Cancellation Token</param>
        /// <returns>نتیجه بررسی دسترسی</returns>
        Task<ContextAccessResult> ValidateAccessAsync(
            AccessContext context,
            long? userId = null,
            CancellationToken ct = default);

        /// <summary>
        /// بررسی دسترسی بر اساس IP
        /// </summary>
        Task<ContextAccessResult> ValidateIpAccessAsync(
            string ipAddress,
            CancellationToken ct = default);

        /// <summary>
        /// بررسی دسترسی بر اساس زمان
        /// </summary>
        Task<ContextAccessResult> ValidateTimeAccessAsync(
            CancellationToken ct = default);

        /// <summary>
        /// بررسی دسترسی بر اساس موقعیت جغرافیایی
        /// </summary>
        Task<ContextAccessResult> ValidateGeoAccessAsync(
            string? countryCode,
            CancellationToken ct = default);

        /// <summary>
        /// بررسی دسترسی بر اساس نوع دستگاه
        /// </summary>
        Task<ContextAccessResult> ValidateDeviceAccessAsync(
            string? userAgent,
            CancellationToken ct = default);

        /// <summary>
        /// بررسی محدودیت نشست همزمان
        /// </summary>
        Task<ContextAccessResult> ValidateConcurrentSessionAsync(
            long userId,
            CancellationToken ct = default);

        /// <summary>
        /// محاسبه امتیاز ریسک دسترسی
        /// </summary>
        Task<RiskAssessmentResult> CalculateRiskScoreAsync(
            AccessContext context,
            long? userId = null,
            CancellationToken ct = default);

        /// <summary>
        /// بررسی آیا IP در محدوده CIDR است
        /// </summary>
        bool IsIpInCidrRange(string ipAddress, string cidrRange);

        /// <summary>
        /// تشخیص نوع دستگاه از User Agent
        /// </summary>
        DeviceType DetectDeviceType(string? userAgent);
    }

    /// <summary>
    /// اطلاعات Context درخواست
    /// </summary>
    public class AccessContext
    {
        /// <summary>
        /// آدرس IP کاربر
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User Agent مرورگر
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// کد کشور (ISO 2 حرفی)
        /// </summary>
        public string? CountryCode { get; set; }

        /// <summary>
        /// شناسه نشست فعلی
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// مسیر درخواست
        /// </summary>
        public string? RequestPath { get; set; }

        /// <summary>
        /// متد HTTP
        /// </summary>
        public string? HttpMethod { get; set; }

        /// <summary>
        /// زمان درخواست
        /// </summary>
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// شناسه Permission مورد نیاز
        /// </summary>
        public string? RequiredPermission { get; set; }

        /// <summary>
        /// اطلاعات اضافی
        /// </summary>
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// نتیجه بررسی دسترسی Context
    /// </summary>
    public class ContextAccessResult
    {
        /// <summary>
        /// آیا دسترسی مجاز است؟
        /// </summary>
        public bool IsAllowed { get; set; }

        /// <summary>
        /// دلیل رد دسترسی (در صورت رد شدن)
        /// </summary>
        public string? DenialReason { get; set; }

        /// <summary>
        /// کد خطا
        /// </summary>
        public ContextAccessDenialCode? DenialCode { get; set; }

        /// <summary>
        /// امتیاز ریسک (0-100)
        /// </summary>
        public int RiskScore { get; set; }

        /// <summary>
        /// آیا نیاز به MFA است؟
        /// </summary>
        public bool RequiresMfa { get; set; }

        /// <summary>
        /// جزئیات بررسی‌های انجام شده
        /// </summary>
        public List<ContextCheckDetail> CheckDetails { get; set; } = new List<ContextCheckDetail>();

        /// <summary>
        /// زمان بررسی
        /// </summary>
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ایجاد نتیجه موفق
        /// </summary>
        public static ContextAccessResult Allowed(int riskScore = 0)
        {
            return new ContextAccessResult
            {
                IsAllowed = true,
                RiskScore = riskScore
            };
        }

        /// <summary>
        /// ایجاد نتیجه رد شده
        /// </summary>
        public static ContextAccessResult Denied(
            ContextAccessDenialCode code,
            string reason,
            int riskScore = 100)
        {
            return new ContextAccessResult
            {
                IsAllowed = false,
                DenialCode = code,
                DenialReason = reason,
                RiskScore = riskScore
            };
        }
    }

    /// <summary>
    /// جزئیات بررسی Context
    /// </summary>
    public class ContextCheckDetail
    {
        /// <summary>
        /// نوع بررسی
        /// </summary>
        public string CheckType { get; set; } = string.Empty;

        /// <summary>
        /// آیا موفق بود؟
        /// </summary>
        public bool Passed { get; set; }

        /// <summary>
        /// پیام
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// مقدار بررسی شده
        /// </summary>
        public string? CheckedValue { get; set; }
    }

    /// <summary>
    /// نتیجه ارزیابی ریسک
    /// </summary>
    public class RiskAssessmentResult
    {
        /// <summary>
        /// امتیاز ریسک کلی (0-100)
        /// </summary>
        public int TotalScore { get; set; }

        /// <summary>
        /// سطح ریسک
        /// </summary>
        public RiskLevel Level { get; set; }

        /// <summary>
        /// فاکتورهای ریسک
        /// </summary>
        public List<RiskFactor> Factors { get; set; } = new List<RiskFactor>();

        /// <summary>
        /// آیا نیاز به MFA است؟
        /// </summary>
        public bool RequiresMfa { get; set; }

        /// <summary>
        /// آیا دسترسی باید رد شود؟
        /// </summary>
        public bool ShouldDeny { get; set; }
    }

    /// <summary>
    /// فاکتور ریسک
    /// </summary>
    public class RiskFactor
    {
        /// <summary>
        /// نام فاکتور
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// امتیاز این فاکتور
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// توضیحات
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// سطح ریسک
    /// </summary>
    public enum RiskLevel
    {
        /// <summary>
        /// ریسک پایین (0-30)
        /// </summary>
        Low = 1,

        /// <summary>
        /// ریسک متوسط (31-60)
        /// </summary>
        Medium = 2,

        /// <summary>
        /// ریسک بالا (61-80)
        /// </summary>
        High = 3,

        /// <summary>
        /// ریسک بحرانی (81-100)
        /// </summary>
        Critical = 4
    }

    /// <summary>
    /// کد دلیل رد دسترسی
    /// </summary>
    public enum ContextAccessDenialCode
    {
        /// <summary>
        /// IP در لیست سیاه است
        /// </summary>
        IpBlacklisted = 1,

        /// <summary>
        /// IP در لیست سفید نیست
        /// </summary>
        IpNotWhitelisted = 2,

        /// <summary>
        /// خارج از ساعات مجاز
        /// </summary>
        OutsideAllowedHours = 3,

        /// <summary>
        /// روز غیرمجاز
        /// </summary>
        DayNotAllowed = 4,

        /// <summary>
        /// کشور غیرمجاز
        /// </summary>
        CountryBlocked = 5,

        /// <summary>
        /// نوع دستگاه غیرمجاز
        /// </summary>
        DeviceTypeBlocked = 6,

        /// <summary>
        /// User Agent ممنوع
        /// </summary>
        UserAgentBlocked = 7,

        /// <summary>
        /// تعداد نشست بیش از حد مجاز
        /// </summary>
        TooManySessions = 8,

        /// <summary>
        /// امتیاز ریسک بالا
        /// </summary>
        HighRiskScore = 9,

        /// <summary>
        /// سایر دلایل
        /// </summary>
        Other = 99
    }

    /// <summary>
    /// نوع دستگاه
    /// </summary>
    public enum DeviceType
    {
        /// <summary>
        /// نامشخص
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// دسکتاپ
        /// </summary>
        Desktop = 1,

        /// <summary>
        /// موبایل
        /// </summary>
        Mobile = 2,

        /// <summary>
        /// تبلت
        /// </summary>
        Tablet = 3,

        /// <summary>
        /// ربات/Crawler
        /// </summary>
        Bot = 4
    }
}

