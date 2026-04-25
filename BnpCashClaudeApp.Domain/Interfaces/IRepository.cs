using BnpCashClaudeApp.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Domain.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        /// <summary>
        /// دریافت Entity بر اساس شناسه داخلی (long)
        /// </summary>
        Task<T> GetByIdAsync(long id);
        
        /// <summary>
        /// دریافت Entity بر اساس شناسه عمومی (GUID)
        /// </summary>
        Task<T> GetByPublicIdAsync(Guid publicId);
        
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        
        /// <summary>
        /// حذف Entity بر اساس شناسه داخلی (long)
        /// </summary>
        Task DeleteAsync(long id);
        
        /// <summary>
        /// حذف Entity بر اساس شناسه عمومی (GUID)
        /// </summary>
        Task DeleteByPublicIdAsync(Guid publicId);
        
        /// <summary>
        /// بررسی وجود Entity بر اساس شناسه داخلی (long)
        /// </summary>
        Task<bool> ExistsAsync(long id);
        
        /// <summary>
        /// بررسی وجود Entity بر اساس شناسه عمومی (GUID)
        /// </summary>
        Task<bool> ExistsByPublicIdAsync(Guid publicId);
    }
}
