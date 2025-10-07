#region Usings
using BankingSystemAPI.Domain.Entities;
using System.Linq.Expressions;
#endregion


namespace BankingSystemAPI.Application.Specifications.CurrencySpecification
{
    public class CurrencyByIdSpecification : BaseSpecification<Currency>
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        public CurrencyByIdSpecification(int id) : base(c => c.Id == id)
        {
        }
    }
}

