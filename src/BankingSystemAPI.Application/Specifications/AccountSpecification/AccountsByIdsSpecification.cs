using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    public class AccountsByIdsSpecification : BaseSpecification<Account>
    {
        public AccountsByIdsSpecification(System.Collections.Generic.IEnumerable<int> ids) : base(a => ids.Contains(a.Id)) { }
    }
}
