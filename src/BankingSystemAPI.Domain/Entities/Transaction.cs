#region Usings
using BankingSystemAPI.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Domain.Entities
{
    public class Transaction
    {
        public int Id { get; set; }
        public TransactionType TransactionType { get; set; }
        public DateTime Timestamp { get; set; }

        public ICollection<AccountTransaction> AccountTransactions { get; set; }
    }
}

