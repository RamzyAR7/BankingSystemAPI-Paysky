using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Specifications
{
    /// <summary>
    /// Specification to get all roles
    /// </summary>
    public class AllRolesSpecification : BaseSpecification<ApplicationRole>
    {
        public AllRolesSpecification() : base(r => true)
        {
            AsNoTracking = true;
        }
    }
}