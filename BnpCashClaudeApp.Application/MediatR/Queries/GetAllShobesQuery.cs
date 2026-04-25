using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Queries
{
    public class GetAllShobesQuery : IRequest<List<ShobeDto>>
    {
    }

    public class GetAllShobesQueryHandler : IRequestHandler<GetAllShobesQuery, List<ShobeDto>>
    {
        private readonly IRepository<tblShobe> _repository;

        public GetAllShobesQueryHandler(IRepository<tblShobe> repository)
        {
            _repository = repository;
        }

        public async Task<List<ShobeDto>> Handle(GetAllShobesQuery request, CancellationToken cancellationToken)
        {
            var shobes = await _repository.GetAllAsync();
            var shobeList = shobes.ToList();
            
            return shobeList.Select(s =>
            {
                // پیدا کردن ParentPublicId و ParentTitle
                Guid? parentPublicId = null;
                string? parentTitle = null;
                if (s.ParentId.HasValue)
                {
                    var parent = shobeList.FirstOrDefault(p => p.Id == s.ParentId.Value);
                    parentPublicId = parent?.PublicId;
                    parentTitle = parent?.Title;
                }

                return new ShobeDto
                {
                    PublicId = s.PublicId,
                    Title = s.Title,
                    ShobeCode = s.ShobeCode,
                    Address = s.Address,
                    Phone = s.Phone,
                    PostalCode = s.PostalCode,
                    ParentPublicId = parentPublicId,
                    ParentTitle = parentTitle,
                    IsActive = s.IsActive,
                    Description = s.Description,
                    DisplayOrder = s.DisplayOrder,
                    ZamanInsert = s.ZamanInsert,
                    ZamanLastEdit = s.ZamanLastEdit
                };
            }).ToList();
        }
    }
}

