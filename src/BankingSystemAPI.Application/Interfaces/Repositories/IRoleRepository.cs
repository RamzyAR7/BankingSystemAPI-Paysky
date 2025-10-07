#region Usings
using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Interfaces.Repositories
{
    public interface IRoleRepository : IGenericRepository<ApplicationRole, string>
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        Task<ApplicationRole?> GetRoleByUserIdAsync(string userId);
        Task<Dictionary<string, string?>> GetRolesByUserIdsAsync(IEnumerable<string> userIds);
        IQueryable<string> UsersWithRoleQuery(string roleName);
    }
}

