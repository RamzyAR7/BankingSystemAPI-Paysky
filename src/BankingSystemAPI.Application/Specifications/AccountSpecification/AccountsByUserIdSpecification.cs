#region Usings
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    public class AccountsByUserIdSpecification : BaseSpecification<Account>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public AccountsByUserIdSpecification(string userId) : base(a => a.UserId == userId)
        {
            AddInclude(a => a.User);
            AddInclude(a => a.Currency);
        }
    }
}

