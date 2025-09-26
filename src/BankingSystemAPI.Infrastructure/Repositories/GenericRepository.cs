using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Application.Interfaces.Specification;
using BankingSystemAPI.Infrastructure.Context;
using BankingSystemAPI.Infrastructure.Specifications;
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

        // Expose the queryable table for advanced queries in concrete repos
        public IQueryable<T> Table => _dbSet;

        public async Task<IEnumerable<T>> GetAllAsync(bool asNoTracking = true)
        {
            IQueryable<T> query = _dbSet;
            if (asNoTracking) query = query.AsNoTracking();
            return await query.ToListAsync();
        }

        public virtual async Task<T> GetByIdAsync(TKey id)
        {
            return await _dbSet.FindAsync(id);
        }

        // Helper to apply includes (expression includes, or include-builder)
        private IQueryable<T> ApplyIncludes(IQueryable<T> query, Expression<Func<T, object>>[] includeExpressions = null, Func<IQueryable<T>, IQueryable<T>> includeBuilder = null)
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
            Expression<Func<T, object>>[] includeExpressions = null,
            Func<IQueryable<T>, IQueryable<T>> includeBuilder = null)
        {
            IQueryable<T> query = _dbSet;
            if (asNoTracking) query = query.AsNoTracking();

            // Apply includes using unified helper
            query = ApplyIncludes(query, includeExpressions, includeBuilder);

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

        // Protected generic HasOrderBy for reuse in repos
        protected static bool HasOrderBy(IQueryable<T> query)
        {
            if (query.Expression is MethodCallExpression m)
            {
                while (m != null)
                {
                    var name = m.Method.Name;
                    if (name.StartsWith("OrderBy", StringComparison.Ordinal) || name.StartsWith("ThenBy", StringComparison.Ordinal))
                        return true;

                    if (m.Arguments.Count > 0 && m.Arguments[0] is MethodCallExpression inner)
                        m = inner;
                    else
                        break;
                }
            }

            return false;
        }

        // Simple predicate-based helpers
        public async Task<T> FindAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[] includeExpressions = null, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, asNoTracking: asNoTracking, includeExpressions: includeExpressions);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[] includeExpressions = null, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, asNoTracking: asNoTracking, includeExpressions: includeExpressions);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, int take = 0, int skip = 0, Expression<Func<T, object>> orderBy = null, string orderByDirection = "ASC", Expression<Func<T, object>>[] includeExpressions = null, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, orderBy: orderBy, orderByDirection: orderByDirection, skip: skip, take: take, asNoTracking: asNoTracking, includeExpressions: includeExpressions);
            return await query.ToListAsync();
        }

        // Unified include-builder overloads (explicit names to avoid ambiguity)
        public async Task<T> FindWithIncludeBuilderAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>> includeBuilder, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, asNoTracking: asNoTracking, includeBuilder: includeBuilder);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<T>> FindAllWithIncludeBuilderAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>> includeBuilder, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, asNoTracking: asNoTracking, includeBuilder: includeBuilder);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAllWithIncludeBuilderAsync(Expression<Func<T, bool>> predicate, int take, int skip, Expression<Func<T, object>> orderBy, string orderByDirection, Func<IQueryable<T>, IQueryable<T>> includeBuilder, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, orderBy: orderBy, orderByDirection: orderByDirection, skip: skip, take: take, asNoTracking: asNoTracking, includeBuilder: includeBuilder);
            return await query.ToListAsync();
        }

        // expression-based include methods
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

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate = null)
        {
            if (predicate == null) return await _dbSet.AnyAsync();
            return await _dbSet.AnyAsync(predicate);
        }

        public async Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, asNoTracking: asNoTracking);
            return await query.SingleOrDefaultAsync();
        }

        public async Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[] includeExpressions, bool asNoTracking = true)
        {
            var query = BuildQuery(predicate: predicate, asNoTracking: asNoTracking, includeExpressions: includeExpressions);
            return await query.SingleOrDefaultAsync();
        }

        // Specification Pattern methods
        private IQueryable<T> ApplySpecification(ISpecification<T> spec)
        {
            IQueryable<T> query = _dbSet;
            if (spec == null) return query;
            if (spec.AsNoTracking) query = query.AsNoTracking();
            if (spec.Criteria != null) query = query.Where(spec.Criteria);
            foreach (var include in spec.Includes)
                query = query.Include(include);
            if (spec.OrderBy != null)
                query = spec.OrderBy(query);
            if (spec.Skip.HasValue)
                query = query.Skip(spec.Skip.Value);
            if (spec.Take.HasValue)
                query = query.Take(spec.Take.Value);
            return query;
        }

        public async Task<T> GetAsync(ISpecification<T> spec)
        {
            var query = ApplySpecification(spec);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<T>> ListAsync(ISpecification<T> spec)
        {
            var query = ApplySpecification(spec);
            return await query.ToListAsync();
        }

        public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(ISpecification<T> spec)
        {
            // Create a base specification for count (without paging)
            var baseSpec = new Specification<T>(spec.Criteria);
            foreach (var include in spec.Includes)
                baseSpec.AddInclude(include);
            baseSpec.AsNoTrackingQuery(spec.AsNoTracking);
            var baseQuery = ApplySpecification(baseSpec);
            var total = await baseQuery.CountAsync();
            var query = ApplySpecification(spec);
            var items = await query.ToListAsync();
            return (items, total);
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

        private bool ShouldDeferSave()
        {
            return UnitOfWork.UnitOfWork.TransactionActive?.Value == true;
        }

        public virtual async Task<T> AddAsync(T Entity)
        {
            await _dbSet.AddAsync(Entity);
            if (!ShouldDeferSave())
                await _context.SaveChangesAsync();
            return Entity;
        }

        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> Entities)
        {
            await _dbSet.AddRangeAsync(Entities);
            if (!ShouldDeferSave())
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

            if (!ShouldDeferSave())
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

            if (!ShouldDeferSave())
                await _context.SaveChangesAsync();
        }
        public virtual async Task DeleteRangeAsync(IEnumerable<T> Entities)
        {
            _dbSet.RemoveRange(Entities);
            if (!ShouldDeferSave())
                await _context.SaveChangesAsync();
        }

        public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(IQueryable<T> query, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var total = await query.CountAsync();

            if (!HasOrderBy(query))
                query = query.OrderBy(x => EF.Property<object>(x, "Id"));

            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }

        // Paged predicate overload used by various services
        public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            Expression<Func<T, bool>> predicate,
            int take,
            int skip,
            Expression<Func<T, object>> orderBy = null,
            string orderByDirection = "ASC",
            Expression<Func<T, object>>[] includeExpressions = null,
            bool asNoTracking = true)
        {
            var baseQuery = BuildQuery(predicate: predicate, asNoTracking: asNoTracking, includeExpressions: includeExpressions);
            var total = await baseQuery.CountAsync();
            var query = BuildQuery(predicate: predicate, orderBy: orderBy, orderByDirection: orderByDirection, skip: skip, take: take, asNoTracking: asNoTracking, includeExpressions: includeExpressions);
            var items = await query.ToListAsync();
            return (items, total);
        }

        public async Task<IEnumerable<T>> GetByIdsAsync(IEnumerable<TKey> ids, Expression<Func<T, object>>[] includeExpressions = null, bool asNoTracking = true, int batchSize = 500)
        {
            if (ids == null) return Enumerable.Empty<T>();
            var idList = ids.Where(i => i != null).Distinct().ToList();
            if (!idList.Any()) return Enumerable.Empty<T>();

            var results = new List<T>(idList.Count);
            for (int i = 0; i < idList.Count; i += batchSize)
            {
                var batch = idList.Skip(i).Take(batchSize).ToList();
                IQueryable<T> query = _dbSet;
                if (asNoTracking) query = query.AsNoTracking();
                if (includeExpressions != null)
                {
                    foreach (var include in includeExpressions)
                        query = query.Include(include);
                }

                query = query.Where(x => batch.Contains(EF.Property<TKey>(x, "Id")));
                results.AddRange(await query.ToListAsync());
            }
            return results;
        }

        // Explicit include-builder variant required by interface
        public async Task<IEnumerable<T>> GetByIdsWithIncludeBuilderAsync(IEnumerable<TKey> ids, Func<IQueryable<T>, IQueryable<T>> includeBuilder, bool asNoTracking = true, int batchSize = 500)
        {
            if (ids == null) return Enumerable.Empty<T>();
            var idList = ids.Where(i => i != null).Distinct().ToList();
            if (!idList.Any()) return Enumerable.Empty<T>();

            var results = new List<T>(idList.Count);
            for (int i = 0; i < idList.Count; i += batchSize)
            {
                var batch = idList.Skip(i).Take(batchSize).ToList();
                IQueryable<T> query = _dbSet;
                if (asNoTracking) query = query.AsNoTracking();
                query = includeBuilder(query);

                query = query.Where(x => batch.Contains(EF.Property<TKey>(x, "Id")));
                results.AddRange(await query.ToListAsync());
            }
            return results;
        }
    }
}
