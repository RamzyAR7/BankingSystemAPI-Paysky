using BankingSystemAPI.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace BankingSystemAPI.Presentation.AuthorizationFilter
{
    public class PermissionFilter : IAsyncAuthorizationFilter
    {
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
                throw new UnauthorizedException("User is not authenticated.");
            }

            var hasPermission = user.Claims.Any(c => string.Equals(c.Type, "Permission", System.StringComparison.OrdinalIgnoreCase) && c.Value == _permission);
            if (!hasPermission)
            {
                throw new ForbiddenException($"Missing required permission: {_permission}");
            }

            return Task.CompletedTask;
        }
    }
}
