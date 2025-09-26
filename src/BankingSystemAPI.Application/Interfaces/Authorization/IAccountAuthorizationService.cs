using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Authorization
{
    public interface IAccountAuthorizationService
    {
        Task CanViewAccountAsync(int accountId);
        Task CanModifyAccountAsync(int accountId, AccountModificationOperation operation);
        Task<(IEnumerable<Account> Accounts, int TotalCount)> FilterAccountsAsync(IQueryable<Account> query, int pageNumber = 1, int pageSize = 10);
        Task CanCreateAccountForUserAsync(string targetUserId);
        Task<IQueryable<Account>> FilterAccountsQueryAsync(IQueryable<Account> query);
    }
}
