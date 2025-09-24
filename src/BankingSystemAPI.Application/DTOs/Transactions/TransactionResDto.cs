using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.DTOs.Transactions
{
    /// <summary>
    /// DTO representing a transaction record.
    /// </summary>
    public class TransactionResDto
    {
        /// <summary>
        /// Transaction identifier.
        /// </summary>
        public int TransactionId { get; set; }

        /// <summary>
        /// Source account identifier (nullable for credits).
        /// </summary>
        public int? SourceAccountId { get; set; }

        /// <summary>
        /// Target account identifier (nullable for debits).
        /// </summary>
        public int? TargetAccountId { get; set; }

        /// <summary>
        /// Currency code for the source account.
        /// </summary>
        public string SourceCurrency { get; set; } = string.Empty;

        /// <summary>
        /// Currency code for the target account.
        /// </summary>
        public string TargetCurrency { get; set; } = string.Empty;

        /// <summary>
        /// Transaction type (Deposit, Withdraw, Transfer, etc.).
        /// </summary>
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// Role of the primary account transaction (Source/Target) as string.
        /// </summary>
        public string TransactionRole { get; set; } = string.Empty;

        /// <summary>
        /// Amount involved in the transaction. For transfers this will reflect the target (converted) amount.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Source amount (amount deducted from the source account). Populated for transfers.
        /// </summary>
        public decimal SourceAmount { get; set; }

        /// <summary>
        /// Target amount (amount credited to the target account). Populated for transfers.
        /// </summary>
        public decimal TargetAmount { get; set; }

        /// <summary>
        /// UTC timestamp when the transaction occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Fee charged on the source account for this transaction (in source account currency).
        /// </summary>
        public decimal Fees { get; set; }
    }
}
