using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace BankingSystemAPI.Application.DTOs.Transactions
{
    /// <summary>
    /// Request DTO to transfer money between accounts.
    /// </summary>
    public class TransferReqDto
    {
        /// <summary>
        /// Source account identifier.
        /// </summary>
        [Required]
        public int SourceAccountId { get; set; }

        /// <summary>
        /// Target account identifier.
        /// </summary>
        [Required]
        public int TargetAccountId { get; set; }

        /// <summary>
        /// Amount to transfer. Must be positive.
        /// </summary>
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        /*
         * Compatibility proxy properties:
         * Some clients may send JSON using `fromAccountId` / `toAccountId` keys.
         * Add properties decorated with `JsonPropertyName` that delegate to the
         * canonical `SourceAccountId` / `TargetAccountId` so both naming styles
         * are accepted by model binding.
         */

        [JsonPropertyName("fromAccountId")]
        public int FromAccountId
        {
            get => SourceAccountId;
            set => SourceAccountId = value;
        }

        [JsonPropertyName("toAccountId")]
        public int ToAccountId
        {
            get => TargetAccountId;
            set => TargetAccountId = value;
        }
    }
}
