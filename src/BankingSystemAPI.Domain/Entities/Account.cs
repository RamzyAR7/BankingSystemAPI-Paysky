#region Usings
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Domain.Entities
{
    /// <summary>
    /// Abstract base class for all account types in the banking system.
    /// Implements optimistic concurrency control via RowVersion.
    /// </summary>
    public abstract class Account
    {
        #region Properties

        /// <summary>
        /// Primary key identifier for the account.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Unique account number for identification.
        /// </summary>
        [Required(ErrorMessage = "Account number is required")]
        [StringLength(50, MinimumLength = 10, ErrorMessage = "Account number must be between 10 and 50 characters")]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Account number must contain only uppercase letters and numbers")]
        [Column(TypeName = "varchar(50)")]
        public string AccountNumber { get; set; } = string.Empty;

        /// <summary>
        /// Current account balance with precision for financial calculations.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "-999999999999999999", "999999999999999999",
               ErrorMessage = "Balance must be within valid range")]
        public decimal Balance { get; set; }

        /// <summary>
        /// UTC timestamp when the account was created.
        /// </summary>
        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Foreign key reference to the account owner.
        /// </summary>
        [Required(ErrorMessage = "User ID is required")]
        [StringLength(450)] // Standard ASP.NET Identity key length
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key reference to the account currency.
        /// </summary>
        [Required]
        [ForeignKey(nameof(Currency))]
        public int CurrencyId { get; set; }

        /// <summary>
        /// Indicates whether the account is active and can perform transactions.
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Navigation property to the account owner.
        /// </summary>
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// Navigation property to the account currency.
        /// </summary>
        public virtual Currency Currency { get; set; } = null!;

        /// <summary>
        /// Navigation property to all transactions involving this account.
        /// </summary>
        public virtual ICollection<AccountTransaction> AccountTransactions { get; set; } = new List<AccountTransaction>();

        #endregion

        #region Concurrency Control

        /// <summary>
        /// Row version for optimistic concurrency control.
        /// Automatically managed by Entity Framework.
        /// </summary>
        [Timestamp]
        [ConcurrencyCheck]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        #endregion

        #region Abstract Members

        /// <summary>
        /// Gets the account type for polymorphic identification.
        /// </summary>
        public abstract AccountType AccountType { get; }

        #endregion

        #region Domain Operations

        /// <summary>
        /// Performs a deposit operation on the account.
        /// </summary>
        /// <param name="amount">Amount to deposit (must be positive)</param>
        /// <exception cref="InvalidOperationException">Thrown when amount is not positive</exception>
        public virtual void Deposit(decimal amount)
        {
            if (amount <= 0)
                throw new InvalidOperationException("Deposit amount must be greater than zero.");

            Balance = Math.Round(Balance + amount, 2);
        }

        /// <summary>
        /// Abstract method for withdrawal operations.
        /// Concrete account types must implement specific withdrawal rules.
        /// </summary>
        /// <param name="amount">Amount to withdraw</param>
        public abstract void Withdraw(decimal amount);

        /// <summary>
        /// Performs withdrawal for transfer operations including fees.
        /// Default implementation prevents overdraft (no negative balance allowed).
        /// </summary>
        /// <param name="totalAmount">Total amount to withdraw including transfer amount and fees</param>
        /// <exception cref="InvalidOperationException">Thrown when amount is invalid or insufficient funds</exception>
        public void WithdrawForTransfer(decimal totalAmount)
        {
            if (totalAmount <= 0)
                throw new InvalidOperationException("Transfer amount must be greater than zero.");

            if (totalAmount > Balance)
                throw new InvalidOperationException($"Insufficient funds for transfer. Available balance: {Balance:C}, Required: {totalAmount:C}. Overdraft is not available for transfers.");

            Balance = Math.Round(Balance - totalAmount, 2);
        }

        /// <summary>
        /// Validates if the account can perform transactions.
        /// </summary>
        /// <returns>True if account is active and ready for transactions</returns>
        public virtual bool CanPerformTransactions()
        {
            return IsActive && User?.IsActive == true;
        }

        /// <summary>
        /// Gets available balance for withdrawal operations.
        /// Base implementation returns current balance.
        /// Override in derived classes for accounts with overdraft facilities.
        /// </summary>
        /// <returns>Available balance including any overdraft facility</returns>
        public decimal GetAvailableBalance()
        {
            return Balance;
        }

        #endregion
    }
}

