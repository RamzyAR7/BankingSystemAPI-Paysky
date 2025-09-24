using BankingSystemAPI.Domain.Constant;
using System;
using System.ComponentModel.DataAnnotations;

namespace BankingSystemAPI.Application.DTOs.Account
{
    /// <summary>
    /// Generic request DTO for account creation/update. Uses enum numeric values for types.
    /// Implement specialized req DTOs (Checking/Savings) for additional fields.
    /// </summary>
    public class AccountReqDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public int CurrencyId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Initial balance must be non-negative.")]
        public decimal InitialBalance { get; set; }

        /// <summary>
        /// Numeric account type (use enum values). e.g. 1 for Savings, 2 for Checking.
        /// </summary>
        public AccountType AccountType { get; set; }
    }
}