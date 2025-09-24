using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
