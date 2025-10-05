#region Usings
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Application.Specifications.AccountSpecification
{
    public class CheckingAccountByIdSpecification : BaseSpecification<Account>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public CheckingAccountByIdSpecification(int id) : base(a => a.Id == id && a is CheckingAccount) { }
    }
}

