using BankingSystemAPI.Domain.Constant;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.DTOs.Account
{
    /// <summary>
    /// Request DTO to create or update a savings account.
    /// </summary>
    public class SavingsAccountReqDto
    {
        /// <summary>
        /// Identifier of the user who will own the account.
        /// </summary>
        [Required]
        public string UserId { get; set; }

        /// <summary>
        /// Currency identifier for the account.
        /// </summary>
        [Required]
        public int CurrencyId { get; set; }

        /// <summary>
        /// Initial balance for the account.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Initial balance must be non-negative.")]
        public decimal InitialBalance { get; set; }

        /// <summary>
        /// Interest rate (percentage) applied to the savings account.
        /// </summary>
        [Range(0, 100, ErrorMessage = "Interest rate must be between 0 and 100.")]
        public decimal InterestRate { get; set; }

        /// <summary>
        /// Type of interest calculation.
        /// </summary>
        public InterestType InterestType { get; set; }
    }
}
