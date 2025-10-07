#region Usings
using BankingSystemAPI.Domain.Constant;
using System;
using System.ComponentModel.DataAnnotations;
#endregion


namespace BankingSystemAPI.Application.DTOs.Account
{
    /// <summary>
    /// DTO representing a bank account (response model).
    /// </summary>
    public class AccountDto
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        /// <summary>
        /// Account identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Account number.
        /// </summary>
        [Required]
        [StringLength(50)]
        public required string AccountNumber { get; set; } = string.Empty;

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
        public required string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Currency code for the account (e.g. "USD").
        /// </summary>
        public required string CurrencyCode { get; set; } = string.Empty;

        /// <summary>
        /// Type of the account (e.g. "Checking", "Savings").
        /// </summary>
        public required string Type { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the account is active.
        /// </summary>
        public bool IsActive { get; set; }
    }
}

