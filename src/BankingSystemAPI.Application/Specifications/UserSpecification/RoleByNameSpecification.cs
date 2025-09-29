using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Specifications.UserSpecifications
{
    public class RoleByNameSpecification : BaseSpecification<ApplicationRole>
    {
        public RoleByNameSpecification(string name) : base(r => r.Name == name) { }
    }
}
