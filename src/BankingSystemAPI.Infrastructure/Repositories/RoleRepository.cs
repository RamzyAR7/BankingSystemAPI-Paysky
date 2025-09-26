using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Application.Interfaces;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BankingSystemAPI.Infrastructure.Services;


namespace BankingSystemAPI.Infrastructure.Repositories
{
    public class RoleRepository : GenericRepository<ApplicationRole, string>, IRoleRepository
    {

        private static readonly Func<ApplicationDbContext, string, string> _compiledGetRoleIdByUserId =
            EF.CompileQuery((ApplicationDbContext ctx, string userId) =>
                ctx.UserRoles.AsNoTracking().Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).FirstOrDefault());

        private readonly ICacheService _cache;
        private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(5);

        public RoleRepository(ApplicationDbContext context, ICacheService cache) : base(context)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<ApplicationRole> GetRoleByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            // Try cache first (cache maps userId -> roleName)
            if (_cache.TryGetValue<string>($"role_user_{userId}", out var cachedRoleName))
            {
                if (string.IsNullOrEmpty(cachedRoleName)) return null;
                return await FindAsync(r => r.Name == cachedRoleName);
            }

            // Use compiled query for roleId lookup
            var roleId = _compiledGetRoleIdByUserId(_context, userId);

            if (string.IsNullOrEmpty(roleId))
            {
                _cache.Set($"role_user_{userId}", string.Empty, _defaultTtl);
                return null;
            }

            var role = await FindAsync(r => r.Id == roleId);

            // Cache role name for fast future lookups
            _cache.Set($"role_user_{userId}", role?.Name ?? string.Empty, _defaultTtl);

            return role;
        }

        public async Task<Dictionary<string, string>> GetRolesByUserIdsAsync(IEnumerable<string> userIds)
        {
            if (userIds == null || !userIds.Any())
                return new Dictionary<string, string>();

            // For batch queries, bypass cache and query DB directly (optimized JOIN)
            return await _context.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_context.Roles,
                      ur => ur.RoleId,
                      r => r.Id,
                      (ur, r) => new { ur.UserId, r.Name })
                .GroupBy(x => x.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.First().Name);
        }

        public IQueryable<string> UsersWithRoleQuery(string roleName)
        {
            // Use IdentityDbContext's UserRoles and Roles navigation
            return _context.UserRoles
                .Join(_context.Roles,
                      ur => ur.RoleId,
                      r => r.Id,
                      (ur, r) => new { ur.UserId, r.Name })
                .Where(x => x.Name == roleName)
                .Select(x => x.UserId);
        }

        // Cache invalidation on role changes
        public override async Task<ApplicationRole> AddAsync(ApplicationRole Entity)
        {
            var result = await base.AddAsync(Entity);
            // New role usually has no user assignments; nothing to evict
            return result;
        }

        public override async Task<ApplicationRole> UpdateAsync(ApplicationRole Entity)
        {
            var result = await base.UpdateAsync(Entity);
            if (result != null)
            {
                // Evict user->role cache entries for users that have this role
                var userIds = await _context.UserRoles.Where(ur => ur.RoleId == result.Id).Select(ur => ur.UserId).ToListAsync();
                foreach (var uid in userIds)
                {
                    _cache.Remove($"role_user_{uid}");
                }
            }
            return result;
        }

        public override async Task DeleteAsync(ApplicationRole Entity)
        {
            // Capture affected users before deletion
            var userIds = await _context.UserRoles.Where(ur => ur.RoleId == Entity.Id).Select(ur => ur.UserId).ToListAsync();

            await base.DeleteAsync(Entity);

            foreach (var uid in userIds)
            {
                _cache.Remove($"role_user_{uid}");
            }
        }
    }
}
