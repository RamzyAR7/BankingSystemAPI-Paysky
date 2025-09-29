using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    public class AccountsByUserIdSpecification : BaseSpecification<Account>
    {
        public AccountsByUserIdSpecification(string userId) : base(a => a.UserId == userId)
        {
            AddInclude(a => a.User);
            AddInclude(a => a.Currency);
        }
    }
}
