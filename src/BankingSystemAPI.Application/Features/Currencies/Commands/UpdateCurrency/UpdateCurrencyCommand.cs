using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Currencies.Commands.UpdateCurrency
{
    public record UpdateCurrencyCommand(int Id, CurrencyReqDto Currency) : ICommand<CurrencyDto>;
}
