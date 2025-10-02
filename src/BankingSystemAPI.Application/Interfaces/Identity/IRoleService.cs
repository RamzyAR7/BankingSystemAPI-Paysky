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
    }
}
