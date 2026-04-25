using System;

namespace BnpCashClaudeApp.Domain.Enums
{
    /// <summary>
    /// نوع فایل پیوست
    /// </summary>
    public enum AttachmentType
    {
        /// <summary>
        /// سند (PDF, Word, Excel, ...)
        /// </summary>
        Document = 0,

        /// <summary>
        /// تصویر (JPG, PNG, GIF, ...)
        /// </summary>
        Image = 1,

        /// <summary>
        /// امضای دیجیتال
        /// </summary>
        Signature = 2,

        /// <summary>
        /// گزارش
        /// </summary>
        Report = 3,

        /// <summary>
        /// خروجی سیستم
        /// </summary>
        Export = 4,

        /// <summary>
        /// فایل پشتیبان
        /// </summary>
        Backup = 5,

        /// <summary>
        /// تصویر پروفایل
        /// </summary>
        ProfileImage = 6,

        /// <summary>
        /// لوگو
        /// </summary>
        Logo = 7,

        /// <summary>
        /// سایر
        /// </summary>
        Other = 99,


    }

    /// <summary>
    /// وضعیت فایل پیوست
    /// </summary>
    public enum AttachmentStatus
    {
        /// <summary>
        /// در انتظار تایید/پردازش
        /// </summary>
        Pending = 0,

        /// <summary>
        /// فعال و قابل دسترسی
        /// </summary>
        Active = 1,

        /// <summary>
        /// آرشیو شده
        /// </summary>
        Archived = 2,

        /// <summary>
        /// حذف شده (Soft Delete)
        /// </summary>
        Deleted = 3,

        /// <summary>
        /// قرنطینه شده (مشکوک به ویروس)
        /// </summary>
        Quarantined = 4
    }

    /// <summary>
    /// نوع ذخیره‌سازی فایل
    /// </summary>
    public enum StorageType
    {
        /// <summary>
        /// سیستم فایل محلی
        /// </summary>
        FileSystem = 0,

        /// <summary>
        /// دیتابیس (BLOB)
        /// </summary>
        Database = 1,

        /// <summary>
        /// Azure Blob Storage
        /// </summary>
        AzureBlob = 2,

        /// <summary>
        /// Amazon S3
        /// </summary>
        AmazonS3 = 3
    }

    /// <summary>
    /// سطح حساسیت فایل
    /// پیاده‌سازی FDP_ITC.2.1 / FDP_ETC.2.1
    /// </summary>
    public enum FileSensitivityLevel
    {
        /// <summary>
        /// عمومی - قابل دسترسی برای همه
        /// </summary>
        Public = 0,

        /// <summary>
        /// داخلی - فقط کاربران داخلی
        /// </summary>
        Internal = 1,

        /// <summary>
        /// محرمانه - نیاز به دسترسی خاص
        /// </summary>
        Confidential = 2,

        /// <summary>
        /// سری - بالاترین سطح حفاظت
        /// </summary>
        Secret = 3
    }

    /// <summary>
    /// نوع دسترسی به فایل (برای لاگ)
    /// </summary>
    public enum AttachmentAccessType
    {
        /// <summary>
        /// مشاهده/پیش‌نمایش
        /// </summary>
        View = 0,

        /// <summary>
        /// دانلود
        /// </summary>
        Download = 1,

        /// <summary>
        /// چاپ
        /// </summary>
        Print = 2,

        /// <summary>
        /// اشتراک‌گذاری
        /// </summary>
        Share = 3,

        /// <summary>
        /// خروجی گرفتن
        /// </summary>
        Export = 4,

        /// <summary>
        /// حذف
        /// </summary>
        Delete = 5,

        /// <summary>
        /// ویرایش
        /// </summary>
        Edit = 6,

        /// <summary>
        /// آپلود
        /// </summary>
        Upload = 7
    }
}
