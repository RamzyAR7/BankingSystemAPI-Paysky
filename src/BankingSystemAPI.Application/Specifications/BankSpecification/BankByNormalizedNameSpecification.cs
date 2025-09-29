using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Specifications.BankSpecification
{
    public class BankByNormalizedNameSpecification : BaseSpecification<Bank>
    {
        public BankByNormalizedNameSpecification(string normalizedLower) : base(b => b.Name.ToLower() == normalizedLower)
        {
            AddInclude(b => b.ApplicationUsers);
        }
    }
}
