using System.ComponentModel.DataAnnotations;

namespace BankingSystemAPI.Application.DTOs.Account
{
    /// <summary>
    /// DTO used to edit checking account properties (does not allow editing balance).
    /// </summary>
    public class CheckingAccountEditDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public int CurrencyId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Overdraft limit must be non-negative.")]
        public decimal OverdraftLimit { get; set; }
    }
}
