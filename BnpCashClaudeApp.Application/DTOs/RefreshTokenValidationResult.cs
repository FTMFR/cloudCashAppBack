namespace BnpCashClaudeApp.Application.DTOs
{
    /// <summary>
    /// وضعیت اعتبارسنجی Refresh Token
    /// </summary>
    public enum RefreshTokenValidationStatus
    {
        /// <summary>توکن معتبر و قابل استفاده</summary>
        Valid,

        /// <summary>توکن یافت نشد</summary>
        NotFound,

        /// <summary>توکن منقضی یا باطل شده</summary>
        Inactive,

        /// <summary>
        /// استفاده مجدد در بازه Grace Period (مثلاً تب‌های همزمان)
        /// هیچ توکنی باطل نمی‌شود - فقط این درخواست رد می‌شود
        /// </summary>
        RaceCondition,

        /// <summary>
        /// استفاده مجدد خارج از Grace Period - احتمال حمله
        /// تمام توکن‌های کاربر باطل می‌شود
        /// </summary>
        SecurityAlert
    }

    /// <summary>
    /// نتیجه اعتبارسنجی Refresh Token
    /// به جای بازگشت null/userId ساده، وضعیت دقیق را مشخص می‌کند
    /// تا endpoint بتواند تصمیم صحیح بگیرد (مثلاً حذف یا عدم حذف Cookie)
    /// </summary>
    public class RefreshTokenValidationResult
    {
        public long? UserId { get; set; }
        public RefreshTokenValidationStatus Status { get; set; }
        public string? Message { get; set; }

        public bool IsSuccess => Status == RefreshTokenValidationStatus.Valid;

        public static RefreshTokenValidationResult Success(long userId) => new RefreshTokenValidationResult
        {
            UserId = userId,
            Status = RefreshTokenValidationStatus.Valid
        };

        public static RefreshTokenValidationResult Failed(RefreshTokenValidationStatus status, string? message = null) => new RefreshTokenValidationResult
        {
            Status = status,
            Message = message
        };
    }
}
