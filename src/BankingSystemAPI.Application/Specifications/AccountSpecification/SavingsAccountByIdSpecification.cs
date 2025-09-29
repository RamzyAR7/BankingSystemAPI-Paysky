using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    public class SavingsAccountByIdSpecification : BaseSpecification<Account>
    {
        public SavingsAccountByIdSpecification(int id) : base(a => a.Id == id && a is SavingsAccount) { }
    }
}
