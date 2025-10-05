#region Usings
using BankingSystemAPI.Application.Interfaces.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Interfaces.Repositories
{
    public interface IGenericRepository<T, TKey> where T : class
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        IQueryable<T> Table { get; }
        Task<T> GetByIdAsync(TKey id);
        Task<T> FindAsync(ISpecification<T> spec);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate = null);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> ListAsync(ISpecification<T> spec);
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(ISpecification<T> spec);
        // CRUD
        Task<T> AddAsync(T Entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> Entities);
        Task<T> UpdateAsync(T Entity);
        Task DeleteAsync(T Entity);
        Task DeleteRangeAsync(IEnumerable<T> Entities);
    }
}

