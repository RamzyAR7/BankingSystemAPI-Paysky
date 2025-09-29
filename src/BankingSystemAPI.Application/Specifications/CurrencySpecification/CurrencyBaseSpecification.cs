using BankingSystemAPI.Domain.Entities;
using System;
using System.Linq.Expressions;

namespace BankingSystemAPI.Application.Specifications.CurrencySpecification
{
    public class CurrencyBaseSpecification : BaseSpecification<Currency>
    {
        public CurrencyBaseSpecification()
            : base(c => c.IsBase)
        {
        }

        public CurrencyBaseSpecification(int excludeId)
            : base(c => c.IsBase && c.Id != excludeId)
        {
        }
    }
}
