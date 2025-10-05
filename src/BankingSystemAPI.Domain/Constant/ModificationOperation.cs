#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Domain.Constant
{
    public enum UserModificationOperation
    {
        Edit,
        Delete,
        ChangePassword
    }
    public enum AccountModificationOperation
    {
        Deposit,
        Withdraw,
        Edit,
        Delete,
        Freeze,
        Unfreeze
    }
}

