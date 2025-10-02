using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Accounts.Commands.DeleteAccount
{
    public record DeleteAccountCommand(int Id) : ICommand;
}
