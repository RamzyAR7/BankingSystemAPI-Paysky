using BankingSystemAPI.Application.DTOs.Transactions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using System.Collections.Generic;

namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetByAccountId
{
    public record GetTransactionsByAccountQuery(int AccountId, int PageNumber = 1, int PageSize = 20, string? OrderBy = null, string? OrderDirection = null) : IQuery<IEnumerable<TransactionResDto>>;
}
