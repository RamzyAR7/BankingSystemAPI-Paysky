using BankingSystemAPI.Application.DTOs.Role;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface IRoleService
    {
        Task<List<RoleResDto>> GetAllRolesAsync();
        Task<RoleUpdateResultDto> CreateRoleAsync(RoleReqDto roleName);
        Task<RoleUpdateResultDto> DeleteRoleAsync(string roleId);
    }
}
