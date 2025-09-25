using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Authorization
{
    public interface ITransactionAuthorizationService
    {
        Task CanInitiateTransferAsync(int sourceAccountId, int targetAccountId);
        Task<IEnumerable<Transaction>> FilterTransactionsAsync(IEnumerable<Transaction> transactions);
    }
}
