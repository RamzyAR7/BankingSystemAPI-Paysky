using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.DTOs.Transactions
{
    /// <summary>
    /// Request DTO to withdraw money from an account.
    /// </summary>
    public class WithdrawReqDto
    {
        /// <summary>
        /// Source account identifier.
        /// </summary>
        [Required]
        public int AccountId { get; set; }

        /// <summary>
        /// Amount to withdraw. Must be positive.
        /// </summary>
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }
}
