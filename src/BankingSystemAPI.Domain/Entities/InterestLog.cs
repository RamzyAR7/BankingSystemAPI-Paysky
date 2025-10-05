#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Domain.Entities
{
    public class InterestLog
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        // Foreign Key of Savings
        public int SavingsAccountId { get; set; }
        public string SavingsAccountNumber { get; set; }
        // Navigation Property
        public SavingsAccount SavingsAccount { get; set; }

    }
}

