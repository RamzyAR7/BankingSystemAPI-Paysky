using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Domain.Constant
{
    public enum ControllerType
    {
        Auth = 1,
        User,
        Role,
        RoleClaims,
        UserRoles,
        Account,
        CheckingAccount,
        SavingsAccount,
        Currency,
        Transaction,
        Bank
    }
}
