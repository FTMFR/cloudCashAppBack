using MediatR;
using System;

namespace BnpCashClaudeApp.Application.MediatR.Commands.Cash
{
    /// <summary>
    /// دستور ایجاد سرفصل جدید
    /// </summary>
    public class CreateSarfaslCommand : IRequest<Guid>
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
        /// شناسه Combo وضعیت زیرگروه
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
        /// شناسه Combo کنترل عملیات
        /// </summary>
        public long? TblComboIdControlAmaliat { get; set; }

        /// <summary>
        /// عدم نمایش در تراز
        /// </summary>
        public bool NotShowInTaraz { get; set; } = false;

        /// <summary>
        /// شناسه کاربر ایجادکننده
        /// </summary>
        public long TblUserGrpIdInsert { get; set; }
    }

    /// <summary>
    /// دستور ویرایش سرفصل
    /// </summary>
    public class UpdateSarfaslCommand : IRequest<bool>
    {
        /// <summary>
        /// شناسه عمومی سرفصل
        /// </summary>
        public Guid PublicId { get; set; }

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
        /// شناسه Combo وضعیت زیرگروه
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
        /// شناسه Combo کنترل عملیات
        /// </summary>
        public long? TblComboIdControlAmaliat { get; set; }

        /// <summary>
        /// عدم نمایش در تراز
        /// </summary>
        public bool NotShowInTaraz { get; set; }

        /// <summary>
        /// شناسه کاربر ویرایش‌کننده
        /// </summary>
        public long TblUserGrpIdLastEdit { get; set; }

        /// <summary>
        /// شناسه کاربر برای ثبت در Audit Log (اختیاری)
        /// </summary>
        public long? AuditUserId { get; set; }
    }

    /// <summary>
    /// دستور حذف سرفصل
    /// </summary>
    public class DeleteSarfaslCommand : IRequest<bool>
    {
        /// <summary>
        /// شناسه عمومی سرفصل
        /// </summary>
        public Guid PublicId { get; set; }

        /// <summary>
        /// شناسه کاربر حذف‌کننده
        /// </summary>
        public long TblUserGrpIdLastEdit { get; set; }
    }
}
