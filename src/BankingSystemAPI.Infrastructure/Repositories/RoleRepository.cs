using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;


namespace BankingSystemAPI.Infrastructure.Repositories
{
    public class RoleRepository : GenericRepository<ApplicationRole, string>, IRoleRepository
    {
        public RoleRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<ApplicationRole> GetRoleByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var roleId = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(roleId)) return null;

            return await FindAsync(r => r.Id == roleId);
        }

        public async Task<Dictionary<string, string>> GetRolesByUserIdsAsync(IEnumerable<string> userIds)
        {
            if (userIds == null || !userIds.Any())
                return new Dictionary<string, string>();

            return await _context.UserRoles
                .Where(ur => userIds.Contains(ur.UserId))
                .Join(_context.Roles,
                      ur => ur.RoleId,
                      r => r.Id,
                      (ur, r) => new { ur.UserId, r.Name })
                .GroupBy(x => x.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.First().Name);
        }
    }
}
