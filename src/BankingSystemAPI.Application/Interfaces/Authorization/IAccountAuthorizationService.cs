using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Authorization
{
    public interface IAccountAuthorizationService
    {
        /// <summary>
        /// Validates if the current user can view the specified account
        /// </summary>
        Task<Result> CanViewAccountAsync(int accountId);
        
        /// <summary>
        /// Validates if the current user can modify the specified account
        /// </summary>
        Task<Result> CanModifyAccountAsync(int accountId, AccountModificationOperation operation);
        
        /// <summary>
        /// Filters accounts based on current user's permissions and returns paginated results
        /// </summary>
        Task<Result<(IEnumerable<Account> Accounts, int TotalCount)>> FilterAccountsAsync(IQueryable<Account> query, int pageNumber = 1, int pageSize = 10);
        
        /// <summary>
        /// Validates if the current user can create an account for the target user
        /// </summary>
        Task<Result> CanCreateAccountForUserAsync(string targetUserId);
        
        /// <summary>
        /// Returns a filtered query based on current user's permissions
        /// </summary>
        Task<Result<IQueryable<Account>>> FilterAccountsQueryAsync(IQueryable<Account> query);
    }
}
