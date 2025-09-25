using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Domain.Constant
{
    public enum AccessScope
    {
        Global,     // SuperAdmin
        BankLevel,  // Bank Admin or others
        Self        // Client
    }
}
