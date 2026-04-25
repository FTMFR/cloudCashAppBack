using System;

namespace BnpCashClaudeApp.Application.DTOs.CashDtos
{
    /// <summary>
    /// DTO برای نمایش اطلاعات حوزه (دسته‌بندی)
    /// </summary>
    public class AzaNoeDto
    {
        /// <summary>
        /// شناسه عمومی
        /// </summary>
        public Guid PublicId { get; set; }

        /// <summary>
        /// شناسه عمومی شعبه
        /// </summary>
        public Guid? ShobePublicId { get; set; }

        /// <summary>
        /// نام شعبه
        /// </summary>
        public string? ShobeTitle { get; set; }

        /// <summary>
        /// عنوان حوزه
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// کد حوزه
        /// </summary>
        public int CodeHoze { get; set; }

        /// <summary>
        /// آیا پیش‌فرض است
        /// </summary>
        public bool PishFarz { get; set; }

        /// <summary>
        /// شناسه عمومی نوع مشتری
        /// </summary>
        public Guid TafsiliTypePublicId { get; set; }

        /// <summary>
        /// عنوان نوع مشتری
        /// </summary>
        public string? TafsiliTypeTitle { get; set; }

        /// <summary>
        /// کد نوع تفصیلی
        /// </summary>
        public int TafsiliTypeCode { get; set; }

        /// <summary>
        /// وضعیت فعال بودن
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// تاریخ ایجاد
        /// </summary>
        public string? ZamanInsert { get; set; }

        /// <summary>
        /// تاریخ آخرین ویرایش
        /// </summary>
        public string? ZamanLastEdit { get; set; }
    }

    /// <summary>
    /// DTO برای ایجاد حوزه جدید
    /// </summary>
    public class CreateAzaNoeDto
    {
        /// <summary>
        /// شناسه عمومی شعبه
        /// </summary>
        public Guid ShobePublicId { get; set; }

        /// <summary>
        /// عنوان حوزه
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// کد حوزه
        /// </summary>
        public int CodeHoze { get; set; }

        /// <summary>
        /// آیا پیش‌فرض است
        /// </summary>
        public bool PishFarz { get; set; } = false;

        /// <summary>
        /// شناسه عمومی نوع مشتری
        /// </summary>
        public Guid TafsiliTypePublicId { get; set; }
    }

    /// <summary>
    /// DTO برای ویرایش حوزه
    /// </summary>
    public class UpdateAzaNoeDto
    {
        /// <summary>
        /// شناسه عمومی شعبه
        /// </summary>
        public Guid ShobePublicId { get; set; }

        /// <summary>
        /// عنوان حوزه
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// کد حوزه
        /// </summary>
        public int CodeHoze { get; set; }

        /// <summary>
        /// آیا پیش‌فرض است
        /// </summary>
        public bool PishFarz { get; set; }

        /// <summary>
        /// شناسه عمومی نوع مشتری
        /// </summary>
        public Guid TafsiliTypePublicId { get; set; }

        /// <summary>
        /// وضعیت فعال بودن
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
