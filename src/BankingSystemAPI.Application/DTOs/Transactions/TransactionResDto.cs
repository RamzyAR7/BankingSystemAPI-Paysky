using System;
using System.Text.Json.Serialization;

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
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SourceAccountId { get; set; }

        /// <summary>
        /// Target account identifier (nullable for debits).
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TargetAccountId { get; set; }

        /// <summary>
        /// Currency code for the source account.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SourceCurrency { get; set; }

        /// <summary>
        /// Currency code for the target account.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TargetCurrency { get; set; }

        /// <summary>
        /// Transaction type (Deposit, Withdraw, Transfer, etc.).
        /// </summary>
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// Role of the primary account transaction (Source/Target) as string.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string TransactionRole { get; set; } = string.Empty;

        /// <summary>
        /// Amount involved in the transaction. For transfers this will reflect the target (converted) amount.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Source amount (amount deducted from the source account). Populated for transfers.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public decimal SourceAmount { get; set; }

        /// <summary>
        /// Target amount (amount credited to the target account). Populated for transfers.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public decimal TargetAmount { get; set; }

        /// <summary>
        /// UTC timestamp when the transaction occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Fee charged on the source account for this transaction (in source account currency).
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? Fees { get; set; }
    }
}
