#region Usings
using Microsoft.AspNetCore.Http;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Application.Interfaces.Identity;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Authorization
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CurrentUserService> _logger;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public string? UserId => GetUserIdWithLogging();

        public int? BankId => GetBankIdWithLogging();

        public bool IsAuthenticated => GetAuthenticationStatusWithLogging();

        public Task<ApplicationRole> GetRoleFromStoreAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var roleName = user?.FindFirst("role")?.Value;
            if (string.IsNullOrEmpty(roleName))
            {
                roleName = user?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            }

            var role = new ApplicationRole { Name = roleName };
            
            // Add side effects using ResultExtensions patterns
            var result = Result.Success();
            result.OnSuccess(() => 
                {
                    if (!string.IsNullOrEmpty(roleName))
                        _logger.LogDebug("[AUTHORIZATION] Role retrieved from store: {RoleName}", roleName);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("[AUTHORIZATION] Failed to retrieve role from store");
                });

            return Task.FromResult(role);
        }

        public Task<bool> IsInRoleAsync(string roleName)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || string.IsNullOrEmpty(roleName))
            {
                var result = Result<bool>.Success(false);
                result.OnSuccess(() => _logger.LogDebug("[AUTHORIZATION] Role check failed - no user or role name"));
                return Task.FromResult(false);
            }

            var inRole = user.Claims.Any(c => 
                (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role") && 
                string.Equals(c.Value, roleName, System.StringComparison.OrdinalIgnoreCase));

            // Add side effects using ResultExtensions patterns
            var roleCheckResult = Result<bool>.Success(inRole);
            roleCheckResult.OnSuccess(() => 
                {
                    _logger.LogDebug("[AUTHORIZATION] Role check completed: UserId={UserId}, Role={RoleName}, IsInRole={IsInRole}", 
                        UserId, roleName, inRole);
                });

            return Task.FromResult(inRole);
        }

        private string? GetUserIdWithLogging()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;
            
            // Use ResultExtensions for consistent logging patterns
            var result = userId.ToResult("User ID not found in claims");
            result.OnSuccess(() => _logger.LogDebug("[AUTHORIZATION] User ID retrieved: {UserId}", userId));
            
            return userId;
        }

        private int? GetBankIdWithLogging()
        {
            var bankClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("bankid")?.Value;
            
            if (int.TryParse(bankClaim, out var bankId))
            {
                var result = Result<int>.Success(bankId);
                result.OnSuccess(() => _logger.LogDebug("[AUTHORIZATION] Bank ID retrieved: {BankId}", bankId));
                return bankId;
            }

            // Log when bank ID is not available or invalid
            if (!string.IsNullOrEmpty(bankClaim))
            {
                var result = Result<int>.BadRequest("Invalid bank ID format");
                result.OnFailure(errors => _logger.LogWarning("[AUTHORIZATION] Invalid bank ID format in claims: {BankClaim}", bankClaim));
            }

            return null;
        }

        private bool GetAuthenticationStatusWithLogging()
        {
            var isAuthenticated = _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
            
            // Use ResultExtensions patterns for consistent logging
            var result = Result<bool>.Success(isAuthenticated);
            result.OnSuccess(() => 
                {
                    if (isAuthenticated)
                        _logger.LogDebug("[AUTHORIZATION] User is authenticated: {UserId}", UserId);
                    else
                        _logger.LogDebug("[AUTHORIZATION] User is not authenticated");
                });

            return isAuthenticated;
        }
    }
}

