using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Currencies.Queries.GetCurrencyById
{
    public record GetCurrencyByIdQuery(int Id) : IQuery<CurrencyDto>;
}
