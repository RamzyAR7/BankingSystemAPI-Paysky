using BankingSystemAPI.Application.Specifications;
using BankingSystemAPI.Domain.Entities;
using System;

namespace BankingSystemAPI.Application.Specifications.TransactionSpecification
{
    public class TransactionsPagedSpecification : PagedSpecification<Transaction>
    {
        public TransactionsPagedSpecification(int skip, int take, string? orderBy = null, string? orderDir = null, params System.Linq.Expressions.Expression<Func<Transaction, object>>[] includes)
            : base(skip, take, orderBy ?? "Timestamp", orderDir ?? "DESC", includes)
        {
        }
    }
}
