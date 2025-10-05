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
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        Task<AccessScope> GetScopeAsync();
    }
}

