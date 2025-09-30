using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Interfaces.Messaging;
using System.Collections.Generic;

namespace BankingSystemAPI.Application.Features.Currencies.Queries.GetAllCurrencies
{
    public record GetAllCurrenciesQuery() : IQuery<List<CurrencyDto>>;
}
