using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        bool IsAuthenticated { get; }
        Task<string?> GetRoleFromStoreAsync();
        Task<bool> IsInRoleAsync(string roleName);
        int? BankId { get; }
    }
}