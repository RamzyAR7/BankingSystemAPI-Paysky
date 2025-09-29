using BankingSystemAPI.Domain.Entities;
using System.Linq.Expressions;

namespace BankingSystemAPI.Application.Specifications.CurrencySpecification
{
    public class CurrencyByIdSpecification : BaseSpecification<Currency>
    {
        public CurrencyByIdSpecification(int id) : base(c => c.Id == id)
        {
        }
    }
}
