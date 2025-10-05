#region Usings
using BankingSystemAPI.Domain.Constant;
using System;
using System.ComponentModel.DataAnnotations;
#endregion


namespace BankingSystemAPI.Application.DTOs.Account
{
    /// <summary>
    /// Generic request DTO for account creation/update. Uses enum numeric values for types.
    /// Implement specialized req DTOs (Checking/Savings) for additional fields.
    /// </summary>
    public class AccountReqDto
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public string UserId { get; set; }

        public int CurrencyId { get; set; }

        public decimal InitialBalance { get; set; }

        /// <summary>
        /// Numeric account type (use enum values). e.g. 1 for Savings, 2 for Checking.
        /// </summary>
        public AccountType AccountType { get; set; }
    }
}
