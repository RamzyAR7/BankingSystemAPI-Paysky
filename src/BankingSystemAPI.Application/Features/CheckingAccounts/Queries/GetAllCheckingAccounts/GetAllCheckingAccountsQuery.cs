using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using System.Collections.Generic;

namespace BankingSystemAPI.Application.Features.CheckingAccounts.Queries.GetAllCheckingAccounts
{
    public record GetAllCheckingAccountsQuery(int PageNumber = 1, int PageSize = 10, string? OrderBy = null, string? OrderDirection = null) : IQuery<List<CheckingAccountDto>>;
}
