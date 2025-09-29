using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    public class AccountByAccountNumberSpecification : BaseSpecification<Account>
    {
        public AccountByAccountNumberSpecification(string accountNumber) : base(a => a.AccountNumber == accountNumber)
        {
            // include relations by default for account lookups by number
            AddInclude(a => a.User);
            AddInclude(a => a.Currency);
        }
    }
}
