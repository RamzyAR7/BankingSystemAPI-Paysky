#region Usings
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Application.Specifications.BankSpecification
{
    public class BankByIdSpecification : BaseSpecification<Bank>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public BankByIdSpecification(int id) : base(b => b.Id == id)
        {
            AddInclude(b => b.ApplicationUsers);
        }
    }
}

