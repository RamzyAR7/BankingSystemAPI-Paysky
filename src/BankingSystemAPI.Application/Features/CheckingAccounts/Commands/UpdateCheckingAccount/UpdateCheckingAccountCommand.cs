using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.CheckingAccounts.Commands.UpdateCheckingAccount
{
    public record UpdateCheckingAccountCommand(int Id, CheckingAccountEditDto Req) : ICommand<CheckingAccountDto>;
}
