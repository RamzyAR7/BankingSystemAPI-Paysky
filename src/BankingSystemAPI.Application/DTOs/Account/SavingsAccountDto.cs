#region Usings
using BankingSystemAPI.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.DTOs.Account
{
    public class SavingsAccountDto: AccountDto
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public decimal InterestRate { get; set; }
        public string InterestType { get; set; }

        public SavingsAccountDto()
        {
            this.Type = AccountType.Savings.ToString();
        }
    }
}

