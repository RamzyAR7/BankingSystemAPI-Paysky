using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Accounts.Commands.SetAccountActiveStatus
{
    public record SetAccountActiveStatusCommand(int Id, bool IsActive) : ICommand<bool>;
}
