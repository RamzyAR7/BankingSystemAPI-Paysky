#region Usings
using BankingSystemAPI.Application.Interfaces.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Interfaces.Repositories
{
    public interface IGenericRepository<T, TKey> where T : class
    {

        IQueryable<T> Table { get; }
        Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
        Task<T?> FindAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
        Task<int> CountAsync(CancellationToken cancellationToken = default);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> ListAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
        // CRUD
        Task<T> AddAsync(T Entity, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> Entities, CancellationToken cancellationToken = default);
        Task<T?> UpdateAsync(T Entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(T Entity, CancellationToken cancellationToken = default);
        Task DeleteRangeAsync(IEnumerable<T> Entities, CancellationToken cancellationToken = default);
    }
}

