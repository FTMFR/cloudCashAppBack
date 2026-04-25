using MediatR;
using System;

namespace BnpCashClaudeApp.Application.MediatR.Commands.Cash
{
    /// <summary>
    /// دستور ایجاد حوزه جدید
    /// </summary>
    public class CreateAzaNoeCommand : IRequest<Guid>
    {
        /// <summary>
        /// شناسه شعبه (FK)
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
        /// شناسه نوع مشتری (FK)
        /// </summary>
        public Guid TafsiliTypePublicId { get; set; }

        /// <summary>
        /// شناسه کاربر ایجادکننده
        /// </summary>
        public long TblUserGrpIdInsert { get; set; }
    }

    /// <summary>
    /// دستور ویرایش حوزه
    /// </summary>
    public class UpdateAzaNoeCommand : IRequest<bool>
    {
        /// <summary>
        /// شناسه عمومی حوزه
        /// </summary>
        public Guid PublicId { get; set; }

        /// <summary>
        /// شناسه شعبه (FK)
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
        /// شناسه نوع مشتری (FK)
        /// </summary>
        public Guid TafsiliTypePublicId { get; set; }

        /// <summary>
        /// وضعیت فعال بودن
        /// </summary>
        public bool IsActive { get; set; } = true;

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
    /// دستور حذف حوزه (Soft Delete)
    /// </summary>
    public class DeleteAzaNoeCommand : IRequest<bool>
    {
        /// <summary>
        /// شناسه عمومی حوزه
        /// </summary>
        public Guid PublicId { get; set; }

        /// <summary>
        /// شناسه کاربر حذف‌کننده
        /// </summary>
        public long TblUserGrpIdLastEdit { get; set; }
    }
}
