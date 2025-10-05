#region Usings
using BankingSystemAPI.Domain.Entities;
using System;
using System.Linq.Expressions;
#endregion


namespace BankingSystemAPI.Application.Specifications.CurrencySpecification
{
    public class CurrencyBaseSpecification : BaseSpecification<Currency>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public CurrencyBaseSpecification()
            : base(c => c.IsBase)
        {
        }

        public CurrencyBaseSpecification(int excludeId)
            : base(c => c.IsBase && c.Id != excludeId)
        {
        }
    }
}

