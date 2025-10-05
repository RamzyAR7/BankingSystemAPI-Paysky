#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.DTOs.Account
{
    public class CheckingAccountDto : AccountDto
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public decimal OverdraftLimit { get; set; }

        public CheckingAccountDto() => Type = AccountType.Checking.ToString();
    }
}

