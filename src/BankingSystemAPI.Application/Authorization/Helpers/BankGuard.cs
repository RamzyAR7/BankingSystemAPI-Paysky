using BankingSystemAPI.Application.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Authorization.Helpers
{
    public static class BankGuard
    {
        public static void EnsureSameBank(int? actingBankId, int? targetBankId)
        {
            if (actingBankId == null || targetBankId == null || actingBankId != targetBankId)
                throw new ForbiddenException("Access forbidden due to bank isolation.");
        }
    }
}
