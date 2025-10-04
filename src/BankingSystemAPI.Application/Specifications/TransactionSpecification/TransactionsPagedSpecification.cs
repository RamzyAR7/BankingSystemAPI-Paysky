using BankingSystemAPI.Application.Specifications;
using BankingSystemAPI.Domain.Entities;
using System;

namespace BankingSystemAPI.Application.Specifications.TransactionSpecification
{
    public class TransactionsPagedSpecification : PagedSpecification<Transaction>
    {
        public TransactionsPagedSpecification(int skip, int take, string? orderBy = null, string? orderDir = null)
            : base(skip, take, orderBy ?? "Timestamp", orderDir ?? "DESC", t => t.AccountTransactions)
        {
            // AccountTransactions navigation property is now included via the base constructor
            // This ensures that currency information is available for AutoMapper
        }
    }
}
