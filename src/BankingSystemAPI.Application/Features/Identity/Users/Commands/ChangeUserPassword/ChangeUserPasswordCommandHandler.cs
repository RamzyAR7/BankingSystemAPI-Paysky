using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.ChangeUserPassword
{
    public sealed class ChangeUserPasswordCommandHandler : ICommandHandler<ChangeUserPasswordCommand, UserResDto>
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserAuthorizationService? _userAuthorizationService;

        public ChangeUserPasswordCommandHandler(
            IUserService userService,
            ICurrentUserService currentUserService,
            IUserAuthorizationService? userAuthorizationService = null)
        {
            _userService = userService;
            _currentUserService = currentUserService;
            _userAuthorizationService = userAuthorizationService;
        }

        public async Task<Result<UserResDto>> Handle(ChangeUserPasswordCommand request, CancellationToken cancellationToken)
        {
            // Authorization check - let exceptions bubble up to middleware
            if (_userAuthorizationService != null)
            {
                await _userAuthorizationService.CanModifyUserAsync(request.UserId, UserModificationOperation.ChangePassword);
            }

            // Business logic: Determine password change rules based on user roles
            var actingUserId = _currentUserService.UserId;
            var actingRole = await _currentUserService.GetRoleFromStoreAsync();
            var isSuperAdmin = string.Equals(actingRole.Name, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase);
            var isClient = string.Equals(actingRole.Name, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase);
            var isSelf = !string.IsNullOrEmpty(actingUserId) && string.Equals(actingUserId, request.UserId, StringComparison.OrdinalIgnoreCase);

            // Get target user to check their role - will fail if user doesn't exist
            var targetUserResult = await _userService.GetUserByIdAsync(request.UserId);
            if (!targetUserResult.Succeeded)
            {
                return Result<UserResDto>.Failure(targetUserResult.Errors);
            }

            var targetRoleResult = await _userService.GetUserRoleAsync(request.UserId);
            if (!targetRoleResult.Succeeded)
            {
                return Result<UserResDto>.Failure(targetRoleResult.Errors);
            }

            var targetRole = targetRoleResult.Value;
            var isTargetClient = string.Equals(targetRole, UserRole.Client.ToString(), StringComparison.OrdinalIgnoreCase);

            // Business validation: Determine if current password is required
            string? currentPassword = request.PasswordRequest.CurrentPassword;

            // SuperAdmin can reset any password without current password
            if (isSuperAdmin)
            {
                currentPassword = null; // Force password reset
            }
            // Admin/non-client can reset client passwords without current password
            else if (!isClient && !isSuperAdmin && isTargetClient && !isSelf)
            {
                currentPassword = null; // Force password reset
            }
            // Self-change requires current password
            else if (isSelf && string.IsNullOrWhiteSpace(currentPassword))
            {
                return Result<UserResDto>.Failure(new[] { "Current password is required." });
            }
            // Only authorized roles can change passwords
            else if (!isSelf && !isSuperAdmin && !(isTargetClient && !isClient))
            {
                return Result<UserResDto>.Failure(new[] { "You are not authorized to change this user's password." });
            }

            // Create ChangePasswordReqDto with adjusted current password based on business rules
            var changePasswordDto = new ChangePasswordReqDto
            {
                CurrentPassword = currentPassword,
                NewPassword = request.PasswordRequest.NewPassword,
                ConfirmNewPassword = request.PasswordRequest.ConfirmNewPassword
            };

            var result = await _userService.ChangeUserPasswordAsync(request.UserId, changePasswordDto);

            if (!result.Succeeded)
            {
                return Result<UserResDto>.Failure(result.Errors);
            }

            return Result<UserResDto>.Success(result.Value!);
        }
    }
}