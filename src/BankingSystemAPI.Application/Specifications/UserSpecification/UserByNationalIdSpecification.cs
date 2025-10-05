#region Usings
using BankingSystemAPI.Application.Interfaces.Specification;
using BankingSystemAPI.Domain.Entities;
using System;
using System.Linq.Expressions;
#endregion

namespace BankingSystemAPI.Application.Specifications.UserSpecifications
{
    public class UserByNationalIdSpecification : BaseSpecification<ApplicationUser>
    {
        public UserByNationalIdSpecification(string nationalId) : base(u => u.NationalId == nationalId)
        {
            AddInclude(u => u.Accounts);
            AddInclude(u => u.Bank);
        }
    }
}
