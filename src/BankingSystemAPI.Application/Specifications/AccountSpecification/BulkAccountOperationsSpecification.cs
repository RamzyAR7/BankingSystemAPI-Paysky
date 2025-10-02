using BankingSystemAPI.Domain.Entities;
using System.Linq.Expressions;

namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    /// <summary>
    /// Optimized specification for bulk account operations
    /// </summary>
    public class BulkAccountOperationsSpecification : BaseSpecification<Account>
    {
        public BulkAccountOperationsSpecification(IEnumerable<int> accountIds, bool includeUser = false, bool includeCurrency = false) 
            : base(a => accountIds.Contains(a.Id))
        {
            if (includeUser)
            {
                AddInclude(a => a.User);
                AddInclude(a => a.User.Bank);
            }

            if (includeCurrency)
            {
                AddInclude(a => a.Currency);
            }

            // Optimize for bulk operations
            AsNoTracking = true;
        }

        /// <summary>
        /// For bulk deletion scenarios - validate accounts can be deleted
        /// </summary>
        public static BulkAccountOperationsSpecification ForBulkDeletion(IEnumerable<int> accountIds)
        {
            var spec = new BulkAccountOperationsSpecification(accountIds, includeUser: false, includeCurrency: false);
            spec.AsNoTracking = false; // Need tracking for deletion
            return spec;
        }

        /// <summary>
        /// For bulk status updates
        /// </summary>
        public static BulkAccountOperationsSpecification ForBulkStatusUpdate(IEnumerable<int> accountIds)
        {
            var spec = new BulkAccountOperationsSpecification(accountIds, includeUser: false, includeCurrency: false);
            spec.AsNoTracking = false; // Need tracking for updates
            return spec;
        }

        /// <summary>
        /// For bulk authorization checks
        /// </summary>
        public static BulkAccountOperationsSpecification ForBulkAuthorization(IEnumerable<int> accountIds)
        {
            return new BulkAccountOperationsSpecification(accountIds, includeUser: true, includeCurrency: false);
        }
    }
}