using System;

namespace BnpCashClaudeApp.Application.DTOs
{
    /// <summary>
    /// DTO شعبه برای نمایش اطلاعات
    /// ============================================
    /// تمام تاریخ‌ها به صورت شمسی (string) هستند
    /// ============================================
    /// </summary>
    public class ShobeDto
    {
        /// <summary>
        /// شناسه عمومی (GUID) - برای استفاده در API
        /// </summary>
        public Guid PublicId { get; set; }

        /// <summary>
        /// نام شعبه
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// کد شعبه
        /// </summary>
        public int ShobeCode { get; set; }

        /// <summary>
        /// آدرس شعبه
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// شماره تلفن شعبه
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// کد پستی
        /// </summary>
        public string? PostalCode { get; set; }

        /// <summary>
        /// شناسه شعبه والد (PublicId)
        /// </summary>
        public Guid? ParentPublicId { get; set; }

        /// <summary>
        /// نام شعبه والد
        /// </summary>
        public string? ParentTitle { get; set; }

        /// <summary>
        /// آیا شعبه فعال است
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// توضیحات
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// ترتیب نمایش
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// تاریخ ایجاد رکورد (شمسی)
        /// </summary>
        public string ZamanInsert { get; set; }

        /// <summary>
        /// تاریخ آخرین ویرایش (شمسی)
        /// </summary>
        public string? ZamanLastEdit { get; set; }
    }

    /// <summary>
    /// DTO برای ایجاد شعبه جدید
    /// </summary>
    public class CreateShobeDto
    {
        public string Title { get; set; }
        public int ShobeCode { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? PostalCode { get; set; }
        public Guid? ParentPublicId { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// DTO برای ویرایش شعبه
    /// </summary>
    public class UpdateShobeDto
    {
        public string Title { get; set; }
        public int ShobeCode { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? PostalCode { get; set; }
        public Guid? ParentPublicId { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
    }
}

