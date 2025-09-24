using BankingSystemAPI.Application.DTOs.Role;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface IRoleClaimsService
    {
        Task<RoleClaimsUpdateResultDto> UpdateRoleClaimsAsync(UpdateRoleClaimsDto dto);
        Task<ICollection<RoleClaimsResDto>> GetAllClaimsByGroup();
    }
}
