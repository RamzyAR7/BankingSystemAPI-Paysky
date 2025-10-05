#region Usings
using Microsoft.AspNetCore.Mvc.Filters;
using System;
#endregion


namespace BankingSystemAPI.Presentation.AuthorizationFilter
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class PermissionFilterFactory : Attribute, IFilterFactory
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        public bool IsReusable => false;
        private readonly string _permission;

        public PermissionFilterFactory(string permission)
        {
            _permission = permission;
        }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new PermissionFilter(_permission);
        }
    }
}

