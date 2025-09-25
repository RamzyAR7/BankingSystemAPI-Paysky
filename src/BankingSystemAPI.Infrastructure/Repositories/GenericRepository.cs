using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BankingSystemAPI.Infrastructure.Repositories
{
    public class GenericRepository<T, TKey> : IGenericRepository<T, TKey> where T : class
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

        public async Task<T> GetByIdAsync(TKey id)
        {
            return await _dbSet.FindAsync(id);
        }

        // Helper to apply includes (string paths, expression includes, or include-builder)
        private IQueryable<T> ApplyIncludes(IQueryable<T> query, string[] includes = null, Expression<Func<T, object>>[] includeExpressions = null, Func<IQueryable<T>, IQueryable<T>> includeBuilder = null)
        {
            if (includeBuilder != null)
            {
                return includeBuilder(query);
            }

            if (includeExpressions != null)
            {
                foreach (var include in includeExpressions)
                {
                    query = query.Include(include);
                }
                return query;
            }

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return query;
        }

        // Centralized builder for queries with common options
        private IQueryable<T> BuildQuery(
            Expression<Func<T, bool>> predicate = null,
            Expression<Func<T, object>> orderBy = null,
            string orderByDirection = "ASC",
            int skip = 0,
            int take = 0,
            bool asNoTracking = true,
            string[] includes = null,
            Expression<Func<T, object>>[] includeExpressions = null,
            Func<IQueryable<T>, IQueryable<T>> includeBuilder = null)
        {
            IQueryable<T> query = _dbSet;
            if (asNoTracking) query = query.AsNoTracking();

            // Apply includes using unified helper
            query = ApplyIncludes(query, includes, includeExpressions, includeBuilder);

            if (predicate != null)
                query = query.Where(predicate);

            if (orderBy != null)
            {
                if (orderByDirection?.ToUpper() == "ASC")
                    query = query.OrderBy(orderBy);
                else
                    query = query.OrderByDescending(orderBy);
            }

            if (skip > 0) query = query.Skip(skip);
            if (take > 0) query = query.Take(take);

            return query;
        }

        // Existing string-based include overloads (kept for compatibility)
        public async Task<T> FindAsync(Expression<Func<T, bool>> predicate, string[] Includes = null, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, asNoTracking: asNoTracking, includes: Includes);
            return await query.FirstOrDefaultAsync();
        }

        // New overload that accepts an include-builder for advanced includes (ThenInclude etc.)
        public async Task<T> FindAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>> includeBuilder, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, asNoTracking: asNoTracking, includeBuilder: includeBuilder);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, string[] Includes = null, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, asNoTracking: asNoTracking, includes: Includes);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, int take = 0, int skip = 0, Expression<Func<T, object>> orderBy = null, string orderByDirection = "ASC", string[] Includes = null, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, orderBy: orderBy, orderByDirection: orderByDirection, skip: skip, take: take, asNoTracking: asNoTracking, includes: Includes);
            return await query.ToListAsync();
        }

        // New overloads using include-builder for FindAll and paged FindAll
        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>> includeBuilder, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, asNoTracking: asNoTracking, includeBuilder: includeBuilder);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, int take = 0, int skip = 0, Expression<Func<T, object>> orderBy = null, string orderByDirection = "ASC", Func<IQueryable<T>, IQueryable<T>> includeBuilder = null, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, orderBy: orderBy, orderByDirection: orderByDirection, skip: skip, take: take, asNoTracking: asNoTracking, includeBuilder: includeBuilder);
            return await query.ToListAsync();
        }

        // Renamed expression-based include overloads to avoid ambiguity
        public async Task<T> FindWithIncludesAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[] includeExpressions, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, asNoTracking: asNoTracking, includeExpressions: includeExpressions);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<T>> FindAllWithIncludesAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[] includeExpressions, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, asNoTracking: asNoTracking, includeExpressions: includeExpressions);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllPagedWithIncludesAsync(Expression<Func<T, bool>> predicate, int take = 0, int skip = 0, Expression<Func<T, object>> orderBy = null, string orderByDirection = "ASC", Expression<Func<T, object>>[] includeExpressions = null, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, orderBy: orderBy, orderByDirection: orderByDirection, skip: skip, take: take, asNoTracking: asNoTracking, includeExpressions: includeExpressions);
            return await query.ToListAsync();
        }

        // Additional useful helpers
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) return await _dbSet.AnyAsync();
            return await _dbSet.AnyAsync(predicate);
        }

        public async Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, string[] Includes = null, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, asNoTracking: asNoTracking, includes: Includes);
            return await query.SingleOrDefaultAsync();
        }

        // Paged result with total count
        public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(Expression<Func<T, bool>> predicate, int take, int skip, Expression<Func<T, object>> orderBy = null, string orderByDirection = "ASC", Expression<Func<T, object>>[] includeExpressions = null, bool asNoTracking = true)
        {
            var baseQuery = BuildQuery(predicate: predicate, asNoTracking: asNoTracking, includeExpressions: includeExpressions);
            var total = await baseQuery.CountAsync();

            var query = BuildQuery(predicate: predicate, orderBy: orderBy, orderByDirection: orderByDirection, skip: skip, take: take, asNoTracking: asNoTracking, includeExpressions: includeExpressions);
            var items = await query.ToListAsync();
            return (items, total);
        }

        // New GetPagedAsync overload with include-builder
        public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(Expression<Func<T, bool>> predicate, int take, int skip, Expression<Func<T, object>> orderBy = null, string orderByDirection = "ASC", Func<IQueryable<T>, IQueryable<T>> includeBuilder = null, bool asNoTracking = true)
        {
            var baseQuery = BuildQuery(predicate: predicate, asNoTracking: asNoTracking, includeBuilder: includeBuilder);
            var total = await baseQuery.CountAsync();

            var query = BuildQuery(predicate: predicate, orderBy: orderBy, orderByDirection: orderByDirection, skip: skip, take: take, asNoTracking: asNoTracking, includeBuilder: includeBuilder);
            var items = await query.ToListAsync();
            return (items, total);
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
