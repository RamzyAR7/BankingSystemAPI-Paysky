#region Usings
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    public class AccountsByNationalIdSpecification : BaseSpecification<Account>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public AccountsByNationalIdSpecification(string nationalId) : base(a => a.User.NationalId == nationalId)
        {
            AddInclude(a => a.User);
            AddInclude(a => a.Currency);
        }
    }
}

