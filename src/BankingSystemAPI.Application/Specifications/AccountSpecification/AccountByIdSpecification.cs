#region Usings
using BankingSystemAPI.Domain.Entities;
using System;
using System.Linq.Expressions;
#endregion


namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    public class AccountByIdSpecification : BaseSpecification<Account>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public AccountByIdSpecification(int id) : base(a => a.Id == id)
        {
            AddInclude(a => a.User);
            AddInclude(a => a.Currency);
        }
        public AccountByIdSpecification(Expression<Func<Account, bool>> predicate) : base(predicate)
        {
            AddInclude(a => a.User);
            AddInclude(a => a.Currency);
        }
    }
}

