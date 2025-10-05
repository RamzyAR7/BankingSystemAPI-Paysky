#region Usings
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    /// <summary>
    /// Specification to get accounts by a list of IDs with proper includes
    /// </summary>
    public class AccountsByIdsSpecification : BaseSpecification<Account>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public AccountsByIdsSpecification(System.Collections.Generic.IEnumerable<int> ids) 
            : base(a => ids.Contains(a.Id))
        {
            // Include user for authorization checks
            AddInclude(a => a.User);
            
            // No tracking for read operations by default
            AsNoTracking = true;
        }
        
        /// <summary>
        /// Create specification for operations requiring tracking (like delete)
        /// </summary>
        public static AccountsByIdsSpecification WithTracking(System.Collections.Generic.IEnumerable<int> ids)
        {
            var spec = new AccountsByIdsSpecification(ids);
            spec.AsNoTracking = false;
            return spec;
        }
    }
}

