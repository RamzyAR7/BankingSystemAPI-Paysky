using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace BankingSystemAPI.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<ApplicationUser, string>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersByIdsAsync(IEnumerable<string> ids, Expression<Func<ApplicationUser, object>>[] includeExpressions = null, bool asNoTracking = true)
        {
            if (ids == null) return Enumerable.Empty<ApplicationUser>();
            var idList = ids.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToList();
            if (!idList.Any()) return Enumerable.Empty<ApplicationUser>();

            IQueryable<ApplicationUser> query = _dbSet;
            if (asNoTracking) query = query.AsNoTracking();

            if (includeExpressions != null)
            {
                foreach (var include in includeExpressions)
                {
                    query = query.Include(include);
                }
            }

            query = query.Where(u => idList.Contains(u.Id));
            return await query.ToListAsync();
        }
    }
}
