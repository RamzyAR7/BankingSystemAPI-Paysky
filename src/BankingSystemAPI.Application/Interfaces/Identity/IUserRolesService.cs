using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Domain.Common;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface IUserRolesService
    {
        Task<Result<UserRoleUpdateResultDto>> UpdateUserRolesAsync(UpdateUserRolesDto dto);
    }
}
