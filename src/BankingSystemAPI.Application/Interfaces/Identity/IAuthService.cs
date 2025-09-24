using BankingSystemAPI.Application.DTOs.Auth;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface IAuthService
    {
        Task<AuthResultDto> LoginAsync(LoginReqDto request);
        Task<AuthResultDto> RefreshTokenAsync(string token = null);
        Task<AuthResultDto> RevokeTokenAsync(string token);
        Task<AuthResultDto> LogoutAsync(string userId);
    }
}
