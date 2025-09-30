using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountById
{
    public record GetAccountByIdQuery(int Id) : IQuery<AccountDto>;
}
