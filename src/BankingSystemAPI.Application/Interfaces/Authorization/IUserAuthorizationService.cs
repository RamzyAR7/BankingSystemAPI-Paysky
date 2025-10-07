#region Usings
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Interfaces.Authorization
{
    public interface IUserAuthorizationService
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        /// <summary>
        /// Validates if the current user can view the specified user
        /// </summary>
        Task<Result> CanViewUserAsync(string targetUserId);

        /// <summary>
        /// Validates if the current user can modify the specified user
        /// </summary>
        Task<Result> CanModifyUserAsync(string targetUserId, UserModificationOperation operation);

        /// <summary>
        /// Filters users based on current user's permissions and returns paginated results
        /// </summary>
        Task<Result<(IEnumerable<ApplicationUser> Users, int TotalCount)>> FilterUsersAsync(
            IQueryable<ApplicationUser> query,
            int pageNumber = 1,
            int pageSize = 10,
            string? orderBy = null,
            string? orderDirection = null);

        /// <summary>
        /// Validates if the current user can create new users
        /// </summary>
        Task<Result> CanCreateUserAsync();
    }
}

