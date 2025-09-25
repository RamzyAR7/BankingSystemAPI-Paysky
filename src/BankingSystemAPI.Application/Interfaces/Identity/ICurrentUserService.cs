using System.Threading.Tasks;
using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        int? BankId { get; }
        Task<ApplicationRole> GetRoleFromStoreAsync();
        Task<bool> IsInRoleAsync(string roleName); // Add missing method
    }
}
