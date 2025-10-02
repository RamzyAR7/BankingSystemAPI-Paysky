using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Domain.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface IUserService
    {
        Task<Result<UserResDto>> GetUserByUsernameAsync(string username);
        Task<Result<UserResDto>> GetUserByIdAsync(string userId);
        Task<Result<UserResDto>> GetUserByEmailAsync(string email);
        Task<Result<string?>> GetUserRoleAsync(string userId);
        Task<Result<bool>> IsBankActiveAsync(int bankId);
        Task<Result<UserResDto>> CreateUserAsync(UserReqDto user);
        Task<Result<UserResDto>> UpdateUserAsync(string userId, UserEditDto user);
        Task<Result<UserResDto>> ChangeUserPasswordAsync(string userId, ChangePasswordReqDto dto);
        Task<Result<UserResDto>> DeleteUserAsync(string userId);
        Task<Result<bool>> DeleteRangeOfUsersAsync(IEnumerable<string> userIds);
        Task<Result> SetUserActiveStatusAsync(string userId, bool isActive);
        Task<Result<IList<UserResDto>>> GetUsersByBankIdAsync(int bankId);
        Task<Result<IList<UserResDto>>> GetUsersByBankNameAsync(string bankName);
    }
}
