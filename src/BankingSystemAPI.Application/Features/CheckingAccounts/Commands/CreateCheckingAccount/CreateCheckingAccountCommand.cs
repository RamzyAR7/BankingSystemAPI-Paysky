using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.CheckingAccounts.Commands.CreateCheckingAccount
{
    public record CreateCheckingAccountCommand(CheckingAccountReqDto Req) : ICommand<CheckingAccountDto>;
}
