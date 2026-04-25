using BnpCashClaudeApp.Application.DTOs;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace BnpCashClaudeApp.Application.MediatR.Queries
{
    public class GetAllGrpsQuery : IRequest<List<GrpDto>>
    {
    }
    public class GetAllGroupsQueryHandler : IRequestHandler<GetAllGrpsQuery, List<GrpDto>>
    {
        private readonly IRepository<tblGrp> _repository;
        private readonly IRepository<tblShobe> _shobeRepository;

        public GetAllGroupsQueryHandler(
            IRepository<tblGrp> repository,
            IRepository<tblShobe> shobeRepository)
        {
            _repository = repository;
            _shobeRepository = shobeRepository;
        }

        public async Task<List<GrpDto>> Handle(GetAllGrpsQuery request, CancellationToken cancellationToken)
        {
            var groupList = (await _repository.GetAllAsync()).ToList();

            // دریافت لیست شعب برای جلوگیری از کوئری‌های متعدد
            var shobeList = (await _shobeRepository.GetAllAsync()).ToList();
            var RootGrps = groupList.Where(g => g.ParentId == null).ToList();
            return RootGrps.Select(root => BuildGroupTree(root, groupList, shobeList)).ToList();
        }

        public GrpDto BuildGroupTree(tblGrp entity, List<tblGrp> allGrps, List<tblShobe> allShobes)
        {
            var parent = entity.ParentId.HasValue ? allGrps.FirstOrDefault(p => p.Id == entity.ParentId.Value) : null;
            var shobe = entity.tblShobeId.HasValue ? allShobes.FirstOrDefault(s => s.Id == entity.tblShobeId.Value) : null;
            var children = allGrps.Where(g => g.ParentId == entity.Id).ToList();
            var childDto = children.Select(child => BuildGroupTree(child, allGrps, allShobes)).ToList();


            return new GrpDto
            {
                PublicId = entity.PublicId,
                Title = entity.Title,
                GrpCode = entity.GrpCode,
                ZamanInsert = entity.ZamanInsert,
                ZamanLastEdit = entity.ZamanLastEdit,
                ParentPublicId = entity?.PublicId,
                Description = entity?.Description,
                tblShobeId = entity?.tblShobeId,
                ShobePublicId = shobe?.PublicId,
                ShobeTitle = shobe?.Title,
                Children = childDto
            };
        }
    }
}
