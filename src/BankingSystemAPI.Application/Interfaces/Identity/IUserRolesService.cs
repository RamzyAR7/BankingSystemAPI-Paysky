#region Usings
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Domain.Common;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface IUserRolesService
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        Task<Result<UserRoleUpdateResultDto>> UpdateUserRolesAsync(UpdateUserRolesDto dto);
    }
}

