using BnpCashClaudeApp.Application.DTOs.CashDtos;
using MediatR;
using System;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Application.MediatR.Queries.Cash
{
    /// <summary>
    /// کوئری دریافت تمام حوزه‌ها
    /// </summary>
    public class GetAllAzaNoesQuery : IRequest<List<AzaNoeDto>>
    {
        /// <summary>
        /// فیلتر بر اساس شعبه (اختیاری)
        /// </summary>
        public Guid? ShobePublicId { get; set; }

        /// <summary>
        /// فیلتر بر اساس نوع مشتری (اختیاری)
        /// </summary>
        public Guid? TafsiliTypePublicId { get; set; }

        /// <summary>
        /// آیا فقط موارد فعال برگردانده شود
        /// </summary>
        public bool OnlyActive { get; set; } = true;
    }

    /// <summary>
    /// کوئری دریافت حوزه با شناسه
    /// </summary>
    public class GetAzaNoeByIdQuery : IRequest<AzaNoeDto?>
    {
        /// <summary>
        /// شناسه عمومی حوزه
        /// </summary>
        public Guid PublicId { get; set; }
    }

    /// <summary>
    /// کوئری دریافت حوزه‌های یک نوع مشتری خاص
    /// </summary>
    public class GetAzaNoesByTafsiliTypeQuery : IRequest<List<AzaNoeDto>>
    {
        /// <summary>
        /// شناسه عمومی نوع مشتری
        /// </summary>
        public Guid TafsiliTypePublicId { get; set; }

        /// <summary>
        /// آیا فقط موارد فعال برگردانده شود
        /// </summary>
        public bool OnlyActive { get; set; } = true;
    }
}
