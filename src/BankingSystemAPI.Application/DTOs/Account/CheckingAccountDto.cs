using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Application.DTOs.Account
{
    public class CheckingAccountDto : AccountDto
    {
        public decimal OverdraftLimit { get; set; }

        public CheckingAccountDto() => Type = AccountType.Checking.ToString();
    }
}
