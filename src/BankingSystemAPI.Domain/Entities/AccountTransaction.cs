using BankingSystemAPI.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Domain.Entities
{
    public class AccountTransaction
    {
        public int AccountId { get; set; }
        public Account Account { get; set; }

        public int TransactionId { get; set; }
        public Transaction Transaction { get; set; }

        public string TransactionCurrency { get; set; }
        public decimal Amount { get; set; }
        public TransactionRole Role { get; set; }
        public decimal Fees { get; set; }
    }
}
