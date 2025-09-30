using BankingSystemAPI.Domain.Constant;
using System.ComponentModel.DataAnnotations;

namespace BankingSystemAPI.Application.DTOs.Account
{
    /// <summary>
    /// DTO used to edit savings account properties (does not allow editing balance).
    /// </summary>
    public class SavingsAccountEditDto
    {
        public string UserId { get; set; }
        public int CurrencyId { get; set; }
        public decimal InterestRate { get; set; }
        public InterestType InterestType { get; set; }
    }
}
