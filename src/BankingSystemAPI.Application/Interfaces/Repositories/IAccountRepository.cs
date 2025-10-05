#region Usings
using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Interfaces.Repositories
{
    public interface IAccountRepository: IGenericRepository<Account, int>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        Task<IEnumerable<T>> GetAccountsByTypeAsync<T>(int pageNumber = 1, int pageSize = 10) where T : Account;
        Task<(IEnumerable<Account> Accounts, int TotalCount)> GetFilteredAccountsAsync(IQueryable<Account> query, int pageNumber, int pageSize);
        IQueryable<Account> Table { get; }
        IQueryable<Account> QueryByUserId(string userId);
        IQueryable<Account> QueryByNationalId(string nationalId);
    }
}

