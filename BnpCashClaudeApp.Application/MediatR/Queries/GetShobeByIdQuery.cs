using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Queries
{
    public class GetShobeByIdQuery : IRequest<ShobeDto>
    {
        public Guid PublicId { get; set; }
    }

    public class GetShobeByIdQueryHandler : IRequestHandler<GetShobeByIdQuery, ShobeDto>
    {
        private readonly IRepository<tblShobe> _repository;

        public GetShobeByIdQueryHandler(IRepository<tblShobe> repository)
        {
            _repository = repository;
        }

        public async Task<ShobeDto> Handle(GetShobeByIdQuery request, CancellationToken cancellationToken)
        {
            var shobe = await _repository.GetByPublicIdAsync(request.PublicId);
            if (shobe == null) return null;

            // اگر ParentId داریم، باید ParentPublicId و ParentTitle را پیدا کنیم
            Guid? parentPublicId = null;
            string? parentTitle = null;
            if (shobe.ParentId.HasValue)
            {
                var parentShobe = await _repository.GetByIdAsync(shobe.ParentId.Value);
                parentPublicId = parentShobe?.PublicId;
                parentTitle = parentShobe?.Title;
            }

            return new ShobeDto
            {
                PublicId = shobe.PublicId,
                Title = shobe.Title,
                ShobeCode = shobe.ShobeCode,
                Address = shobe.Address,
                Phone = shobe.Phone,
                PostalCode = shobe.PostalCode,
                ParentPublicId = parentPublicId,
                ParentTitle = parentTitle,
                IsActive = shobe.IsActive,
                Description = shobe.Description,
                DisplayOrder = shobe.DisplayOrder,
                ZamanInsert = shobe.ZamanInsert,
                ZamanLastEdit = shobe.ZamanLastEdit
            };
        }
    }
}

