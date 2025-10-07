#region Usings
using System;
using System.ComponentModel.DataAnnotations;
#endregion


namespace BankingSystemAPI.Application.DTOs.Currency
{
    /// <summary>
    /// Request DTO for creating or updating a currency.
    /// </summary>
    public class CurrencyReqDto
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
        /// Currency code (e.g., USD).
        /// </summary>
        [Required]
        [StringLength(5, MinimumLength = 3)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if this is the base currency.
        /// </summary>
        public bool IsBase { get; set; }

        /// <summary>
        /// Exchange rate relative to base currency.
        /// </summary>
        [Range(0.000001, double.MaxValue, ErrorMessage = "Exchange rate must be positive.")]
        public decimal ExchangeRate { get; set; }
    }
}

