using BankingSystemAPI.Domain.Entities;
using System;
using System.Linq;

namespace BankingSystemAPI.Application.Specifications
{
    public class CurrencyBaseSpecification : Specification<Currency>
    {
        // Finds any currency that is marked as base
        public CurrencyBaseSpecification() : base(c => c.IsBase) { }
        // Finds any base currency except the one with the given id (used to ensure only one base currency exists)
        public CurrencyBaseSpecification(int excludeId) : base(c => c.IsBase && c.Id != excludeId) { }
    }
}
