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
        public string UserId { get; set; }

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
