using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountByAccountNumber
{
    public record GetAccountByAccountNumberQuery(string AccountNumber) : IQuery<AccountDto>;
}
