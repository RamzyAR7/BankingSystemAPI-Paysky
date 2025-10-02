using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Application.Interfaces;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Application.Specifications;
using BankingSystemAPI.Application.Specifications.UserSpecifications;


namespace BankingSystemAPI.Infrastructure.Repositories
{
    public class RoleRepository : GenericRepository<ApplicationRole, string>, IRoleRepository
    {
        private static readonly Func<ApplicationDbContext, string, string> _compiledGetRoleIdByUserId =
            EF.CompileQuery((ApplicationDbContext ctx, string userId) =>
                ctx.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.RoleId).FirstOrDefault());

        private readonly ICacheService _cache;
        
        public RoleRepository(ApplicationDbContext context, ICacheService cache) : base(context)
        {
            _cache = cache;
        }

        // This method fetches role for a single userId
        public async Task<ApplicationRole> GetRoleByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            // Try cache first (cache maps userId -> roleName)
            if (_cache.TryGetValue<string>($"role_user_{userId}", out var cachedRoleName))
            {
                if (string.IsNullOrEmpty(cachedRoleName)) return null;
                return await FindAsync(new RoleByNameSpecification(cachedRoleName));
            }

            // Primary source: RoleId stored on Users table
            var roleId = _compiledGetRoleIdByUserId(_context, userId);

            if (string.IsNullOrEmpty(roleId))
            {
                _cache.Set($"role_user_{userId}", string.Empty, TimeSpan.FromHours(1));
                return null;
            }

            var role = await FindAsync(new RoleByIdSpecification(roleId));

            // Cache role name for fast future lookups
            _cache.Set($"role_user_{userId}", role?.Name ?? string.Empty, TimeSpan.FromHours(1));

            return role;
        }

        // this method fetches roles for multiple userIds and returns a dictionary of userId -> roleName
        public async Task<Dictionary<string, string>> GetRolesByUserIdsAsync(IEnumerable<string> userIds)
        {
            if (userIds == null || !userIds.Any())
                return new Dictionary<string, string>();

            // Use Role navigation to fetch role name in a single query
            var userRoles = await _context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id) && u.Role != null)
                .Select(u => new { u.Id, RoleName = u.Role.Name })
                .ToDictionaryAsync(x => x.Id, x => x.RoleName);

            return userRoles;
        }
        
        public IQueryable<string> UsersWithRoleQuery(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return Enumerable.Empty<string>().AsQueryable();

            // Use Role navigation to filter by role name directly
            return _context.Users
                .Where(u => u.Role != null && u.Role.Name == roleName)
                .Select(u => u.Id);
        }
        
        public override async Task<ApplicationRole> UpdateAsync(ApplicationRole entity)
        {
            var result = await base.UpdateAsync(entity);
            if (result != null)
            {
                var userIds = await _context.Users.Where(u => u.RoleId == result.Id).Select(u => u.Id).ToListAsync();
                foreach (var uid in userIds)
                {
                    _cache.Remove($"role_user_{uid}");
                }
            }
            return result;
        }

        public override async Task DeleteAsync(ApplicationRole entity)
        {
            var usersWithRole = await _context.Users.Where(u => u.RoleId == entity.Id).ToListAsync();
            if (usersWithRole.Any())
            {
                foreach (var u in usersWithRole)
                {
                    u.RoleId = string.Empty;
                }
            }

            await base.DeleteAsync(entity);

            // Remove cached entries
            foreach (var uid in usersWithRole.Select(u => u.Id))
            {
                _cache.Remove($"role_user_{uid}");
            }
        }
    }
}
