using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using System.Collections.Generic;

namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetAllTransactions
{
    public record GetAllTransactionsQuery(int PageNumber = 1, int PageSize = 20, string? OrderBy = null, string? OrderDirection = null) : IQuery<IEnumerable<TransactionResDto>>;
}
