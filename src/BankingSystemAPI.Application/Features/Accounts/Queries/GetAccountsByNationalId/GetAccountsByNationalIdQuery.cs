using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;
using System.Collections.Generic;

namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountsByNationalId
{
    public record GetAccountsByNationalIdQuery(string NationalId) : IQuery<List<AccountDto>>;
}

