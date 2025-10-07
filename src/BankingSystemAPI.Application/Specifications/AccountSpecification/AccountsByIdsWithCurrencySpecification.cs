#region Usings
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    /// <summary>
    /// Specification to get accounts by a list of IDs with Currency navigation property included
    /// Used for transfer operations where Currency information is needed
    /// </summary>
    public class AccountsByIdsWithCurrencySpecification : BaseSpecification<Account>
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        public AccountsByIdsWithCurrencySpecification(System.Collections.Generic.IEnumerable<int> ids)
            : base(a => ids.Contains(a.Id))
        {
            // Include user for authorization checks
            AddInclude(a => a.User);

            // Include currency for transaction operations
            AddInclude(a => a.Currency);

            // Enable tracking for update operations
            AsNoTracking = false;
        }
    }
}
