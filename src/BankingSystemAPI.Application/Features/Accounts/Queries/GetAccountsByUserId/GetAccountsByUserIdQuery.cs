using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using System.Collections.Generic;

namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByUserId
{
    public record GetAccountsByUserIdQuery(string UserId) : IQuery<List<AccountDto>>;
}
