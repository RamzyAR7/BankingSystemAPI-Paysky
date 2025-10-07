#region Usings
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface ICurrentUserService
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        string? UserId { get; }
        int? BankId { get; }
        Task<ApplicationRole> GetRoleFromStoreAsync();
        Task<bool> IsInRoleAsync(string roleName); // Add missing method
    }
}

