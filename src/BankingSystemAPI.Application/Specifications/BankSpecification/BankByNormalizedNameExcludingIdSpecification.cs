#region Usings
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Application.Specifications.BankSpecification
{
    public class BankByNormalizedNameExcludingIdSpecification : BaseSpecification<Bank>
    {
        public BankByNormalizedNameExcludingIdSpecification(string normalizedLower, int excludeId)
            : base(b => b.Id != excludeId && b.Name.ToLower() == normalizedLower)
        {
            AddInclude(b => b.ApplicationUsers);
        }
    }
}
