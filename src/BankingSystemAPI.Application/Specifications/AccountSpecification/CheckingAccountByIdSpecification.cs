using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    public class CheckingAccountByIdSpecification : BaseSpecification<Account>
    {
        public CheckingAccountByIdSpecification(int id) : base(a => a.Id == id && a is CheckingAccount) { }
    }
}
