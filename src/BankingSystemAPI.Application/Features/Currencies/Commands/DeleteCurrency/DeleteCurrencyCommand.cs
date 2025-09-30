using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Currencies.Commands.DeleteCurrency
{
    public record DeleteCurrencyCommand(int Id) : ICommand;
}
