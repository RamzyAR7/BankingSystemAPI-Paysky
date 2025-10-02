using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Currencies.Commands.SetCurrencyActiveStatus
{
    public record SetCurrencyActiveStatusCommand(int Id, bool IsActive) : ICommand;
}
