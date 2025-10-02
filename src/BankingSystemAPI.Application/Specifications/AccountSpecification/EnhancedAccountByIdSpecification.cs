using BankingSystemAPI.Domain.Entities;
using System.Linq.Expressions;

namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    /// <summary>
    /// Enhanced account specification with optimized includes for better performance
    /// </summary>
    public class EnhancedAccountByIdSpecification : BaseSpecification<Account>
    {
        public EnhancedAccountByIdSpecification(int id, bool includeUser = true, bool includeCurrency = true, bool includeTransactions = false) 
            : base(a => a.Id == id)
        {
            if (includeUser)
            {
                AddInclude(a => a.User);
                // Note: Bank navigation will be loaded separately if needed
            }

            if (includeCurrency)
            {
                AddInclude(a => a.Currency);
            }

            if (includeTransactions)
            {
                AddInclude(a => a.AccountTransactions);
                // Note: Transaction details will be loaded separately if needed
            }

            // Enable no-tracking for read-only scenarios by default
            AsNoTracking = true;
        }

        /// <summary>
        /// Create specification for modification scenarios (with tracking)
        /// </summary>
        public static EnhancedAccountByIdSpecification ForModification(int id)
        {
            var spec = new EnhancedAccountByIdSpecification(id, includeUser: true, includeCurrency: true);
            spec.AsNoTracking = false; // Enable tracking for updates
            return spec;
        }

        /// <summary>
        /// Create specification for read-only display scenarios
        /// </summary>
        public static EnhancedAccountByIdSpecification ForDisplay(int id)
        {
            return new EnhancedAccountByIdSpecification(id, includeUser: true, includeCurrency: true, includeTransactions: false);
        }

        /// <summary>
        /// Create specification for transaction history scenarios
        /// </summary>
        public static EnhancedAccountByIdSpecification WithTransactionHistory(int id)
        {
            return new EnhancedAccountByIdSpecification(id, includeUser: true, includeCurrency: true, includeTransactions: true);
        }
    }
}