using BankingSystemAPI.Application.DTOs.User;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface IUserService
    {
        Task<IList<UserResDto>> GetAllUsersAsync(int pageNumber, int pageSize, string? orderBy = null, string? orderDirection = null);
        Task<UserResDto?> GetUserByUsernameAsync(string username);
        Task<UserResDto?> GetUserByIdAsync(string userId);
        Task<UserUpdateResultDto> CreateUserAsync(UserReqDto user);
        Task<UserUpdateResultDto> UpdateUserAsync(string userId, UserEditDto user);
        Task<UserUpdateResultDto> ChangeUserPasswordAsync(string userId, ChangePasswordReqDto dto);
        Task<UserUpdateResultDto> DeleteUserAsync(string userId);
        Task<UserUpdateResultDto> DeleteRangeOfUsersAsync(IEnumerable<string> userIds);
        Task<UserResDto?> GetCurrentUserInfoAsync(string userId);
        Task SetUserActiveStatusAsync(string userId, bool isActive);
        Task<IList<UserResDto>> GetUsersByBankIdAsync(int bankId);
        Task<IList<UserResDto>> GetUsersByBankNameAsync(string bankName);
    }
}
