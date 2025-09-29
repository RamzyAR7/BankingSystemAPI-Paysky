using System.Linq.Expressions;
using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Specifications.TransactionSpecification
{
    public class TransactionByIdSpecification : BaseSpecification<Transaction>
    {
        public TransactionByIdSpecification(int transactionId) : base(t => t.Id == transactionId)
        {
            AddInclude(t => t.AccountTransactions);
        }
    }
}
