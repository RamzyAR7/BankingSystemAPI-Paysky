using BankingSystemAPI.Domain.Entities;
using System;
using System.Linq.Expressions;

namespace BankingSystemAPI.Application.Specifications.TransactionSpecification
{
    public class TransactionsByAccountPagedSpecification : PagedSpecification<Transaction>
    {
        public TransactionsByAccountPagedSpecification(int accountId, int skip, int take, string? orderBy = null, string? orderDir = null, params Expression<Func<Transaction, object>>[] includes)
            : base(t => t.AccountTransactions.Any(at => at.AccountId == accountId), skip, take, orderBy ?? "Timestamp", orderDir ?? "DESC", includes)
        {
        }
    }
}
