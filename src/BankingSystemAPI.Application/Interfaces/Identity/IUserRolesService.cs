using BankingSystemAPI.Application.DTOs.User;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface IUserRolesService
    {
        Task<UserRoleUpdateResultDto> UpdateUserRolesAsync(UpdateUserRolesDto dto);
    }
}
