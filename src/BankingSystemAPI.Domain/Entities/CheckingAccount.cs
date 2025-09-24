using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Domain.Entities
{
    public class CheckingAccount : Account
    {
        public decimal OverdraftLimit { get; set; }

        public override AccountType AccountType => AccountType.Checking;

        public override void Withdraw(decimal amount)
        {
            if (amount <= 0) throw new InvalidOperationException("Withdrawal amount must be greater than zero.");

            var available = Balance + OverdraftLimit;
            if (amount > available) throw new InvalidOperationException("Insufficient funds including overdraft.");

            Balance -= amount;
        }

        public override void WithdrawForTransfer(decimal amount)
        {
            // For transfers, do not allow overdraft — only use existing balance
            if (amount <= 0) throw new InvalidOperationException("Withdrawal amount must be greater than zero.");
            if (amount > Balance) throw new InvalidOperationException("Insufficient funds for transfer (overdraft not allowed).");
            Balance -= amount;
        }
    }
}
