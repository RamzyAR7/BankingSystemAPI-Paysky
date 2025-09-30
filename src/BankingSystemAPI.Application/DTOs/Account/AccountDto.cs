using BankingSystemAPI.Domain.Constant;
using System;
using System.ComponentModel.DataAnnotations;

namespace BankingSystemAPI.Application.DTOs.Account
{
    /// <summary>
    /// DTO representing a bank account (response model).
    /// </summary>
    public class AccountDto
    {
        /// <summary>
        /// Account identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Account number.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string AccountNumber { get; set; }

        /// <summary>
        /// Current account balance.
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// Date when the account was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Identifier of the owning user.
        /// </summary>
        [Required]
        public string UserId { get; set; }

        /// <summary>
        /// Currency code for the account (e.g. "USD").
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// Type of the account (e.g. "Checking", "Savings").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Indicates if the account is active.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
