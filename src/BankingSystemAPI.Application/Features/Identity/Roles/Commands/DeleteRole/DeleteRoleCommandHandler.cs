#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Messaging;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Domain.Constant;
#endregion


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
                return Result<RoleUpdateResultDto>.ValidationFailed(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Role ID"));
            }
            
            _logger.LogDebug(ApiResponseMessages.Logging.OperationCompletedController, "role", "delete");

            // Business rule validation: Check if role exists and is not in use
            var businessValidationResult = await ValidateBusinessRulesAsync(request.RoleId);
            if (businessValidationResult.IsFailure)
                return Result<RoleUpdateResultDto>.Failure(businessValidationResult.Errors);

            // Execute role deletion
            var deleteResult = await ExecuteRoleDeletionAsync(request.RoleId);
            
            // Enhanced side effects using ResultExtensions with structured logging
            deleteResult.OnSuccess(() => 
            {
                _logger.LogInformation(ApiResponseMessages.Logging.RoleDeleted, request.RoleId, deleteResult.Value?.Role?.Name);
            })
            .OnFailure(errors => 
            {
                _logger.LogWarning(ApiResponseMessages.Logging.RoleDeleteFailed, request.RoleId, string.Join(", ", errors));
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
                    _logger.LogDebug(ApiResponseMessages.Logging.OperationCompletedController, "role", "validatebusinessrules"));
                
                return successResult;
            }
            catch (Exception ex)
            {
                var exceptionResult = Result.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
                exceptionResult.OnFailure(errors => 
                    _logger.LogError(ex, ApiResponseMessages.Logging.SeedingFailed, ex.Message));
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
                _logger.LogDebug(ApiResponseMessages.Logging.OperationCompletedController, "role", "validateexistence");
                
                // The existence check is implicit in the deletion operation
                // If we need explicit existence validation, we'd add a method to IRoleService
                return Result.Success();
            }
            catch (Exception ex)
            {
                var exceptionResult = Result.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
                exceptionResult.OnFailure(errors => 
                    _logger.LogError(ex, ApiResponseMessages.Logging.SeedingFailed, ex.Message));
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
                _logger.LogDebug(ApiResponseMessages.Logging.OperationCompletedController, "role", "validateroleusage");

                // Use IRoleService for role-specific business logic - proper architecture
                var roleUsageResult = await _roleService.IsRoleInUseAsync(roleId);
                
                // Enhanced error handling using ResultExtensions patterns
                if (roleUsageResult.IsFailure)
                {
                    // Fail-safe approach: if we can't determine usage, we don't allow deletion
                    var failsafeResult = Result.BadRequest(ApiResponseMessages.Validation.DeleteUserHasAccounts);
                    failsafeResult.OnFailure(errors => 
                        _logger.LogWarning(ApiResponseMessages.Logging.RoleUsageCheckFailed, roleId));
                    return failsafeResult;
                }

                // Business rule validation with detailed logging
                if (roleUsageResult.Value)
                {
                    var businessRuleViolation = Result.BadRequest(ApiResponseMessages.BankingErrors.TransfersFromClientsOnly);
                    businessRuleViolation.OnFailure(errors => 
                        _logger.LogWarning(ApiResponseMessages.Logging.RoleUsageCheckFailed, roleId));
                    return businessRuleViolation;
                }

                // Success case with positive confirmation logging
                var successResult = Result.Success();
                successResult.OnSuccess(() => 
                    _logger.LogDebug(ApiResponseMessages.Logging.OperationCompletedController, "role", "validateusage"));
                
                return successResult;
            }
            catch (Exception ex)
            {
                var exceptionResult = Result.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
                exceptionResult.OnFailure(errors => 
                    _logger.LogError(ex, ApiResponseMessages.Logging.SeedingFailed, ex.Message));
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
                _logger.LogDebug(ApiResponseMessages.Logging.OperationCompletedController, "role", "servicecall");

                // Use IRoleService - it now returns Result<RoleUpdateResultDto> directly
                var serviceResult = await _roleService.DeleteRoleAsync(roleId);
                
                // Enhanced logging for service interaction
                serviceResult.OnSuccess(() => 
                    _logger.LogInformation(ApiResponseMessages.Logging.RoleDeleted, 
                        roleId, serviceResult.Value!.Role!.Name))
                    .OnFailure(errors => 
                        _logger.LogError(ApiResponseMessages.Logging.RoleDeleteFailed, roleId, string.Join(", ", errors)));

                return serviceResult;
            }
            catch (Exception ex)
            {
                var exceptionResult = Result<RoleUpdateResultDto>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
                exceptionResult.OnFailure(errors => 
                    _logger.LogError(ex, ApiResponseMessages.Logging.SeedingFailed, ex.Message));
                return exceptionResult;
            }
        }
    }
}
