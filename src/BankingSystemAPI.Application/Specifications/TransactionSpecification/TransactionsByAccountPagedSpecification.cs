#region Usings
using BankingSystemAPI.Domain.Entities;
using System;
using System.Linq.Expressions;
#endregion


namespace BankingSystemAPI.Application.Specifications.TransactionSpecification
{
    public class TransactionsByAccountPagedSpecification : PagedSpecification<Transaction>
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        public TransactionsByAccountPagedSpecification(int accountId, int skip, int take, string? orderBy = null, string? orderDir = null)
            : base(t => t.AccountTransactions.Any(at => at.AccountId == accountId), skip, take, orderBy ?? "Timestamp", orderDir ?? "DESC", t => t.AccountTransactions)
        {

        }
    }
}

