using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    public class AccountsByNationalIdSpecification : BaseSpecification<Account>
    {
        public AccountsByNationalIdSpecification(string nationalId) : base(a => a.User.NationalId == nationalId)
        {
            AddInclude(a => a.User);
            AddInclude(a => a.Currency);
        }
    }
}
