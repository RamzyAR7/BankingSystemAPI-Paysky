using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Repositories
{
    public interface IGenericRepository<T, TKey> where T : class
    {
        Task<T> GetByIdAsync(TKey id);
        Task<IEnumerable<T>> GetAllAsync(bool asNoTracking = true);

        // string-based includes (existing)
        Task<T> FindAsync(Expression<Func<T, bool>> predicate, string[] Includes = null, bool asNoTracking = true);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, string[] Includes = null, bool asNoTracking = true);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, int take = 0, int skip = 0, Expression<Func<T, object>> orderBy = null, string orderByDirection = "ASC", string[] Includes = null, bool asNoTracking = true);

        // include-builder overloads for advanced includes (ThenInclude etc.)
        Task<T> FindAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>> includeBuilder, bool asNoTracking = true);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>> includeBuilder, bool asNoTracking = true);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, int take = 0, int skip = 0, Expression<Func<T, object>> orderBy = null, string orderByDirection = "ASC", Func<IQueryable<T>, IQueryable<T>> includeBuilder = null, bool asNoTracking = true);

        // expression-based include methods (renamed to avoid ambiguity)
        Task<T> FindWithIncludesAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[] includeExpressions, bool asNoTracking = true);
        Task<IEnumerable<T>> FindAllWithIncludesAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[] includeExpressions, bool asNoTracking = true);
        Task<IEnumerable<T>> FindAllPagedWithIncludesAsync(Expression<Func<T, bool>> predicate, int take = 0, int skip = 0, Expression<Func<T, object>> orderBy = null, string orderByDirection = "ASC", Expression<Func<T, object>>[] includeExpressions = null, bool asNoTracking = true);

        // helpers
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, string[] Includes = null, bool asNoTracking = true);
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(Expression<Func<T, bool>> predicate, int take, int skip, Expression<Func<T, object>> orderBy = null, string orderByDirection = "ASC", Expression<Func<T, object>>[] includeExpressions = null, bool asNoTracking = true);

        Task<T> AddAsync(T Entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> Entities);
        Task<T> UpdateAsync(T Entity);
        Task DeleteAsync(T Entity);
        Task DeleteRangeAsync(IEnumerable<T> Entities);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate = null);
    }
}
