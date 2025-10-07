#region Usings
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    public class AccountByAccountNumberSpecification : BaseSpecification<Account>
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        public AccountByAccountNumberSpecification(string accountNumber) : base(a => a.AccountNumber == accountNumber)
        {
            // include relations by default for account lookups by number
            AddInclude(a => a.User);
            AddInclude(a => a.Currency);
        }
    }
}

