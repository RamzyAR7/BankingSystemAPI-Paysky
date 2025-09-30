using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Currencies.Commands.CreateCurrency
{
    public record CreateCurrencyCommand(CurrencyReqDto Currency): ICommand<CurrencyDto>;
}
