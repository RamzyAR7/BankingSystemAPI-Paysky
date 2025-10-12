#region Usings
using BankingSystemAPI.Domain.Constant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Interfaces.Authorization
{
    public interface IScopeResolver
    {
        Task<AccessScope> GetScopeAsync();
    }
}

