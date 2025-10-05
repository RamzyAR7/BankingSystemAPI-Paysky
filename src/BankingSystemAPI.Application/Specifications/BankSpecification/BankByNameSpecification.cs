#region Usings
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Application.Specifications.BankSpecification
{
    public class BankByNameSpecification : BaseSpecification<Bank>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public BankByNameSpecification(string name) : base(b => b.Name == name)
        {
            AddInclude(b => b.ApplicationUsers);
        }
    }
}

