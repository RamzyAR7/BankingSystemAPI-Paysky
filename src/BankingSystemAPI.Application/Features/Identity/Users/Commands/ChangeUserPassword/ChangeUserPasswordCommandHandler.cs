using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;
using Microsoft.Extensions.Logging;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.ChangeUserPassword
{
    public sealed class ChangeUserPasswordCommandHandler : ICommandHandler<ChangeUserPasswordCommand, UserResDto>
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserAuthorizationService _userAuthorizationService;
        private readonly ILogger<ChangeUserPasswordCommandHandler> _logger;

        public ChangeUserPasswordCommandHandler(
            IUserService userService,
            ICurrentUserService currentUserService,
            ILogger<ChangeUserPasswordCommandHandler> logger,
            IUserAuthorizationService userAuthorizationService)
        {
            _userService = userService;
            _currentUserService = currentUserService;
            _userAuthorizationService = userAuthorizationService;
            _logger = logger;
        }

        public async Task<Result<UserResDto>> Handle(ChangeUserPasswordCommand request, CancellationToken cancellationToken)
        {
            // Validate authorization
            var authResult = await ValidateAuthorizationAsync(request.UserId);
            if (authResult.IsFailure)
                return Result<UserResDto>.Failure(authResult.Errors);

            // Get user context
            var contextResult = await GetPasswordChangeContextAsync(request.UserId);
            if (contextResult.IsFailure)
                return Result<UserResDto>.Failure(contextResult.Errors);

            // Validate business rules and execute password change
            var changeResult = await ValidateAndExecutePasswordChangeAsync(request, contextResult.Value!);
            
            // Add side effects using ResultExtensions
            changeResult.OnSuccess(() => 
                {
                    _logger.LogInformation("Password changed successfully for user: {UserId} by {ActorId}", 
                        request.UserId, _currentUserService.UserId);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("Password change failed for user: {UserId} by {ActorId}. Errors: {Errors}",
                        request.UserId, _currentUserService.UserId, string.Join(", ", errors));
                });

            return changeResult;
        }

        private async Task<Result> ValidateAuthorizationAsync(string userId)
        {
            try
            {
                var authResult = await _userAuthorizationService.CanModifyUserAsync(userId, UserModificationOperation.ChangePassword);
                return authResult;
            }
            catch (Exception ex)
            {
                return Result.Forbidden($"Authorization failed: {ex.Message}");
            }
        }

        private async Task<Result<PasswordChangeContext>> GetPasswordChangeContextAsync(string userId)
        {
            // Get acting user context
            var actingUserId = _currentUserService.UserId;
            var actingRole = await _currentUserService.GetRoleFromStoreAsync();
            
            // Get target user information
            var targetUserResult = await _userService.GetUserByIdAsync(userId);
            if (targetUserResult.IsFailure)
                return Result<PasswordChangeContext>.Failure(targetUserResult.Errors);

            var targetRoleResult = await _userService.GetUserRoleAsync(userId);
            if (targetRoleResult.IsFailure)
                return Result<PasswordChangeContext>.Failure(targetRoleResult.Errors);

            var context = new PasswordChangeContext
            {
                ActingUserId = actingUserId,
                ActingRole = actingRole.Name,
                TargetUserId = userId,
                TargetRole = targetRoleResult.Value,
                TargetUser = targetUserResult.Value!,
                IsSelf = !string.IsNullOrEmpty(actingUserId) && string.Equals(actingUserId, userId, StringComparison.OrdinalIgnoreCase)
            };

            return Result<PasswordChangeContext>.Success(context);
        }

        private async Task<Result<UserResDto>> ValidateAndExecutePasswordChangeAsync(ChangeUserPasswordCommand request, PasswordChangeContext context)
        {
            // Determine password change rules using functional approach
            var passwordRulesResult = DeterminePasswordRules(context);
            if (passwordRulesResult.IsFailure)
                return Result<UserResDto>.Failure(passwordRulesResult.Errors);

            var rules = passwordRulesResult.Value!;

            // Validate current password requirement
            if (rules.RequiresCurrentPassword && string.IsNullOrWhiteSpace(request.PasswordRequest.CurrentPassword))
                return Result<UserResDto>.BadRequest("Current password is required.");

            // Create ChangePasswordReqDto with business rules applied
            var changePasswordDto = new ChangePasswordReqDto
            {
                CurrentPassword = rules.RequiresCurrentPassword ? request.PasswordRequest.CurrentPassword : null,
                NewPassword = request.PasswordRequest.NewPassword,
                ConfirmNewPassword = request.PasswordRequest.ConfirmNewPassword
            };

            // Execute password change
            var result = await _userService.ChangeUserPasswordAsync(request.UserId, changePasswordDto);
            
            return result.IsSuccess
                ? Result<UserResDto>.Success(result.Value!)
                : Result<UserResDto>.Failure(result.Errors);
        }

        private Result<PasswordChangeRules> DeterminePasswordRules(PasswordChangeContext context)
        {
            var isSuperAdmin = string.Equals(context.ActingRole, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase);
            var isClient = string.Equals(context.ActingRole, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase);
            var isTargetClient = string.Equals(context.TargetRole, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase);

            // Business rule validation using ResultExtensions patterns
            var authorizationValidation = ValidatePasswordChangeAuthorization(context, isSuperAdmin, isClient, isTargetClient);
            if (authorizationValidation.IsFailure)
                return Result<PasswordChangeRules>.Failure(authorizationValidation.Errors);

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
            return Result.Forbidden("You are not authorized to change this user's password.");
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