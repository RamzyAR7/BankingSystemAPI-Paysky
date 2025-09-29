using BankingSystemAPI.Domain.Entities;
using System.Linq.Expressions;

namespace BankingSystemAPI.Application.Specifications.CurrencySpecification
{
    public class AllCurrenciesSpecification : BaseSpecification<Currency>
    {
        public AllCurrenciesSpecification() : base(c => true)
        {
        }
    }
}
