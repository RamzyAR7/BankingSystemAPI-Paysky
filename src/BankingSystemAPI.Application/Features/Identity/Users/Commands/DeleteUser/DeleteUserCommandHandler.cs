using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.DeleteUser
{
    public sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, UserResDto>
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserAuthorizationService? _userAuthorizationService;

        public DeleteUserCommandHandler(
            IUserService userService,
            ICurrentUserService currentUserService,
            IUserAuthorizationService? userAuthorizationService = null)
        {
            _userService = userService;
            _currentUserService = currentUserService;
            _userAuthorizationService = userAuthorizationService;
        }

        public async Task<Result<UserResDto>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            // Authorization check - let exceptions bubble up to middleware
            if (_userAuthorizationService != null)
            {
                await _userAuthorizationService.CanModifyUserAsync(request.UserId, UserModificationOperation.Delete);
            }

            // Business validation: Prevent self-deletion
            var actingUserId = _currentUserService.UserId;
            if (!string.IsNullOrEmpty(actingUserId) && string.Equals(actingUserId, request.UserId, StringComparison.OrdinalIgnoreCase))
            {
                return Result<UserResDto>.Failure(new[] { "Cannot delete yourself." });
            }

            // Business validation: Check if user exists - will fail if user doesn't exist
            var existingUserResult = await _userService.GetUserByIdAsync(request.UserId);
            if (!existingUserResult.Succeeded)
            {
                return Result<UserResDto>.Failure(existingUserResult.Errors);
            }

            var existingUser = existingUserResult.Value!;

            // Business validation: Check if user has accounts
            if (existingUser.Accounts != null && existingUser.Accounts.Any())
            {
                return Result<UserResDto>.Failure(new[] { "Cannot delete user with existing accounts." });
            }

            var result = await _userService.DeleteUserAsync(request.UserId);

            if (!result.Succeeded)
            {
                return Result<UserResDto>.Failure(result.Errors);
            }

            return Result<UserResDto>.Success(result.Value!);
        }
    }
}