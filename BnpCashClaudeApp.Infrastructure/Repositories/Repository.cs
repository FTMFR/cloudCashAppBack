using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.NavigationSubsystem;
using BnpCashClaudeApp.Domain.Interfaces;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly NavigationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(NavigationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T> GetByIdAsync(long id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<T> GetByPublicIdAsync(Guid publicId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(e => e.PublicId == publicId);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            // اطمینان از اینکه PublicId تنظیم شده است
            if (entity.PublicId == Guid.Empty)
            {
                entity.PublicId = Guid.NewGuid();
            }
            
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteByPublicIdAsync(Guid publicId)
        {
            var entity = await GetByPublicIdAsync(publicId);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _dbSet.AnyAsync(e => e.Id == id);
        }

        public async Task<bool> ExistsByPublicIdAsync(Guid publicId)
        {
            return await _dbSet.AnyAsync(e => e.PublicId == publicId);
        }
    }
}
