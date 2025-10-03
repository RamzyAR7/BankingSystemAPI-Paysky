using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Domain.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface IRoleService
    {
        Task<Result<List<RoleResDto>>> GetAllRolesAsync();
        Task<Result<RoleUpdateResultDto>> CreateRoleAsync(RoleReqDto roleName);
        Task<Result<RoleUpdateResultDto>> DeleteRoleAsync(string roleId);
        
        /// <summary>
        /// Checks if a role is currently assigned to any users.
        /// This method belongs to IRoleService as it's role-specific business logic.
        /// </summary>
        /// <param name="roleId">The role ID to check</param>
        /// <returns>True if role is in use, false otherwise</returns>
        Task<Result<bool>> IsRoleInUseAsync(string roleId);
    }
}
