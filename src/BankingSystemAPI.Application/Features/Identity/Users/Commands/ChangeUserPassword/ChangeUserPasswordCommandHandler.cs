#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;
using Microsoft.Extensions.Logging;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.ChangeUserPassword
{
    public sealed class ChangeUserPasswordCommandHandler : ICommandHandler<ChangeUserPasswordCommand, UserResDto>
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserAuthorizationService _userAuthorization_service;
        private readonly ILogger<ChangeUserPasswordCommandHandler> _logger;

        public ChangeUserPasswordCommandHandler(
            IUserService userService,
            ICurrentUserService currentUserService,
            ILogger<ChangeUserPasswordCommandHandler> logger,
            IUserAuthorizationService userAuthorizationService)
        {
            _userService = userService;
            _currentUserService = currentUserService;
            _userAuthorization_service = userAuthorizationService;
            _logger = logger;
        }

        public async Task<Result<UserResDto>> Handle(ChangeUserPasswordCommand request, CancellationToken cancellationToken)
        {
            // Validate authorization
            var authResult = await ValidateAuthorizationAsync(request.UserId);
            if (authResult.IsFailure)
            {
                // Use structured logging with matching placeholders to avoid FormatException
                _logger.LogWarning("Password change authorization failed for user {TargetUserId} by {ActorId}: {Errors}",
                    request.UserId, _currentUserService.UserId, string.Join(", ", authResult.Errors));
                return Result<UserResDto>.Failure(authResult.ErrorItems);
            }

            // Get user context
            var contextResult = await GetPasswordChangeContextAsync(request.UserId);
            if (contextResult.IsFailure)
            {
                _logger.LogWarning("Failed to get password change context for user {TargetUserId}: {Errors}",
                    request.UserId, string.Join(", ", contextResult.Errors));
                return Result<UserResDto>.Failure(contextResult.ErrorItems);
            }

            // Validate business rules and execute password change
            var changeResult = await ValidateAndExecutePasswordChangeAsync(request, contextResult.Value!);
            
            // Add side effects using ResultExtensions
            changeResult.OnSuccess(() => 
            {
                _logger.LogInformation(ApiResponseMessages.Logging.PasswordChanged, request.UserId);
            })
            .OnFailure(errors => 
            {
                _logger.LogWarning(ApiResponseMessages.Logging.PasswordChangeFailed, request.UserId, string.Join(", ", errors));
            });

            return changeResult;
        }

        private async Task<Result> ValidateAuthorizationAsync(string userId)
        {
            try
            {
                var authResult = await _userAuthorization_service.CanModifyUserAsync(userId, UserModificationOperation.ChangePassword);
                return authResult;
            }
            catch (Exception)
            {
                return Result.Forbidden(ApiResponseMessages.Validation.NotAuthorizedToChangePassword);
            }
        }

        private async Task<Result<PasswordChangeContext>> GetPasswordChangeContextAsync(string userId)
        {
            // Get acting user context
            var actingUserId = _currentUserService.UserId;
            var actingRole = await _currentUserService.GetRoleFromStoreAsync();
            var actingRoleName = actingRole?.Name ?? string.Empty;
            var actingBankId = _currentUserService.BankId;
            
            // Get target user information
            var targetUserResult = await _userService.GetUserByIdAsync(userId);
            if (targetUserResult.IsFailure)
                return Result<PasswordChangeContext>.Failure(targetUserResult.ErrorItems);

            var targetRoleResult = await _userService.GetUserRoleAsync(userId);
            if (targetRoleResult.IsFailure)
                return Result<PasswordChangeContext>.Failure(targetRoleResult.ErrorItems);

            var targetUser = targetUserResult.Value!;
            var targetRoleName = targetRoleResult.Value;

            var isSelf = !string.IsNullOrEmpty(actingUserId) && string.Equals(actingUserId, userId, StringComparison.OrdinalIgnoreCase);

            // Enforce additional policy: Admins can only change their own password or reset client passwords in same bank
            var isActingAdmin = string.Equals(actingRoleName, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
            var isTargetAdmin = string.Equals(targetRoleName, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
            var isTargetClient = string.Equals(targetRoleName, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase);

            if (isActingAdmin && !isSelf)
            {
                // Admin cannot change another admin's password
                if (isTargetAdmin)
                {
                    return Result<PasswordChangeContext>.Forbidden(ApiResponseMessages.Validation.NotAuthorizedToChangePassword);
                }

                // Admin can only change client passwords within the same bank
                if (!isTargetClient || actingBankId != targetUser.BankId)
                {
                    return Result<PasswordChangeContext>.Forbidden(ApiResponseMessages.Validation.NotAuthorizedToChangePassword);
                }
            }

            var context = new PasswordChangeContext
            {
                ActingUserId = actingUserId,
                ActingRole = actingRoleName,
                TargetUserId = userId,
                TargetRole = targetRoleName,
                TargetUser = targetUser,
                IsSelf = isSelf
            };

            return Result<PasswordChangeContext>.Success(context);
        }

        private async Task<Result<UserResDto>> ValidateAndExecutePasswordChangeAsync(ChangeUserPasswordCommand request, PasswordChangeContext context)
        {
            // Determine password change rules using functional approach
            var passwordRulesResult = DeterminePasswordRules(context);
            if (passwordRulesResult.IsFailure)
                return Result<UserResDto>.Failure(passwordRulesResult.ErrorItems);

            var rules = passwordRulesResult.Value!;

            // Validate current password requirement
            if (rules.RequiresCurrentPassword && string.IsNullOrWhiteSpace(request.PasswordRequest.CurrentPassword))
                return Result<UserResDto>.BadRequest(ApiResponseMessages.Validation.CurrentPasswordRequired);

            // Validate password confirmation early
            if (request.PasswordRequest.NewPassword != request.PasswordRequest.ConfirmNewPassword)
                return Result<UserResDto>.BadRequest(ApiResponseMessages.Validation.PasswordsDoNotMatch);

            // Create ChangePasswordReqDto with business rules applied
            var changePasswordDto = new ChangePasswordReqDto
            {
                CurrentPassword = rules.RequiresCurrentPassword ? request.PasswordRequest.CurrentPassword : null,
                NewPassword = request.PasswordRequest.NewPassword,
                ConfirmNewPassword = request.PasswordRequest.ConfirmNewPassword
            };

            // Execute password change
            var result = await _userService.ChangeUserPasswordAsync(request.UserId, changePasswordDto);
            
            // Enhanced error handling for password change failures
            if (result.IsFailure)
            {
                // Log the original error for debugging purposes
                _logger.LogWarning(ApiResponseMessages.Logging.PasswordChangeFailed, request.UserId, string.Join(", ", result.Errors));

                // Check for common password change error patterns and provide specific messages
                // Optionally, you can still enhance errors, but Result<UserResDto>.Failure expects ResultError, not string
                return Result<UserResDto>.Failure(result.ErrorItems);
            }

            return Result<UserResDto>.Success(result.Value!);
        }

        private Result<PasswordChangeRules> DeterminePasswordRules(PasswordChangeContext context)
        {
            var isSuperAdmin = string.Equals(context.ActingRole, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase);
            var isClient = string.Equals(context.ActingRole, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase);
            var isTargetClient = string.Equals(context.TargetRole, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase);

            // Business rule validation using ResultExtensions patterns
            var authorizationValidation = ValidatePasswordChangeAuthorization(context, isSuperAdmin, isClient, isTargetClient);
            if (authorizationValidation.IsFailure)
                return Result<PasswordChangeRules>.Failure(authorizationValidation.ErrorItems);

            var rules = new PasswordChangeRules
            {
                RequiresCurrentPassword = DetermineCurrentPasswordRequirement(context, isSuperAdmin, isClient, isTargetClient)
            };

            return Result<PasswordChangeRules>.Success(rules);
        }

        private Result ValidatePasswordChangeAuthorization(PasswordChangeContext context, bool isSuperAdmin, bool isClient, bool isTargetClient)
        {
            // SuperAdmin can change any password
            if (isSuperAdmin) return Result.Success();

            // Self-change is always allowed
            if (context.IsSelf) return Result.Success();

            // Non-client roles can change client passwords
            if (!isClient && isTargetClient) return Result.Success();

            // All other cases are unauthorized
            return Result.Forbidden(ApiResponseMessages.Validation.NotAuthorizedToChangePassword);
        }

        private bool DetermineCurrentPasswordRequirement(PasswordChangeContext context, bool isSuperAdmin, bool isClient, bool isTargetClient)
        {
            // SuperAdmin can reset any password without current password
            if (isSuperAdmin) return false;

            // Admin/non-client can reset client passwords without current password
            if (!isClient && !isSuperAdmin && isTargetClient && !context.IsSelf) return false;

            // Self-change requires current password
            if (context.IsSelf) return true;

            // Default to requiring current password for security
            return true;
        }

        /// <summary>
        /// Enhances password change error messages with more specific user-friendly messages
        /// </summary>
        private IEnumerable<string> EnhancePasswordChangeErrors(IReadOnlyList<string> originalErrors, PasswordChangeContext context, PasswordChangeRules rules)
        {
            var enhancedErrors = new List<string>();

            foreach (var error in originalErrors)
            {
                // ASP.NET Identity returns "Incorrect password." for wrong current password
                if (error.Equals("Incorrect password.", StringComparison.OrdinalIgnoreCase))
                {
                    enhancedErrors.Add(ApiResponseMessages.Validation.IncorrectCurrentPassword);
                }
                // Keep other errors as-is for simplicity
                else
                {
                    enhancedErrors.Add(error);
                }
            }

            return enhancedErrors.Any() ? enhancedErrors : originalErrors;
        }

        private class PasswordChangeContext
        {
            public string? ActingUserId { get; set; }
            public string ActingRole { get; set; } = null!;
            public string TargetUserId { get; set; } = null!;
            public string? TargetRole { get; set; }
            public UserResDto TargetUser { get; set; } = null!;
            public bool IsSelf { get; set; }
        }

        private class PasswordChangeRules
        {
            public bool RequiresCurrentPassword { get; set; }
        }
    }
}
