using BnpCashClaudeApp.Application.DTOs.CashDtos;
using MediatR;
using System;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Application.MediatR.Queries.Cash
{
    /// <summary>
    /// کوئری دریافت تمام انواع مشتری
    /// </summary>
    public class GetAllTafsiliTypesQuery : IRequest<List<TafsiliTypeDto>>
    {
        /// <summary>
        /// فیلتر بر اساس شعبه (اختیاری)
        /// </summary>
        public Guid? ShobePublicId { get; set; }

        /// <summary>
        /// آیا فقط موارد فعال برگردانده شود
        /// </summary>
        public bool OnlyActive { get; set; } = true;
    }

    /// <summary>
    /// کوئری دریافت نوع مشتری با شناسه
    /// </summary>
    public class GetTafsiliTypeByIdQuery : IRequest<TafsiliTypeDto?>
    {
        /// <summary>
        /// شناسه عمومی نوع مشتری
        /// </summary>
        public Guid PublicId { get; set; }
    }

    /// <summary>
    /// کوئری دریافت انواع مشتری به صورت درختی
    /// </summary>
    public class GetTafsiliTypeTreeQuery : IRequest<List<TafsiliTypeDto>>
    {
        /// <summary>
        /// فیلتر بر اساس شعبه (اختیاری)
        /// </summary>
        public Guid? ShobePublicId { get; set; }

        /// <summary>
        /// آیا فقط موارد فعال برگردانده شود
        /// </summary>
        public bool OnlyActive { get; set; } = true;
    }
}
