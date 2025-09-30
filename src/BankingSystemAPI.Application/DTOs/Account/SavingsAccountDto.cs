using BankingSystemAPI.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.DTOs.Account
{
    public class SavingsAccountDto: AccountDto
    {
        public decimal InterestRate { get; set; }
        public string InterestType { get; set; }

        public SavingsAccountDto()
        {
            this.Type = AccountType.Savings.ToString();
        }
    }
}
