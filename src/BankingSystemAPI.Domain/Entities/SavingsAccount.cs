using BankingSystemAPI.Domain.Constant;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Domain.Entities
{
    /// <summary>
    /// Savings account with interest calculation.
    /// Does not allow overdraft - balance must remain non-negative.
    /// </summary>
    public class SavingsAccount : Account
    {
        /// <summary>
        /// Annual interest rate as a decimal (e.g., 0.05 for 5%).
        /// Must be between 0% and 100%.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(5,4)")]
        [Range(0.0000, 1.0000, ErrorMessage = "Interest rate must be between 0% and 100%")]
        public decimal InterestRate { get; set; } = 0.0000m;

        /// <summary>
        /// Type of interest calculation frequency (Monthly, Quarterly, Annually).
        /// </summary>
        [Required]
        [Column(TypeName = "int")]
        public InterestType InterestType { get; set; } = InterestType.Monthly;

        /// <summary>
        /// Navigation property to interest calculation history.
        /// </summary>
        public virtual ICollection<InterestLog> InterestLogs { get; set; } = new List<InterestLog>();

        /// <summary>
        /// Gets the account type identifier.
        /// </summary>
        public override AccountType AccountType => AccountType.Savings;

        /// <summary>
        /// Performs withdrawal from savings account.
        /// Does not allow overdraft - balance must remain non-negative.
        /// </summary>
        /// <param name="amount">Amount to withdraw</param>
        /// <exception cref="InvalidOperationException">Thrown when insufficient funds or invalid amount</exception>
        public override void Withdraw(decimal amount)
        {
            if (amount <= 0) 
                throw new InvalidOperationException("Withdrawal amount must be greater than zero.");
            
            if (amount > Balance) 
                throw new InvalidOperationException("Insufficient funds for savings account.");
            
            Balance = Math.Round(Balance - amount, 2);
        }

        /// <summary>
        /// Calculates interest based on current balance, interest rate and frequency.
        /// </summary>
        /// <param name="days">Number of days to calculate interest for</param>
        /// <returns>Interest amount calculated</returns>
        public decimal CalculateInterest(int days)
        {
            if (days <= 0 || Balance <= 0) return 0m;

            // Calculate based on interest frequency
            var periodsPerYear = GetPeriodsPerYear();
            var dailyRate = InterestRate / 365m;
            var interest = Balance * dailyRate * days;
            
            return Math.Round(interest, 2);
        }

        /// <summary>
        /// Gets the number of interest periods per year based on InterestType.
        /// </summary>
        /// <returns>Number of periods per year</returns>
        private int GetPeriodsPerYear()
        {
            return InterestType switch
            {
                InterestType.Monthly => 12,
                InterestType.Quarterly => 4,
                InterestType.Annually => 1,
                InterestType.every5minutes => 365 * 24 * 12, // For testing
                _ => 12 // Default to monthly
            };
        }

        /// <summary>
        /// Applies calculated interest to the account balance.
        /// </summary>
        /// <param name="interestAmount">Amount of interest to apply</param>
        /// <param name="calculationDate">Date when interest was calculated</param>
        public void ApplyInterest(decimal interestAmount, DateTime calculationDate)
        {
            if (interestAmount <= 0) return;

            var balanceBeforeInterest = Balance;
            Balance = Math.Round(Balance + interestAmount, 2);

            // Create interest log entry using existing InterestLog properties
            var interestLog = new InterestLog
            {
                SavingsAccountId = Id,
                SavingsAccountNumber = AccountNumber,
                Amount = interestAmount,
                Timestamp = calculationDate
            };

            InterestLogs.Add(interestLog);
        }

        /// <summary>
        /// Gets the last interest calculation date.
        /// </summary>
        /// <returns>Date of last interest calculation, or account creation date if no interest applied</returns>
        public DateTime GetLastInterestDate()
        {
            var lastLog = InterestLogs?.OrderByDescending(log => log.Timestamp).FirstOrDefault();
            return lastLog?.Timestamp ?? CreatedDate;
        }

        /// <summary>
        /// Checks if interest should be applied based on the interest type and last calculation.
        /// </summary>
        /// <returns>True if interest should be calculated and applied</returns>
        public bool ShouldApplyInterest()
        {
            var lastInterestDate = GetLastInterestDate();
            var daysSinceLastInterest = (DateTime.UtcNow - lastInterestDate).Days;

            return InterestType switch
            {
                InterestType.Monthly => daysSinceLastInterest >= 30,
                InterestType.Quarterly => daysSinceLastInterest >= 90,
                InterestType.Annually => daysSinceLastInterest >= 365,
                InterestType.every5minutes => (DateTime.UtcNow - lastInterestDate).TotalMinutes >= 5,
                _ => false
            };
        }

        /// <summary>
        /// Gets total interest earned on this account.
        /// </summary>
        /// <returns>Sum of all interest amounts applied</returns>
        public decimal GetTotalInterestEarned()
        {
            return InterestLogs?.Sum(log => log.Amount) ?? 0m;
        }
    }
}
