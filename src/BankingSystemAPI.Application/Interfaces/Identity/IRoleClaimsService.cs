#region Usings
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Domain.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface IRoleClaimsService
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        Task<Result<RoleClaimsUpdateResultDto>> UpdateRoleClaimsAsync(UpdateRoleClaimsDto dto);
        Task<Result<ICollection<RoleClaimsResDto>>> GetAllClaimsByGroup();
    }
}

