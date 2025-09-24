using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.DTOs.InterestLog
{
    public class InterestLogDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public int SavingsAccountId { get; set; }
        public string SavingsAccountNumber { get; set; }
    }
}
