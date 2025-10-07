#region Usings
using BankingSystemAPI.Application.Interfaces.Specification;
using BankingSystemAPI.Domain.Entities;
using System;
using System.Linq.Expressions;
#endregion


namespace BankingSystemAPI.Application.Specifications.UserSpecifications
{
    public class UserByIdSpecification : BaseSpecification<ApplicationUser>
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        public UserByIdSpecification(string id) : base(u => u.Id == id)
        {
            AddInclude(u => u.Accounts);
            AddInclude(u => u.Bank);
        }
    }
}

