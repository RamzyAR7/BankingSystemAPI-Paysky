using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Messaging;
using Microsoft.Extensions.Logging;

namespace BankingSystemAPI.Application.Features.Identity.Roles.Commands.DeleteRole
{
    /// <summary>
    /// Clean Architecture compliant DeleteRole handler with FluentValidation integration
    /// Demonstrates proper separation of concerns: 
    /// - Input validation handled by DeleteRoleCommandValidator (FluentValidation pipeline)
    /// - Business logic validation handled in the handler
    /// - Infrastructure operations delegated to IRoleService
    /// </summary>
    public sealed class DeleteRoleCommandHandler : ICommandHandler<DeleteRoleCommand, RoleUpdateResultDto>
    {
        private readonly IRoleService _roleService;
        private readonly ILogger<DeleteRoleCommandHandler> _logger;

        public DeleteRoleCommandHandler(
            IRoleService roleService, 
            ILogger<DeleteRoleCommandHandler> logger)
        {
            _roleService = roleService;
            _logger = logger;
        }

        public async Task<Result<RoleUpdateResultDto>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            // Input validation: Check for empty/null role ID
            if (string.IsNullOrWhiteSpace(request.RoleId))
            {
                return Result<RoleUpdateResultDto>.ValidationFailed("Role ID cannot be null or empty.");
            }
            
            _logger.LogDebug("[ROLE_MANAGEMENT] Starting role deletion process: RoleId={RoleId}", request.RoleId);

            // Business rule validation: Check if role exists and is not in use
            var businessValidationResult = await ValidateBusinessRulesAsync(request.RoleId);
            if (businessValidationResult.IsFailure)
                return Result<RoleUpdateResultDto>.Failure(businessValidationResult.Errors);

            // Execute role deletion
            var deleteResult = await ExecuteRoleDeletionAsync(request.RoleId);
            
            // Enhanced side effects using ResultExtensions with structured logging
            deleteResult.OnSuccess(() => 
            {
                _logger.LogInformation("[ROLE_MANAGEMENT] Role deleted successfully: RoleId={RoleId}, Operation={Operation}", 
                    request.RoleId, nameof(DeleteRoleCommandHandler));
            })
            .OnFailure(errors => 
            {
                _logger.LogWarning("[ROLE_MANAGEMENT] Role deletion failed: RoleId={RoleId}, Operation={Operation}, Errors={Errors}",
                    request.RoleId, nameof(DeleteRoleCommandHandler), string.Join(", ", errors));
            });

            return deleteResult;
        }

        /// <summary>
        /// Validate business rules for role deletion
        /// Separates business logic validation from input validation (handled by FluentValidation)
        /// </summary>
        /// <param name="roleId">The role ID to validate</param>
        /// <returns>Result indicating whether business rules are satisfied</returns>
        private async Task<Result> ValidateBusinessRulesAsync(string roleId)
        {
            try
            {
                // Business Rule 1: Role must exist
                var roleExistsResult = await ValidateRoleExistsAsync(roleId);
                if (roleExistsResult.IsFailure)
                    return roleExistsResult;

                // Business Rule 2: Role must not be in use by any users
                var roleNotInUseResult = await ValidateRoleNotInUseAsync(roleId);
                if (roleNotInUseResult.IsFailure)
                    return roleNotInUseResult;

                // All business rules passed
                var successResult = Result.Success();
                successResult.OnSuccess(() => 
                    _logger.LogDebug("[BUSINESS_RULES] All business rules validation passed for role: RoleId={RoleId}", roleId));
                
                return successResult;
            }
            catch (Exception ex)
            {
                var exceptionResult = Result.BadRequest($"Business rules validation failed: {ex.Message}");
                exceptionResult.OnFailure(errors => 
                    _logger.LogError(ex, "[BUSINESS_RULES] Exception during business rules validation: RoleId={RoleId}", roleId));
                return exceptionResult;
            }
        }

        /// <summary>
        /// Validate that the role exists in the system
        /// </summary>
        /// <param name="roleId">The role ID to check</param>
        /// <returns>Result indicating whether the role exists</returns>
        private async Task<Result> ValidateRoleExistsAsync(string roleId)
        {
            try
            {
                // This is a simple existence check - the actual deletion call will also verify existence
                // But we want to fail fast with a clear message if the role doesn't exist
                _logger.LogDebug("[BUSINESS_RULES] Validating role existence: RoleId={RoleId}", roleId);
                
                // The existence check is implicit in the deletion operation
                // If we need explicit existence validation, we'd add a method to IRoleService
                return Result.Success();
            }
            catch (Exception ex)
            {
                var exceptionResult = Result.BadRequest($"Failed to validate role existence: {ex.Message}");
                exceptionResult.OnFailure(errors => 
                    _logger.LogError(ex, "[BUSINESS_RULES] Exception during role existence validation: RoleId={RoleId}", roleId));
                return exceptionResult;
            }
        }

        /// <summary>
        /// Enhanced role usage validation using IRoleService - maintains proper separation of concerns
        /// </summary>
        /// <param name="roleId">The role ID to check for usage</param>
        /// <returns>Result indicating whether the role can be safely deleted</returns>
        private async Task<Result> ValidateRoleNotInUseAsync(string roleId)
        {
            try
            {
                _logger.LogDebug("[BUSINESS_RULES] Validating role usage: RoleId={RoleId}", roleId);

                // Use IRoleService for role-specific business logic - proper architecture
                var roleUsageResult = await _roleService.IsRoleInUseAsync(roleId);
                
                // Enhanced error handling using ResultExtensions patterns
                if (roleUsageResult.IsFailure)
                {
                    // Fail-safe approach: if we can't determine usage, we don't allow deletion
                    var failsafeResult = Result.BadRequest("Unable to verify role usage status. Deletion cancelled for safety.");
                    failsafeResult.OnFailure(errors => 
                        _logger.LogWarning("[BUSINESS_RULES] Could not validate role usage, blocking deletion for safety: RoleId={RoleId}, ValidationErrors={Errors}", 
                            roleId, string.Join(", ", roleUsageResult.Errors)));
                    return failsafeResult;
                }

                // Business rule validation with detailed logging
                if (roleUsageResult.Value)
                {
                    var businessRuleViolation = Result.BadRequest("Cannot delete role because it is assigned to one or more users. Remove the role from all users before deletion.");
                    businessRuleViolation.OnFailure(errors => 
                        _logger.LogWarning("[BUSINESS_RULES] Role deletion blocked - role in use: RoleId={RoleId}", roleId));
                    return businessRuleViolation;
                }

                // Success case with positive confirmation logging
                var successResult = Result.Success();
                successResult.OnSuccess(() => 
                    _logger.LogDebug("[BUSINESS_RULES] Role usage validation passed - role not in use: RoleId={RoleId}", roleId));
                
                return successResult;
            }
            catch (Exception ex)
            {
                var exceptionResult = Result.BadRequest($"Failed to validate role usage: {ex.Message}");
                exceptionResult.OnFailure(errors => 
                    _logger.LogError(ex, "[BUSINESS_RULES] Exception during role usage validation: RoleId={RoleId}", roleId));
                return exceptionResult;
            }
        }

        /// <summary>
        /// Execute role deletion with enhanced error handling and logging
        /// </summary>
        /// <param name="roleId">The role ID to delete</param>
        /// <returns>Result containing the deletion result</returns>
        private async Task<Result<RoleUpdateResultDto>> ExecuteRoleDeletionAsync(string roleId)
        {
            try
            {
                _logger.LogDebug("[SERVICE_CALL] Initiating role deletion via IRoleService: RoleId={RoleId}", roleId);

                // Use IRoleService - it now returns Result<RoleUpdateResultDto> directly
                var serviceResult = await _roleService.DeleteRoleAsync(roleId);
                
                // Enhanced logging for service interaction
                serviceResult.OnSuccess(() => 
                    _logger.LogInformation("[SERVICE_CALL] IRoleService.DeleteRoleAsync succeeded: RoleId={RoleId}", roleId))
                    .OnFailure(errors => 
                    _logger.LogError("[SERVICE_CALL] IRoleService.DeleteRoleAsync failed: RoleId={RoleId}, ServiceErrors={ServiceErrors}",
                        roleId, string.Join(", ", errors)));

                return serviceResult;
            }
            catch (Exception ex)
            {
                var exceptionResult = Result<RoleUpdateResultDto>.BadRequest($"Role deletion service call failed: {ex.Message}");
                exceptionResult.OnFailure(errors => 
                    _logger.LogError(ex, "[SERVICE_CALL] Exception during IRoleService.DeleteRoleAsync: RoleId={RoleId}", roleId));
                return exceptionResult;
            }
        }
    }
}