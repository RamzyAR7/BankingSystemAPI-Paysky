using Microsoft.AspNetCore.Http;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Interfaces.Identity;
using System.Linq;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Authorization
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

        public Task<ApplicationRole> GetRoleFromStoreAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var roleName = user?.FindFirst("role")?.Value;
            if (string.IsNullOrEmpty(roleName))
            {
                roleName = user?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            }
            return Task.FromResult(new ApplicationRole { Name = roleName });
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
