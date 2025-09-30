using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.SavingsAccounts.Commands.CreateSavingsAccount
{
    public record CreateSavingsAccountCommand(SavingsAccountReqDto Req) : ICommand<SavingsAccountDto>;
}
