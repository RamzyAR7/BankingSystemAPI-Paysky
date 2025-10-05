using BankingSystemAPI.Domain.Entities;
using System.Linq.Expressions;

namespace BankingSystemAPI.Application.Specifications.CurrencySpecification
{
    public class CurrencyByCodeSpecification : BaseSpecification<Currency>
    {
        public CurrencyByCodeSpecification(string code, int? excludeId = null) : base(c =>
            (c.Code != null && c.Code.ToLower() == code.ToLower()) && (!excludeId.HasValue || c.Id != excludeId.Value))
        {
        }
    }
}
