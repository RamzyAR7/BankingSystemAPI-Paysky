#region Usings
using System;
using System.ComponentModel.DataAnnotations;
#endregion


namespace BankingSystemAPI.Application.DTOs.Currency
{
    /// <summary>
    /// Currency details DTO.
    /// </summary>
    public class CurrencyDto
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
        /// Currency identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Currency code (e.g. USD).
        /// </summary>
        [Required]
        [StringLength(5, MinimumLength = 3)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if currency is base currency.
        /// </summary>
        public bool IsBase { get; set; }

        /// <summary>
        /// Exchange rate relative to base currency.
        /// </summary>
        [Range(0.000001, double.MaxValue)]
        public decimal ExchangeRate { get; set; }

        /// <summary>
        /// Indicates if the currency is active.
        /// </summary>
        public bool IsActive { get; set; }
    }
}

