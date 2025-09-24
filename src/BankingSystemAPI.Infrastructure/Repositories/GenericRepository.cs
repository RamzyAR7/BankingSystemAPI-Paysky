using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using BankingSystemAPI.Infrastructure.UnitOfWork;

namespace BankingSystemAPI.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected ApplicationDbContext _context;
        protected DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync(bool asNoTracking = true)
        {
            IQueryable<T> query = _dbSet;
            if (asNoTracking) query = query.AsNoTracking();
            return await query.ToListAsync();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<T> FindAsync(Expression<Func<T, bool>> predicate, string[] Includes = null, bool asNoTracking = true)
        {
            IQueryable<T> query = _dbSet;
            if (asNoTracking) query = query.AsNoTracking();

            if (Includes != null)
            {
                foreach (var include in Includes)
                {
                    query = query.Include(include);
                }
            }

            if (predicate != null)
                return await query.FirstOrDefaultAsync(predicate);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, string[] Includes = null, bool asNoTracking = true)
        {
            IQueryable<T> query = _dbSet;

            if (asNoTracking) query = query.AsNoTracking();

            if (Includes != null)
            {
                foreach (var include in Includes)
                {
                    query = query.Include(include);
                }
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, int take = 0, int skip = 0, Expression<Func<T, object>> orderBy = null, string orderByDirection = "ASC", string[] Includes = null, bool asNoTracking = true)
        {
            IQueryable<T> query = _dbSet;

            if (asNoTracking) query = query.AsNoTracking();

            if (Includes != null)
            {
                foreach (var include in Includes)
                {
                    query = query.Include(include);
                }
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (orderBy != null)
            {
                if (orderByDirection?.ToUpper() == "ASC")
                {
                    query = query.OrderBy(orderBy);
                }
                else
                {
                    query = query.OrderByDescending(orderBy);
                }
            }

            if (skip > 0)
            {
                query = query.Skip(skip);
            }

            if (take > 0)
            {
                query = query.Take(take);
            }

            return await query.ToListAsync();
        }

        private bool ShouldDeferSave()
        {
            return BankingSystemAPI.Infrastructure.UnitOfWork.UnitOfWork.TransactionActive?.Value == true;
        }

        public async Task<T> AddAsync(T Entity)
        {
            await _dbSet.AddAsync(Entity);
            if (!ShouldDeferSave())
                await _context.SaveChangesAsync();
            return Entity;
        }

        public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> Entities)
        {
            await _dbSet.AddRangeAsync(Entities);
            if (!ShouldDeferSave())
                await _context.SaveChangesAsync();
            return Entities;
        }
        public async Task<T> UpdateAsync(T Entity)
        {
            _dbSet.Update(Entity);
            if (!ShouldDeferSave())
                await _context.SaveChangesAsync();
            return Entity;
        }
        public async Task DeleteAsync(T Entity)
        {
            _dbSet.Remove(Entity);
            if (!ShouldDeferSave())
                await _context.SaveChangesAsync();
        }
        public async Task DeleteRangeAsync(IEnumerable<T> Entities)
        {
            _dbSet.RemoveRange(Entities);
            if (!ShouldDeferSave())
                await _context.SaveChangesAsync();
        }
        public async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync();
        }
        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                return await _dbSet.CountAsync();

            return await _dbSet.CountAsync(predicate);
        }

    }
}
