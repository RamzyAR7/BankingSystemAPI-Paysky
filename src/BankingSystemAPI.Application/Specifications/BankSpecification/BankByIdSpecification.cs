using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Specifications.BankSpecification
{
    public class BankByIdSpecification : BaseSpecification<Bank>
    {
        public BankByIdSpecification(int id) : base(b => b.Id == id)
        {
            AddInclude(b => b.ApplicationUsers);
        }
    }
}
