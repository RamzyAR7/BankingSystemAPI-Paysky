using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Authorization
{
    public interface ITransactionAuthorizationService
    {
        /// <summary>
        /// Validates if the current user can initiate a transfer between the specified accounts
        /// </summary>
        Task<Result> CanInitiateTransferAsync(int sourceAccountId, int targetAccountId);
        
        /// <summary>
        /// Filters transactions based on current user's permissions and returns paginated results
        /// </summary>
        Task<Result<(IEnumerable<Transaction> Transactions, int TotalCount)>> FilterTransactionsAsync(IQueryable<Transaction> query, int pageNumber = 1, int pageSize = 10);
    }
}
