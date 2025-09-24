using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Domain.Entities
{
    public abstract class Account
    {
        #region Properties
        public int Id { get; set; }
        public string AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedDate { get; set; }
        // Foreign Keys of user
        public string UserId { get; set; }
        // Foreign Keys of currency
        public int CurrencyId { get; set; }
        public bool IsActive { get; set; } = true;
        #endregion

        #region Navigation Properties
        public ApplicationUser User { get; set; }
        public Currency Currency { get; set; }
        public ICollection<AccountTransaction> AccountTransactions { get; set; }
        #endregion

        [Timestamp]
        public byte[] RowVersion { get; set; }

        // Provide AccountType for easy inspection
        public abstract AccountType AccountType { get; }

        // Domain operations
        public virtual void Deposit(decimal amount)
        {
            if (amount <= 0) throw new InvalidOperationException("Deposit amount must be greater than zero.");
            Balance += amount;
        }

        // Concrete accounts must implement withdraw rules (general withdrawal behavior)
        public abstract void Withdraw(decimal amount);

        // Withdraw for transfer: default behavior prevents overdraft (useful when transfers must not use overdraft)
        public virtual void WithdrawForTransfer(decimal amount)
        {
            if (amount <= 0) throw new InvalidOperationException("Withdrawal amount must be greater than zero.");
            if (amount > Balance) throw new InvalidOperationException("Insufficient funds for transfer (overdraft not allowed).");
            Balance -= amount;
        }
    }
}
