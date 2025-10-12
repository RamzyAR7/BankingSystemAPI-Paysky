#region Usings
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Domain.Constant;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#endregion


namespace BankingSystemAPI.Application.Authorization.Helpers
{
    public class ScopeResolver : IScopeResolver
    {
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<ScopeResolver> _logger;

        public ScopeResolver(ICurrentUserService currentUser, ILogger<ScopeResolver> logger)
        {
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<AccessScope> GetScopeAsync()
        {
            try
            {
                var role = await _currentUser.GetRoleFromStoreAsync();
                var scope = DetermineAccessScope(role.Name);

                // Use ResultExtensions for consistent logging patterns
                var result = Result<AccessScope>.Success(scope);
                result.OnSuccess(() =>
                {
                    _logger.LogDebug(ApiResponseMessages.Logging.ScopeResolved,
                        _currentUser.UserId, role.Name, scope);
                });

                return scope;
            }
            catch (Exception ex)
            {
                // Use ResultExtensions for error handling
                var errorResult = Result<AccessScope>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
                errorResult.OnFailure(errors =>
                    {
                        _logger.LogError(ex, ApiResponseMessages.Logging.ScopeResolveFailed, _currentUser.UserId, ex.Message);
                    });

                // Default to most restrictive scope on error
                return AccessScope.Self;
            }
        }

        private AccessScope DetermineAccessScope(string? roleName)
        {
            // Use functional approach with ResultExtensions patterns
            var scopeResult = ProcessRoleToScope(roleName);

            scopeResult.OnSuccess(() =>
            {
                _logger.LogDebug("[AUTHORIZATION] Role processed successfully: {Role} -> {Scope}",
                    roleName, scopeResult.Value);
            })
            .OnFailure(errors =>
            {
                _logger.LogWarning("[AUTHORIZATION] Role processing failed: {Role}, using default scope", roleName);
            });

            return scopeResult ? scopeResult.Value : AccessScope.Self;
        }

        private Result<AccessScope> ProcessRoleToScope(string? roleName)
        {
            if (string.IsNullOrEmpty(roleName))
                return Result<AccessScope>.BadRequest("No role provided").Map(_ => AccessScope.Self);

            if (RoleHelper.IsSuperAdmin(roleName))
                return Result<AccessScope>.Success(AccessScope.Global);

            if (RoleHelper.IsClient(roleName))
                return Result<AccessScope>.Success(AccessScope.Self);

            // Default to bank level for other roles (Admin, etc.)
            return Result<AccessScope>.Success(AccessScope.BankLevel);
        }
    }
}

