using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.DTOs.Account
{
    /// <summary>
    /// Request DTO to create or update a checking account.
    /// </summary>
    public class CheckingAccountReqDto
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
        /// Overdraft limit allowed for checking account.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Overdraft limit must be non-negative.")]
        public decimal OverdraftLimit { get; set; }
    }
}
