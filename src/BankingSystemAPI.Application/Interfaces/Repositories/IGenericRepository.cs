using BankingSystemAPI.Application.Interfaces.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Repositories
{
    public interface IGenericRepository<T, TKey> where T : class
    {
        IQueryable<T> Table { get; }
        Task<T> GetByIdAsync(TKey id);
        Task<IEnumerable<T>> GetAllAsync(bool asNoTracking = true);
        Task<T> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[] includeExpressions = null, bool asNoTracking = true);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[] includeExpressions = null, bool asNoTracking = true);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, int take = 0, int skip = 0, Expression<Func<T, object>> orderBy = null, string orderByDirection = "ASC", Expression<Func<T, object>>[] includeExpressions = null, bool asNoTracking = true);
        // Expression-based include overloads
        Task<T> FindWithIncludesAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[] includeExpressions, bool asNoTracking = true);
        Task<IEnumerable<T>> FindAllWithIncludesAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[] includeExpressions, bool asNoTracking = true);
        Task<IEnumerable<T>> FindAllPagedWithIncludesAsync(Expression<Func<T, bool>> predicate, int take = 0, int skip = 0, Expression<Func<T, object>> orderBy = null, string orderByDirection = "ASC", Expression<Func<T, object>>[] includeExpressions = null, bool asNoTracking = true);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate = null);
        Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = true);
        Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[] includeExpressions, bool asNoTracking = true);
        // Specification Pattern methods
        Task<T> GetAsync(ISpecification<T> spec);
        Task<IEnumerable<T>> ListAsync(ISpecification<T> spec);
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(ISpecification<T> spec);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(IQueryable<T> query, int pageNumber, int pageSize);
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(Expression<Func<T, bool>> predicate, int take, int skip, Expression<Func<T, object>> orderBy = null, string orderByDirection = "ASC", Expression<Func<T, object>>[] includeExpressions = null, bool asNoTracking = true);
        Task<IEnumerable<T>> GetByIdsAsync(IEnumerable<TKey> ids, Expression<Func<T, object>>[] includeExpressions = null, bool asNoTracking = true, int batchSize = 500);
        // CRUD
        Task<T> AddAsync(T Entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> Entities);
        Task<T> UpdateAsync(T Entity);
        Task DeleteAsync(T Entity);
        Task DeleteRangeAsync(IEnumerable<T> Entities);
    }
}
