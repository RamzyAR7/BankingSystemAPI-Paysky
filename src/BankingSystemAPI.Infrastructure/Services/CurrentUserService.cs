using BankingSystemAPI.Application.Interfaces.Identity;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BankingSystemAPI.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;

        public int? BankId
        {
            get
            {
                var bankClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("bankid")?.Value;
                if (int.TryParse(bankClaim, out var b)) return b;
                return null;
            }
        }

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public Task<string?> GetRoleFromStoreAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var primary = user?.FindFirst("role")?.Value;
            if (!string.IsNullOrEmpty(primary)) return Task.FromResult<string?>(primary);

            var fromRoleClaim = user?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(fromRoleClaim)) return Task.FromResult<string?>(fromRoleClaim);

            return Task.FromResult<string?>(null);
        }

        public Task<bool> IsInRoleAsync(string roleName)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || string.IsNullOrEmpty(roleName)) return Task.FromResult(false);

            var inRole = user.Claims.Any(c => (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role") && string.Equals(c.Value, roleName, System.StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(inRole);
        }
    }
}
