using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Domain.Entities
{
    /// <summary>
    /// Checking account with overdraft facility.
    /// Allows negative balance up to the overdraft limit.
    /// </summary>
    public class CheckingAccount : Account
    {
        /// <summary>
        /// Maximum overdraft amount allowed for this checking account.
        /// Must be non-negative.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999999999999, ErrorMessage = "Overdraft limit must be non-negative")]
        public decimal OverdraftLimit { get; set; } = 0m;

        /// <summary>
        /// Gets the account type identifier.
        /// </summary>
        public override AccountType AccountType => AccountType.Checking;

        /// <summary>
        /// Performs withdrawal with overdraft facility.
        /// Allows withdrawal up to balance + overdraft limit.
        /// </summary>
        /// <param name="amount">Amount to withdraw</param>
        /// <exception cref="InvalidOperationException">Thrown when amount exceeds available funds including overdraft</exception>
        public override void Withdraw(decimal amount)
        {
            if (amount <= 0) 
                throw new InvalidOperationException("Withdrawal amount must be greater than zero.");

            var available = Balance + OverdraftLimit;
            if (amount > available) 
                throw new InvalidOperationException("Insufficient funds including overdraft.");

            Balance = Math.Round(Balance - amount, 2);
        }

        /// <summary>
        /// Checks if account is using overdraft facility.
        /// </summary>
        /// <returns>True if balance is negative</returns>
        public bool IsOverdrawn()
        {
            return Balance < 0;
        }

        /// <summary>
        /// Gets the current overdraft amount being used.
        /// </summary>
        /// <returns>Overdraft amount in use (0 if not overdrawn)</returns>
        public decimal GetOverdraftUsed()
        {
            return Balance < 0 ? Math.Abs(Balance) : 0m;
        }

        /// <summary>
        /// Gets remaining overdraft facility available.
        /// </summary>
        /// <returns>Remaining overdraft that can be used</returns>
        public decimal GetRemainingOverdraft()
        {
            var used = GetOverdraftUsed();
            return OverdraftLimit - used;
        }
    }
}
