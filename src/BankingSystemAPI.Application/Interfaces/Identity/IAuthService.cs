#region Usings
using BankingSystemAPI.Application.DTOs.Auth;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface IAuthService
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        Task<AuthResultDto> LoginAsync(LoginReqDto request);
        Task<AuthResultDto> RefreshTokenAsync(string token = null);
        Task<AuthResultDto> RevokeTokenAsync(string token);
        Task<AuthResultDto> LogoutAsync(string userId);
    }
}

