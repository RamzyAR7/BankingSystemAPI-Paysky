#region Usings
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.DTOs.Account
{
    /// <summary>
    /// Request DTO to create or update a checking account.
    /// </summary>
    public class CheckingAccountReqDto
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
        /// Identifier of the user who will own the account.
        /// </summary>
        public required string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Currency identifier for the account.
        /// </summary>
        public int CurrencyId { get; set; }

        /// <summary>
        /// Initial balance for the account.
        /// </summary>
        public decimal InitialBalance { get; set; }

        /// <summary>
        /// Overdraft limit allowed for checking account.
        /// </summary>
        public decimal OverdraftLimit { get; set; }
    }
}

