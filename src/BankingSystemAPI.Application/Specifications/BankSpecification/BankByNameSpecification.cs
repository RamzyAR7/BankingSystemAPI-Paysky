using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Specifications.BankSpecification
{
    public class BankByNameSpecification : BaseSpecification<Bank>
    {
        public BankByNameSpecification(string name) : base(b => b.Name == name)
        {
            AddInclude(b => b.ApplicationUsers);
        }
    }
}
