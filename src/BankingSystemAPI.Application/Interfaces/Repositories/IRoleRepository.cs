using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Repositories
{
    public interface IRoleRepository: IGenericRepository<ApplicationRole, string>
    {
        Task<ApplicationRole> GetRoleByUserIdAsync(string userId);
        Task<Dictionary<string, string>> GetRolesByUserIdsAsync(IEnumerable<string> userIds);
        IQueryable<string> UsersWithRoleQuery(string roleName);
    }
}
