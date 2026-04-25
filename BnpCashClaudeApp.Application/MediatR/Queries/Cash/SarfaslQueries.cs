using BnpCashClaudeApp.Application.DTOs.CashDtos;
using MediatR;
using System;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Application.MediatR.Queries.Cash
{
    /// <summary>
    /// کوئری دریافت تمام سرفصل‌ها
    /// </summary>
    public class GetAllSarfaslsQuery : IRequest<List<SarfaslDto>>
    {
        /// <summary>
        /// فیلتر بر اساس شعبه (اختیاری)
        /// </summary>
        public long? TblShobeId { get; set; }

        /// <summary>
        /// فیلتر بر اساس پروتکل (اختیاری)
        /// </summary>
        public Guid? SarfaslProtocolPublicId { get; set; }

        /// <summary>
        /// فیلتر بر اساس نوع سرفصل (اختیاری)
        /// </summary>
        public Guid? SarfaslTypePublicId { get; set; }

        /// <summary>
        /// فقط سرفصل‌های با جزء تفصیلی
        /// </summary>
        public bool? OnlyWithJoze { get; set; }
    }

    /// <summary>
    /// کوئری دریافت سرفصل با شناسه
    /// </summary>
    public class GetSarfaslByIdQuery : IRequest<SarfaslDto?>
    {
        /// <summary>
        /// شناسه عمومی سرفصل
        /// </summary>
        public Guid PublicId { get; set; }
    }

    /// <summary>
    /// کوئری دریافت سرفصل‌ها به صورت درختی
    /// </summary>
    public class GetSarfaslTreeQuery : IRequest<List<SarfaslDto>>
    {
        /// <summary>
        /// فیلتر بر اساس شعبه (اختیاری)
        /// </summary>
        public long? TblShobeId { get; set; }

        /// <summary>
        /// فیلتر بر اساس پروتکل (اختیاری)
        /// </summary>
        public Guid? SarfaslProtocolPublicId { get; set; }
    }

    /// <summary>
    /// کوئری دریافت زیرمجموعه‌های یک سرفصل
    /// </summary>
    public class GetSarfaslChildrenQuery : IRequest<List<SarfaslDto>>
    {
        /// <summary>
        /// شناسه عمومی سرفصل والد
        /// </summary>
        public Guid ParentPublicId { get; set; }
    }

    /// <summary>
    /// کوئری جستجوی سرفصل بر اساس کد
    /// </summary>
    public class GetSarfaslByCodeQuery : IRequest<SarfaslDto?>
    {
        /// <summary>
        /// کد سرفصل
        /// </summary>
        public string CodeSarfasl { get; set; } = string.Empty;

        /// <summary>
        /// شناسه شعبه (اختیاری)
        /// </summary>
        public long? TblShobeId { get; set; }
    }
}
