using BankingSystemAPI.Domain.Constant;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.DTOs.Account
{
    /// <summary>
    /// Request DTO to create or update a savings account.
    /// </summary>
    public class SavingsAccountReqDto
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
        /// Interest rate (percentage) applied to the savings account.
        /// </summary>
        public decimal InterestRate { get; set; }

        /// <summary>
        /// Type of interest calculation.
        /// </summary>
        public InterestType InterestType { get; set; }
    }
}
