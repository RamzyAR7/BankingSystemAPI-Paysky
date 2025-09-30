using System.ComponentModel.DataAnnotations;

namespace BankingSystemAPI.Application.DTOs.Account
{
    /// <summary>
    /// DTO used to edit checking account properties (does not allow editing balance).
    /// </summary>
    public class CheckingAccountEditDto
    {
        public string UserId { get; set; }

        public int CurrencyId { get; set; }
        public decimal OverdraftLimit { get; set; }
    }
}
