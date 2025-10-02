using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Application.Interfaces.Specification;
using BankingSystemAPI.Infrastructure.Context;
using BankingSystemAPI.Infrastructure.SpecificationEvaluatorClass;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BankingSystemAPI.Infrastructure.Repositories
{
    /// <summary>
    /// Base repository class with specification pattern support
    /// </summary>
    public abstract class GenericRepository<T, TKey> : IGenericRepository<T, TKey> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public IQueryable<T> Table => _dbSet;

        #region Read Operations

        public virtual async Task<T> GetByIdAsync(TKey id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<T> FindAsync(ISpecification<T> spec)
        {
            var query = SpecificationEvaluator.ApplySpecification(_dbSet.AsQueryable(), spec, true);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate = null)
        {
            return predicate == null 
                ? await _dbSet.AnyAsync() 
                : await _dbSet.AnyAsync(predicate);
        }

        public async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return predicate == null 
                ? await _dbSet.CountAsync()
                : await _dbSet.CountAsync(predicate);
        }

        public async Task<IEnumerable<T>> ListAsync(ISpecification<T> spec)
        {
            var query = SpecificationEvaluator.ApplySpecification(_dbSet.AsQueryable(), spec, true);
            return await query.ToListAsync();
        }

        public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(ISpecification<T> spec)
        {
            // Get count without includes for performance
            var countQuery = SpecificationEvaluator.ApplySpecification(_dbSet.AsQueryable(), spec, false);
            var total = await countQuery.CountAsync();
            
            // Get paged data with includes
            var itemsQuery = SpecificationEvaluator.ApplySpecification(_dbSet.AsQueryable(), spec, true);
            var items = await itemsQuery.ToListAsync();
            
            return (items, total);
        }

        #endregion

        #region Write Operations

        public virtual async Task<T> AddAsync(T entity)
        {
            var entry = await _dbSet.AddAsync(entity);
            
            // Auto-save if not in transaction
            if (!UnitOfWork.UnitOfWork.TransactionActive.Value)
                await _context.SaveChangesAsync();
                
            return entry.Entity;
        }

        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            var entityList = entities.ToList();
            if (!entityList.Any()) return entityList;

            await _dbSet.AddRangeAsync(entityList);
            
            // Auto-save if not in transaction
            if (!UnitOfWork.UnitOfWork.TransactionActive.Value)
                await _context.SaveChangesAsync();
                
            return entityList;
        }

        public virtual Task<T> UpdateAsync(T entity)
        {
            var entry = _context.Entry(entity);
            
            // Check if entity is already being tracked
            if (entry.State == EntityState.Detached)
            {
                // Try to find if another instance is already tracked
                var keyValues = entry.Metadata.FindPrimaryKey()?.Properties
                    .ToDictionary(p => p.Name, p => entry.Property(p.Name).CurrentValue);
                
                if (keyValues?.Values.All(v => v != null) == true)
                {
                    // Check if entity with same key is already tracked
                    var existingEntry = _context.ChangeTracker.Entries<T>()
                        .FirstOrDefault(e => keyValues.All(kv => 
                            Equals(e.Property(kv.Key).CurrentValue, kv.Value)));
                    
                    if (existingEntry != null)
                    {
                        // Update the existing tracked entity
                        existingEntry.CurrentValues.SetValues(entity);
                        return Task.FromResult(existingEntry.Entity);
                    }
                }
                
                // Safe to attach if no conflict
                _dbSet.Attach(entity);
                entry.State = EntityState.Modified;
            }
            else if (entry.State == EntityState.Unchanged)
            {
                entry.State = EntityState.Modified;
            }
            
            // Deferred save - will be handled by UoW
            return Task.FromResult(entity);
        }

        public virtual Task DeleteAsync(T entity)
        {
            var entry = _context.Entry(entity);
            
            // Check if entity is already being tracked
            if (entry.State == EntityState.Detached)
            {
                // Try to find if another instance is already tracked
                var keyValues = entry.Metadata.FindPrimaryKey()?.Properties
                    .ToDictionary(p => p.Name, p => entry.Property(p.Name).CurrentValue);
                
                if (keyValues?.Values.All(v => v != null) == true)
                {
                    // Check if entity with same key is already tracked
                    var existingEntry = _context.ChangeTracker.Entries<T>()
                        .FirstOrDefault(e => keyValues.All(kv => 
                            Equals(e.Property(kv.Key).CurrentValue, kv.Value)));
                    
                    if (existingEntry != null)
                    {
                        // Delete the existing tracked entity
                        _dbSet.Remove(existingEntry.Entity);
                        return Task.CompletedTask;
                    }
                }
                
                // Safe to attach if no conflict
                _dbSet.Attach(entity);
            }
                
            _dbSet.Remove(entity);
            
            // Deferred save - will be handled by UoW
            return Task.CompletedTask;
        }

        public virtual Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            var entityList = entities.ToList();
            if (!entityList.Any()) return Task.CompletedTask;

            _dbSet.RemoveRange(entityList);
            
            // Deferred save - will be handled by UoW
            return Task.CompletedTask;
        }

        #endregion
    }
}