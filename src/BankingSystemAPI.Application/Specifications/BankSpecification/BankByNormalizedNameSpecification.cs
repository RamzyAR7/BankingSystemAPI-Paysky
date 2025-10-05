#region Usings
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Application.Specifications.BankSpecification
{
    public class BankByNormalizedNameSpecification : BaseSpecification<Bank>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public BankByNormalizedNameSpecification(string normalizedLower) : base(b => b.Name.ToLower() == normalizedLower)
        {
            AddInclude(b => b.ApplicationUsers);
        }
    }
}

