#region Usings
using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Interfaces.Repositories
{
    public interface ITransactionRepository : IGenericRepository<Transaction, int>
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        IQueryable<Transaction> QueryWithAccountTransactions();
        IQueryable<Transaction> QueryByAccountId(int accountId);
    }
}

