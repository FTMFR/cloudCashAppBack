using MediatR;
using System;

namespace BnpCashClaudeApp.Application.MediatR.Commands.Cash
{
    /// <summary>
    /// دستور ایجاد نوع مشتری جدید
    /// </summary>
    public class CreateTafsiliTypeCommand : IRequest<Guid>
    {
        /// <summary>
        /// شناسه شعبه (FK)
        /// </summary>
        public Guid ShobePublicId { get; set; }

        /// <summary>
        /// شناسه والد (اختیاری)
        /// </summary>
        public Guid? ParentPublicId { get; set; }

        /// <summary>
        /// عنوان نوع مشتری
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// شناسه کاربر ایجادکننده
        /// </summary>
        public long TblUserGrpIdInsert { get; set; }
    }

    /// <summary>
    /// دستور ویرایش نوع مشتری
    /// </summary>
    public class UpdateTafsiliTypeCommand : IRequest<bool>
    {
        /// <summary>
        /// شناسه عمومی نوع مشتری
        /// </summary>
        public Guid PublicId { get; set; }

        /// <summary>
        /// شناسه شعبه (FK)
        /// </summary>
        public Guid ShobePublicId { get; set; }

        /// <summary>
        /// شناسه والد (اختیاری)
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
    /// دستور حذف نوع مشتری (Soft Delete)
    /// </summary>
    public class DeleteTafsiliTypeCommand : IRequest<bool>
    {
        /// <summary>
        /// شناسه عمومی نوع مشتری
        /// </summary>
        public Guid PublicId { get; set; }

        /// <summary>
        /// شناسه کاربر حذف‌کننده
        /// </summary>
        public long TblUserGrpIdLastEdit { get; set; }
    }
}
