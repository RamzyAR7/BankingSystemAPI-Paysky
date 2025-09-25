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
        Task<IEnumerable<Account>> FilterAccountsAsync(IEnumerable<Account> accounts);
        Task CanCreateAccountForUserAsync(string targetUserId);
    }
}
