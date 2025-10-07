#region Usings
using BankingSystemAPI.Domain.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
#endregion


namespace BankingSystemAPI.Application.Interfaces.Repositories
{
    public interface IBankRepository : IGenericRepository<Bank, int>
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        Task<Dictionary<int, string>> GetBankNamesByIdsAsync(IEnumerable<int> ids);
    }
}

