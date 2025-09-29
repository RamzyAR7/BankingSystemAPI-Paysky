using BankingSystemAPI.Domain.Entities;
using System.Linq.Expressions;

namespace BankingSystemAPI.Application.Specifications.CurrencySpecification
{
    public class CurrencyByCodeSpecification : BaseSpecification<Currency>
    {
        public CurrencyByCodeSpecification(string code) : base(c => c.Code == code)
        {
        }
    }
}
