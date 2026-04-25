using System;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Application.DTOs.CashDtos
{
    /// <summary>
    /// DTO برای نمایش اطلاعات سرفصل
    /// </summary>
    public class SarfaslDto
    {
        /// <summary>
        /// شناسه عمومی
        /// </summary>
        public Guid PublicId { get; set; }

        /// <summary>
        /// شناسه شعبه
        /// </summary>
        public long TblShobeId { get; set; }

        /// <summary>
        /// شناسه عمومی والد
        /// </summary>
        public Guid? ParentPublicId { get; set; }

        /// <summary>
        /// عنوان والد
        /// </summary>
        public string? ParentTitle { get; set; }

        /// <summary>
        /// کد سرفصل والد
        /// </summary>
        public string? ParentCodeSarfasl { get; set; }

        /// <summary>
        /// شناسه عمومی نوع سرفصل
        /// </summary>
        public Guid? SarfaslTypePublicId { get; set; }

        /// <summary>
        /// عنوان نوع سرفصل
        /// </summary>
        public string? SarfaslTypeTitle { get; set; }

        /// <summary>
        /// شناسه عمومی پروتکل
        /// </summary>
        public Guid? SarfaslProtocolPublicId { get; set; }

        /// <summary>
        /// عنوان پروتکل
        /// </summary>
        public string? SarfaslProtocolTitle { get; set; }

        /// <summary>
        /// کد سرفصل
        /// </summary>
        public string CodeSarfasl { get; set; } = string.Empty;

        /// <summary>
        /// عنوان سرفصل
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// توضیحات
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// دارای جزء تفصیلی
        /// </summary>
        public bool WithJoze { get; set; }

        /// <summary>
        /// شناسه Combo وضعیت زیرگروه (GrpCode=15)
        /// </summary>
        public long? TblComboIdVazeiatZirGrp { get; set; }

        /// <summary>
        /// عنوان وضعیت زیرگروه
        /// </summary>
        public string? VazeiatZirGrpTitle { get; set; }

        /// <summary>
        /// تعداد ارقام زیرگروه
        /// </summary>
        public int? TedadArghamZirGrp { get; set; }

        /// <summary>
        /// میزان اعتبار بدهکار
        /// </summary>
        public decimal MizanEtebarBedehkar { get; set; }

        /// <summary>
        /// میزان اعتبار بستانکار
        /// </summary>
        public decimal MizanEtebarBestankar { get; set; }

        /// <summary>
        /// شناسه Combo کنترل عملیات (GrpCode=16)
        /// </summary>
        public long? TblComboIdControlAmaliat { get; set; }

        /// <summary>
        /// عنوان کنترل عملیات
        /// </summary>
        public string? ControlAmaliatTitle { get; set; }

        /// <summary>
        /// عدم نمایش در تراز
        /// </summary>
        public bool NotShowInTaraz { get; set; }

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
        public List<SarfaslDto>? Children { get; set; }

        /// <summary>
        /// تعداد زیرمجموعه‌ها
        /// </summary>
        public int ChildrenCount { get; set; }

        /// <summary>
        /// سطح در درخت (برای نمایش)
        /// </summary>
        public int Level { get; set; }
    }

    /// <summary>
    /// DTO برای ایجاد سرفصل جدید
    /// </summary>
    public class CreateSarfaslDto
    {
        /// <summary>
        /// شناسه شعبه (پیش‌فرض 1)
        /// </summary>
        public long TblShobeId { get; set; } = 1;

        /// <summary>
        /// شناسه عمومی والد (اختیاری)
        /// </summary>
        public Guid? ParentPublicId { get; set; }

        /// <summary>
        /// شناسه عمومی نوع سرفصل (اختیاری)
        /// </summary>
        public Guid? SarfaslTypePublicId { get; set; }

        /// <summary>
        /// شناسه عمومی پروتکل (اختیاری)
        /// </summary>
        public Guid? SarfaslProtocolPublicId { get; set; }

        /// <summary>
        /// کد سرفصل
        /// </summary>
        public string CodeSarfasl { get; set; } = string.Empty;

        /// <summary>
        /// عنوان سرفصل
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// توضیحات
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// دارای جزء تفصیلی
        /// </summary>
        public bool WithJoze { get; set; } = false;

        /// <summary>
        /// شناسه Combo وضعیت زیرگروه (GrpCode=15)
        /// </summary>
        public long? TblComboIdVazeiatZirGrp { get; set; }

        /// <summary>
        /// تعداد ارقام زیرگروه
        /// </summary>
        public int? TedadArghamZirGrp { get; set; }

        /// <summary>
        /// میزان اعتبار بدهکار
        /// </summary>
        public decimal MizanEtebarBedehkar { get; set; } = 0;

        /// <summary>
        /// میزان اعتبار بستانکار
        /// </summary>
        public decimal MizanEtebarBestankar { get; set; } = 0;

        /// <summary>
        /// شناسه Combo کنترل عملیات (GrpCode=16)
        /// </summary>
        public long? TblComboIdControlAmaliat { get; set; }

        /// <summary>
        /// عدم نمایش در تراز
        /// </summary>
        public bool NotShowInTaraz { get; set; } = false;
    }

    /// <summary>
    /// DTO برای ویرایش سرفصل
    /// </summary>
    public class UpdateSarfaslDto
    {
        /// <summary>
        /// شناسه شعبه
        /// </summary>
        public long TblShobeId { get; set; } = 1;

        /// <summary>
        /// شناسه عمومی والد (اختیاری)
        /// </summary>
        public Guid? ParentPublicId { get; set; }

        /// <summary>
        /// شناسه عمومی نوع سرفصل (اختیاری)
        /// </summary>
        public Guid? SarfaslTypePublicId { get; set; }

        /// <summary>
        /// شناسه عمومی پروتکل (اختیاری)
        /// </summary>
        public Guid? SarfaslProtocolPublicId { get; set; }

        /// <summary>
        /// کد سرفصل
        /// </summary>
        public string CodeSarfasl { get; set; } = string.Empty;

        /// <summary>
        /// عنوان سرفصل
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// توضیحات
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// دارای جزء تفصیلی
        /// </summary>
        public bool WithJoze { get; set; }

        /// <summary>
        /// شناسه Combo وضعیت زیرگروه (GrpCode=15)
        /// </summary>
        public long? TblComboIdVazeiatZirGrp { get; set; }

        /// <summary>
        /// تعداد ارقام زیرگروه
        /// </summary>
        public int? TedadArghamZirGrp { get; set; }

        /// <summary>
        /// میزان اعتبار بدهکار
        /// </summary>
        public decimal MizanEtebarBedehkar { get; set; }

        /// <summary>
        /// میزان اعتبار بستانکار
        /// </summary>
        public decimal MizanEtebarBestankar { get; set; }

        /// <summary>
        /// شناسه Combo کنترل عملیات (GrpCode=16)
        /// </summary>
        public long? TblComboIdControlAmaliat { get; set; }

        /// <summary>
        /// عدم نمایش در تراز
        /// </summary>
        public bool NotShowInTaraz { get; set; }
    }
}
