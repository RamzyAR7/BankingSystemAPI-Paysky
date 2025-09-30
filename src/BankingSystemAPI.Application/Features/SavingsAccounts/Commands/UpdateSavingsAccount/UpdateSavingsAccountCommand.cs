using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.SavingsAccounts.Commands.UpdateSavingsAccount
{
    public record UpdateSavingsAccountCommand(int Id, SavingsAccountEditDto Req) : ICommand<SavingsAccountDto>;
}
