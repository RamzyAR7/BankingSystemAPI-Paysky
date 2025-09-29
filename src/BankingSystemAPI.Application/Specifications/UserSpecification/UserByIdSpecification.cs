using BankingSystemAPI.Application.Interfaces.Specification;
using BankingSystemAPI.Domain.Entities;
using System;
using System.Linq.Expressions;

namespace BankingSystemAPI.Application.Specifications.UserSpecifications
{
    public class UserByIdSpecification : BaseSpecification<ApplicationUser>
    {
        public UserByIdSpecification(string id) : base(u => u.Id == id)
        {
            AddInclude(u => u.Accounts);
            AddInclude(u => u.Bank);
        }
    }
}
