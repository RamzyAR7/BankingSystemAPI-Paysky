#region Usings
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    public class SavingsAccountByIdSpecification : BaseSpecification<Account>
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        public SavingsAccountByIdSpecification(int id) : base(a => a.Id == id && a is SavingsAccount) { }
    }
}

