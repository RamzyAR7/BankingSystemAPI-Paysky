using BankingSystemAPI.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Domain.Entities
{
    public class SavingsAccount : Account
    {
        public decimal InterestRate { get; set; }
        public InterestType InterestType { get; set; }

        public ICollection<InterestLog> InterestLogs { get; set; }

        public override AccountType AccountType => AccountType.Savings;

        public override void Withdraw(decimal amount)
        {
            if (amount <= 0) throw new InvalidOperationException("Withdrawal amount must be greater than zero.");
            if (amount > Balance) throw new InvalidOperationException("Insufficient funds for savings account.");
            Balance -= amount;
        }

        public override void WithdrawForTransfer(decimal amount)
        {
            // Same rule as normal withdraw for savings
            Withdraw(amount);
        }
    }
}
