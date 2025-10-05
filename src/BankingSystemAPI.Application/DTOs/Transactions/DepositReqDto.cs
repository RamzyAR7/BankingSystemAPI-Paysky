#region Usings
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.DTOs.Transactions
{
    /// <summary>
    /// Request DTO to deposit money into an account.
    /// </summary>
    public class DepositReqDto
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
        /// Target account identifier.
        /// </summary>
        [Required]
        public int AccountId { get; set; }

        /// <summary>
        /// Amount to deposit. Must be positive.
        /// </summary>
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }
}

