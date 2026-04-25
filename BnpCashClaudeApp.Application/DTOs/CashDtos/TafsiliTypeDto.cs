using System;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Application.DTOs.CashDtos
{
    /// <summary>
    /// DTO برای نمایش اطلاعات نوع مشتری (نوع تفصیلی)
    /// </summary>
    public class TafsiliTypeDto
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
        /// شناسه عمومی والد
        /// </summary>
        public Guid? ParentPublicId { get; set; }

        /// <summary>
        /// عنوان والد
        /// </summary>
        public string? ParentTitle { get; set; }

        /// <summary>
        /// عنوان نوع مشتری
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// کد نوع تفصیلی (خودکار)
        /// </summary>
        public int CodeTafsiliType { get; set; }

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

        /// <summary>
        /// زیرمجموعه‌ها (برای نمایش درختی)
        /// </summary>
        public List<TafsiliTypeDto>? Children { get; set; }

        /// <summary>
        /// تعداد حوزه‌های مرتبط
        /// </summary>
        public int AzaNoeCount { get; set; }
    }

    /// <summary>
    /// DTO برای ایجاد نوع مشتری جدید
    /// </summary>
    public class CreateTafsiliTypeDto
    {
        /// <summary>
        /// شناسه عمومی شعبه
        /// </summary>
        public Guid ShobePublicId { get; set; }

        /// <summary>
        /// شناسه عمومی والد (اختیاری)
        /// </summary>
        public Guid? ParentPublicId { get; set; }

        /// <summary>
        /// عنوان نوع مشتری
        /// </summary>
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO برای ویرایش نوع مشتری
    /// </summary>
    public class UpdateTafsiliTypeDto
    {
        /// <summary>
        /// شناسه عمومی شعبه
        /// </summary>
        public Guid ShobePublicId { get; set; }

        /// <summary>
        /// شناسه عمومی والد (اختیاری)
        /// </summary>
        public Guid? ParentPublicId { get; set; }

        /// <summary>
        /// عنوان نوع مشتری
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// وضعیت فعال بودن
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
