using System;
using System.Collections.Generic;
using System.Text;

namespace BnpCashClaudeApp.Application.DTOs
{
    /// <summary>
    /// DTO منو برای نمایش اطلاعات
    /// ============================================
    /// تمام تاریخ‌ها به صورت شمسی (string) هستند
    /// ============================================
    /// </summary>
    public class MenuDto
    {
        /// <summary>
        /// شناسه عمومی (GUID) - برای استفاده در API
        /// </summary>
        public Guid PublicId { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
        /// <summary>
        /// آیکون منو
        /// </summary>
        public string? Icon { get; set; }
        /// <summary>
        /// شناسه عمومی منوی والد (GUID)
        /// </summary>
        public Guid? ParentPublicId { get; set; }

        /// <summary>
        /// آیا این آیتم یک منو است
        /// </summary>
        public bool IsMenu { get; set; }

        /// <summary>
        /// شناسه نرم‌افزار
        /// null = منوی عمومی (راهبری سیستم)
        /// </summary>
        public long? tblSoftwareId { get; set; }

        /// <summary>
        /// نام نرم‌افزار (برای نمایش)
        /// </summary>
        public string? SoftwareName { get; set; }

        /// <summary>
        /// کد نرم‌افزار (برای نمایش)
        /// </summary>
        public string? SoftwareCode { get; set; }

        /// <summary>
        /// تاریخ ایجاد رکورد (شمسی)
        /// </summary>
        public string ZamanInsert { get; set; }

        /// <summary>
        /// زیرمنوها
        /// </summary>
        public List<MenuDto> Children { get; set; } = new List<MenuDto>();
    }
    public class CreateMenuDto
    {
        public string Title { get; set; }
        public string Path { get; set; }
        /// <summary>
        /// شناسه عمومی منوی والد (GUID)
        /// </summary>
        public Guid? ParentPublicId { get; set; }

        /// <summary>
        /// شناسه نرم‌افزار (اختیاری)
        /// null = منوی عمومی (راهبری سیستم)
        /// </summary>
        public long? tblSoftwareId { get; set; }
    }
    public class UpdateMenuDto
    {
        public string Title { get; set; }
        public string Path { get; set; }
        /// <summary>
        /// شناسه عمومی منوی والد (GUID)
        /// </summary>
        public Guid? ParentPublicId { get; set; }

        /// <summary>
        /// شناسه نرم‌افزار (اختیاری)
        /// null = منوی عمومی (راهبری سیستم)
        /// </summary>
        public long? tblSoftwareId { get; set; }
    }
}
