using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Repositories
{
    public interface ITransactionRepository: IGenericRepository<Transaction, int>
    {
        IQueryable<Transaction> QueryWithAccountTransactions();
        IQueryable<Transaction> QueryByAccountId(int accountId);
    }
}
