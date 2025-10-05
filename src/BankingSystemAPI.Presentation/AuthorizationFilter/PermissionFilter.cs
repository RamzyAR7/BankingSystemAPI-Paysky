#region Usings
using BankingSystemAPI.Domain.Constant;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Presentation.AuthorizationFilter
{
    public class PermissionFilter : IAsyncAuthorizationFilter
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        private readonly string _permission;
        public PermissionFilter(string permission)
        {
            _permission = permission;
        }

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user?.Identity is not { IsAuthenticated: true })
            {
                context.Result = new ObjectResult(new { message = ApiResponseMessages.ErrorPatterns.NotAuthenticated })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return Task.CompletedTask;
            }
            var hasPermission = user.Claims.Any(c => string.Equals(c.Type, "Permission", StringComparison.OrdinalIgnoreCase) && c.Value == _permission);

            if (!hasPermission)
            {
                var msg = string.Format(ApiResponseMessages.ErrorPatterns.MissingPermissionFormat, _permission);
                context.Result = new ObjectResult(new { message = msg })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}

