#region Usings
using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Specifications.UserSpecifications
{
    public class RoleByIdSpecification : BaseSpecification<ApplicationRole>
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public RoleByIdSpecification(string id) : base(r => r.Id == id) { }
    }
}

