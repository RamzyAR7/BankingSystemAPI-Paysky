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
        /// Gets the maximum amount that can be withdrawn including overdraft facility.
        /// This is what should be used for withdrawal validation.
        /// </summary>
        /// <returns>Balance plus available overdraft credit</returns>
        public decimal GetMaxWithdrawalAmount()
        {
            if (Balance >= 0)
            {
                // Positive balance: can withdraw balance + full overdraft
                return Balance + OverdraftLimit;
            }
            else
            {
                // Already overdrawn: can only withdraw remaining overdraft credit
                var usedOverdraft = Math.Abs(Balance);
                var remainingCredit = OverdraftLimit - usedOverdraft;
                return Math.Max(0, remainingCredit);
            }
        }

        /// <summary>
        /// Checks if a withdrawal amount is allowed with overdraft facility.
        /// </summary>
        /// <param name="amount">Amount to withdraw</param>
        /// <returns>True if withdrawal is within limits</returns>
        public bool CanWithdraw(decimal amount)
        {
            if (amount <= 0) return false;
            return amount <= GetMaxWithdrawalAmount();
        }

        /// <summary>
        /// Performs withdrawal with overdraft facility.
        /// Validates against maximum withdrawal amount including overdraft.
        /// </summary>
        /// <param name="amount">Amount to withdraw</param>
        /// <exception cref="InvalidOperationException">Thrown when withdrawal exceeds limits</exception>
        public override void Withdraw(decimal amount)
        {
            if (amount <= 0) 
                throw new InvalidOperationException("Withdrawal amount must be greater than zero.");

            if (!CanWithdraw(amount))
            {
                var maxAllowed = GetMaxWithdrawalAmount();
                var balanceFormatted = Balance >= 0 ? $"${Balance:F2}" : $"-${Math.Abs(Balance):F2}";
                var overdraftAvailable = GetAvailableOverdraftCredit();
                
                throw new InvalidOperationException(
                    $"Insufficient funds. Maximum withdrawal: ${maxAllowed:F2} " +
                    $"(Balance: {balanceFormatted}, Overdraft available: ${overdraftAvailable:F2})");
            }

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
        /// Gets remaining overdraft credit available for withdrawal.
        /// </summary>
        /// <returns>Remaining overdraft credit that can be used</returns>
        public decimal GetAvailableOverdraftCredit()
        {
            var used = GetOverdraftUsed();
            return OverdraftLimit - used;
        }

        /// <summary>
        /// Gets detailed account status including overdraft information.
        /// </summary>
        /// <returns>Formatted string with balance and overdraft details</returns>
        public string GetAccountStatus()
        {
            if (IsOverdrawn())
            {
                return $"Overdrawn: {Balance:C} (Using {GetOverdraftUsed():C} of {OverdraftLimit:C} overdraft limit)";
            }
            else
            {
                return $"Balance: {Balance:C} (Overdraft available: {OverdraftLimit:C})";
            }
        }
    }
}
