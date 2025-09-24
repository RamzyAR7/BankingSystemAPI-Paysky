using System.Threading.Tasks;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using System.Collections.Generic;

namespace BankingSystemAPI.Application.Interfaces.Identity
{
    public interface IBankAuthorizationHelper
    {
        Task<bool> IsSuperAdminAsync();
        Task<bool> IsClientAsync();
        Task<ApplicationUser> GetActingUserAsync();
        Task<int?> GetActingUserBankIdAsync();
        Task EnsureCanAccessBankAsync(int targetBankId);
        Task EnsureCanAccessUserAsync(string targetUserId);
        Task EnsureCanAccessAccountAsync(int accountId);
        Task EnsureCanInitiateTransferAsync(int sourceAccountId, int targetAccountId);
        Task EnsureCanModifyAccountAsync(int accountId, AccountModificationOperation operation);
        Task EnsureCanModifyUserAsync(string targetUserId, UserModificationOperation operation);
        Task EnsureCanCreateAccountForUserAsync(string targetUserId);

        // Helpers to filter collections without throwing (used for list endpoints)
        Task<IEnumerable<ApplicationUser>> FilterUsersAsync(IEnumerable<ApplicationUser> users);
        Task<IEnumerable<Account>> FilterAccountsAsync(IEnumerable<Account> accounts);
        Task<IEnumerable<Transaction>> FilterTransactionsAsync(IEnumerable<Transaction> transactions);
    }
}