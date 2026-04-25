using BnpCashClaudeApp.Application.MediatR.Commands;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Application.MediatR.Handlers
{
    public class CreateShobeCommandHandler : IRequestHandler<CreateShobeCommand, Guid>
    {
        private readonly IRepository<tblShobe> _repository;

        public CreateShobeCommandHandler(IRepository<tblShobe> repository)
        {
            _repository = repository;
        }

        public async Task<Guid> Handle(CreateShobeCommand request, CancellationToken cancellationToken)
        {
            // اعتبارسنجی ShobeCode: باید بزرگتر از صفر باشد
            if (request.ShobeCode <= 0)
            {
                throw new ArgumentException("کد شعبه باید بزرگتر از صفر باشد.");
            }

            // اعتبارسنجی یکتایی ShobeCode
            var allShobes = await _repository.GetAllAsync();
            if (allShobes.Any(s => s.ShobeCode == request.ShobeCode))
            {
                throw new ArgumentException($"کد شعبه '{request.ShobeCode}' قبلاً ثبت شده است.");
            }

            // اگر ParentPublicId مشخص شده باشد، باید Parent را پیدا کنیم
            tblShobe? parent = null;
            if (request.ParentPublicId.HasValue)
            {
                parent = await _repository.GetByPublicIdAsync(request.ParentPublicId.Value);
                if (parent == null)
                {
                    throw new ArgumentException("شعبه والد یافت نشد.");
                }
            }

            var shobe = new tblShobe
            {
                Title = request.Title,
                ShobeCode = request.ShobeCode,
                Address = request.Address,
                Phone = request.Phone,
                PostalCode = request.PostalCode,
                ParentId = parent?.Id,
                IsActive = request.IsActive,
                Description = request.Description,
                DisplayOrder = request.DisplayOrder,
                TblUserGrpIdInsert = request.TblUserGrpIdInsert
            };

            // تنظیم تاریخ به شمسی
            shobe.SetZamanInsert(DateTime.Now);

            var result = await _repository.AddAsync(shobe);
            return result.PublicId; // برگرداندن PublicId به جای Id
        }
    }
}

