#region Usings
using System.Linq.Expressions;
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Application.Specifications.TransactionSpecification
{
    public class TransactionByIdSpecification : BaseSpecification<Transaction>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public TransactionByIdSpecification(int transactionId) : base(t => t.Id == transactionId)
        {
            AddInclude(t => t.AccountTransactions);
        }
    }
}

