using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Infrastructure.Context;
using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Infrastructure.Repositories
{
    public class TransactionRepository : GenericRepository<Transaction, int>, ITransactionRepository
    {
        public TransactionRepository(ApplicationDbContext context) : base(context)
        {
        }

        public IQueryable<Transaction> QueryWithAccountTransactions()
        {
            return Table
                .Include(t => t.AccountTransactions)
                    .ThenInclude(at => at.Account).ThenInclude(a => a.User)
                .Include(t => t.AccountTransactions)
                    .ThenInclude(at => at.Account).ThenInclude(a => a.Currency)
                .AsQueryable();
        }

        public IQueryable<Transaction> QueryByAccountId(int accountId)
        {
            return Table
                .Where(t => t.AccountTransactions.Any(at => at.AccountId == accountId))
                .Include(t => t.AccountTransactions).ThenInclude(at => at.Account).ThenInclude(a => a.User)
                .Include(t => t.AccountTransactions).ThenInclude(at => at.Account).ThenInclude(a => a.Currency)
                .AsQueryable();
        }
    }
}
