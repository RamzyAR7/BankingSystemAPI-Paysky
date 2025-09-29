using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Application.Interfaces.Specification;
using BankingSystemAPI.Infrastructure.Context;
using BankingSystemAPI.Infrastructure.SpecificationEvaluatorClass;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        #region Helpers
        // Specification Pattern methods
        private IQueryable<T> ApplySpecificationInternal(ISpecification<T> spec, bool evaluatePaging = true)
        {
            return SpecificationEvaluator.ApplySpecification(_dbSet.AsQueryable(), spec, evaluatePaging);
        }
        #endregion
        public IQueryable<T> Table => _dbSet;

        public virtual async Task<T> GetByIdAsync(TKey id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<T> FindAsync(ISpecification<T> spec)
        {
            var query = ApplySpecificationInternal(spec, true);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate = null)
        {
            if (predicate == null) return await _dbSet.AnyAsync();
            return await _dbSet.AnyAsync(predicate);
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

        public async Task<IEnumerable<T>> ListAsync(ISpecification<T> spec)
        {
            var query = ApplySpecificationInternal(spec, true);
            return await query.ToListAsync();
        }

        public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(ISpecification<T> spec)
        {
            var baseQuery = ApplySpecificationInternal(spec, false);
            var total = await baseQuery.CountAsync();
            var query = ApplySpecificationInternal(spec, true);
            var items = await query.ToListAsync();
            return (items, total);
        }
        #region CRUD
        public virtual async Task<T> AddAsync(T Entity)
        {
            await _dbSet.AddAsync(Entity);
            await _context.SaveChangesAsync();
            return Entity;
        }

        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> Entities)
        {
            await _dbSet.AddRangeAsync(Entities);
            await _context.SaveChangesAsync();
            return Entities;
        }
        public virtual async Task<T> UpdateAsync(T Entity)
        {
            // Determine primary key name and value
            var entityType = _context.Model.FindEntityType(typeof(T));
            var pk = entityType?.FindPrimaryKey()?.Properties?.FirstOrDefault()?.Name;

            bool handled = false;

            if (!string.IsNullOrEmpty(pk))
            {
                var pkProp = typeof(T).GetProperty(pk);
                if (pkProp != null)
                {
                    var keyVal = pkProp.GetValue(Entity);

                    // Look for any tracked entry with same primary key value
                    var existingEntry = _context.ChangeTracker.Entries()
                        .FirstOrDefault(e =>
                        {
                            try
                            {
                                var prop = e.Properties.FirstOrDefault(p => p.Metadata.Name == pk);
                                if (prop == null) return false;
                                return Equals(prop.CurrentValue, keyVal);
                            }
                            catch
                            {
                                return false;
                            }
                        });

                    if (existingEntry != null)
                    {
                        // If tracked instance is same object, just update values
                        if (!ReferenceEquals(existingEntry.Entity, Entity))
                        {
                            existingEntry.CurrentValues.SetValues(Entity);
                        }

                        handled = true;
                    }
                }
            }

            if (!handled)
            {
                // Attach and mark modified - safer than DbSet.Update which may try to attach graphs
                _dbSet.Attach(Entity);
                _context.Entry(Entity).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
            return Entity;
        }
        public virtual async Task DeleteAsync(T Entity)
        {
            // Avoid duplicate-tracking issues: if there's an already tracked entry for this key, remove that entry
            var entityType = _context.Model.FindEntityType(typeof(T));
            var pk = entityType?.FindPrimaryKey()?.Properties?.FirstOrDefault()?.Name;
            bool handled = false;

            if (!string.IsNullOrEmpty(pk))
            {
                var pkProp = typeof(T).GetProperty(pk);
                if (pkProp != null)
                {
                    var keyVal = pkProp.GetValue(Entity);

                    var existingEntry = _context.ChangeTracker.Entries()
                        .FirstOrDefault(e =>
                        {
                            try
                            {
                                var prop = e.Properties.FirstOrDefault(p => p.Metadata.Name == pk);
                                if (prop == null) return false;
                                return Equals(prop.CurrentValue, keyVal);
                            }
                            catch
                            {
                                return false;
                            }
                        });

                    if (existingEntry != null)
                    {
                        // Remove the tracked entity instance
                        _dbSet.Remove((T)existingEntry.Entity);
                        handled = true;
                    }
                }
            }

            if (!handled)
            {
                // Attach if necessary and remove
                if (_context.Entry(Entity).State == EntityState.Detached)
                    _dbSet.Attach(Entity);

                _dbSet.Remove(Entity);
            }

            await _context.SaveChangesAsync();
        }
        public virtual async Task DeleteRangeAsync(IEnumerable<T> Entities)
        {
            _dbSet.RemoveRange(Entities);
            await _context.SaveChangesAsync();
        }
        #endregion

    }
}
