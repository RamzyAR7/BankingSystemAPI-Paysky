using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Domain.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface IRoleClaimsService
    {
        Task<Result<RoleClaimsUpdateResultDto>> UpdateRoleClaimsAsync(UpdateRoleClaimsDto dto);
        Task<Result<ICollection<RoleClaimsResDto>>> GetAllClaimsByGroup();
    }
}
