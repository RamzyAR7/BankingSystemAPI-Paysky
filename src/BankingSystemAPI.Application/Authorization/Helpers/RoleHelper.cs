#region Usings
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
    public static class RoleHelper
    {
        private static ILogger? _logger;

        // Allow setting logger for enhanced logging capabilities
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        public static bool IsRole(this string? role, UserRole expectedRole)
        {
            var isMatch = string.Equals(role, expectedRole.ToString(), StringComparison.OrdinalIgnoreCase);

            // Use ResultExtensions patterns for consistent logging
            var result = Result<bool>.Success(isMatch);
            result.OnSuccess(() =>
            {
                _logger?.LogDebug(ApiResponseMessages.Logging.RoleCheck, role, expectedRole, isMatch);
            });

            return isMatch;
        }

        public static bool IsSuperAdmin(this string? role)
        {
            var result = ValidateRoleInput(role, UserRole.SuperAdmin);
            return result.IsSuccess ? result.Value : false;
        }

        public static bool IsClient(this string? role)
        {
            var result = ValidateRoleInput(role, UserRole.Client);
            return result.IsSuccess ? result.Value : false;
        }

        public static bool IsBankAdmin(this string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                var result = Result<bool>.Success(false);
                result.OnSuccess(() => _logger?.LogDebug(ApiResponseMessages.Logging.BankAdminCheckFailed));
                return false;
            }

            var isNotClientOrSuperAdmin = !role.IsRole(UserRole.Client) && !role.IsRole(UserRole.SuperAdmin);

            // Use ResultExtensions for consistent logging
            var validationResult = Result<bool>.Success(isNotClientOrSuperAdmin);
            validationResult.OnSuccess(() =>
                {
                    _logger?.LogDebug(ApiResponseMessages.Logging.BankAdminCheck, role, isNotClientOrSuperAdmin);
                });

            return isNotClientOrSuperAdmin;
        }

        /// <summary>
        /// Enhanced role validation with ResultExtensions patterns
        /// </summary>
        public static Result<bool> ValidateRoleAsync(string? role, UserRole expectedRole)
        {
            var validation = ValidateRoleInput(role, expectedRole);

            validation.OnSuccess(() =>
                {
                    _logger?.LogDebug(ApiResponseMessages.Logging.RoleValidationSuccessful, role, expectedRole);
                });

            return validation;
        }

        /// <summary>
        /// Validate multiple roles using ResultExtensions composition
        /// </summary>
        public static Result ValidateMultipleRoles(string? userRole, params UserRole[] allowedRoles)
        {
            if (allowedRoles == null || !allowedRoles.Any())
                return Result.BadRequest("No allowed roles specified");

            var roleValidations = allowedRoles.Select(role =>
                userRole.IsRole(role) ? Result.Success() : Result.BadRequest($"Role {userRole} is not {role}"))
                .ToArray();

            // Use ResultExtensions.ValidateAll for at least one success (OR logic)
            var hasValidRole = roleValidations.Any(r => r.IsSuccess);
            var result = hasValidRole ? Result.Success() : Result.BadRequest($"Role {userRole} is not in allowed roles: {string.Join(", ", allowedRoles)}");

            result.OnSuccess(() =>
                {
                    _logger?.LogDebug("[AUTHORIZATION] Multi-role validation passed: {UserRole} is in {AllowedRoles}",
                        userRole, string.Join(", ", allowedRoles));
                })
                .OnFailure(errors =>
                {
                    _logger?.LogWarning("[AUTHORIZATION] Multi-role validation failed: {UserRole} not in {AllowedRoles}",
                        userRole, string.Join(", ", allowedRoles));
                });

            return result;
        }

        private static Result<bool> ValidateRoleInput(string? role, UserRole expectedRole)
        {
            if (string.IsNullOrWhiteSpace(role))
                return Result<bool>.Success(false);

            var isMatch = string.Equals(role, expectedRole.ToString(), StringComparison.OrdinalIgnoreCase);
            return Result<bool>.Success(isMatch);
        }
    }
}

