#region Usings
using BankingSystemAPI.Domain.Entities;
#endregion


namespace BankingSystemAPI.Application.Specifications.UserSpecifications
{
    public class RoleByNameSpecification : BaseSpecification<ApplicationRole>
    {
        #region Fields
        #endregion

        #region Constructors
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion
        public RoleByNameSpecification(string name) : base(r => r.Name == name) { }
    }
}

