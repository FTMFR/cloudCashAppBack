using BnpCashClaudeApp.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;

namespace BnpCashClaudeApp.Application.MediatR.Queries
{
    public class GetAllShobeSettingsQuery : IRequest<List<ShobeSettingDto>>
    {
        /// <summary>
        /// فیلتر بر اساس شناسه شعبه (PublicId) - nullable برای دریافت همه تنظیمات
        /// </summary>
        public Guid? ShobePublicId { get; set; }
    }
}
