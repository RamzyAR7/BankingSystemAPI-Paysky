using BankingSystemAPI.Domain.Constant;
using System.ComponentModel.DataAnnotations;

namespace BankingSystemAPI.Application.DTOs.Account
{
    /// <summary>
    /// DTO used to edit savings account properties (does not allow editing balance).
    /// </summary>
    public class SavingsAccountEditDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public int CurrencyId { get; set; }

        [Range(0, 100, ErrorMessage = "Interest rate must be between 0 and 100.")]
        public decimal InterestRate { get; set; }

        public InterestType InterestType { get; set; }
    }
}
