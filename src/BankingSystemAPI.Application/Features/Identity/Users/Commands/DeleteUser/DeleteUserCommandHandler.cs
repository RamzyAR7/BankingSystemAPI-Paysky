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


namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.DeleteUser
{
    public sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, UserResDto>
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserAuthorizationService _userAuthorizationService;
        private readonly ILogger<DeleteUserCommandHandler> _logger;

        public DeleteUserCommandHandler(
            IUserService userService,
            ICurrentUserService currentUserService,
            ILogger<DeleteUserCommandHandler> logger,
            IUserAuthorizationService userAuthorizationService)
        {
            _userService = userService;
            _currentUserService = currentUserService;
            _userAuthorizationService = userAuthorizationService;
            _logger = logger;
        }

        public async Task<Result<UserResDto>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            // Chain validation and deletion using ResultExtensions
            var authResult = await ValidateAuthorizationAsync(request.UserId);
            if (authResult.IsFailure)
                return Result<UserResDto>.Failure(authResult.Errors);

            var selfDeletionResult = ValidateSelfDeletion(request.UserId);
            if (selfDeletionResult.IsFailure)
                return Result<UserResDto>.Failure(selfDeletionResult.Errors);

            var userResult = await ValidateUserExistsAsync(request.UserId);
            if (userResult.IsFailure)
                return Result<UserResDto>.Failure(userResult.Errors);

            var accountsResult = ValidateNoExistingAccounts(userResult.Value!);
            if (accountsResult.IsFailure)
                return Result<UserResDto>.Failure(accountsResult.Errors);

            var deleteResult = await ExecuteUserDeletionAsync(request.UserId);
            
            // Add side effects using ResultExtensions
            deleteResult.OnSuccess(() => 
                {
                    _logger.LogInformation(ApiResponseMessages.Logging.UserDeleted, request.UserId);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning(ApiResponseMessages.Logging.UserDeletionFailed, request.UserId, string.Join(", ", errors));
                });

            return deleteResult;
        }

        private async Task<Result> ValidateAuthorizationAsync(string userId)
        {
            try
            {
                var authResult = await _userAuthorizationService.CanModifyUserAsync(userId, UserModificationOperation.Delete);
                return authResult;
            }
            catch (Exception ex)
            {
                return Result.Forbidden($"Authorization failed: {ex.Message}");
            }
        }

        private Result ValidateSelfDeletion(string targetUserId)
        {
            var actingUserId = _currentUserService.UserId;
            
            return !string.IsNullOrEmpty(actingUserId) && string.Equals(actingUserId, targetUserId, StringComparison.OrdinalIgnoreCase)
                ? Result.BadRequest(AuthorizationConstants.ErrorMessages.CannotDeleteSelf)
                : Result.Success();
        }

        private async Task<Result<UserResDto>> ValidateUserExistsAsync(string userId)
        {
            var result = await _userService.GetUserByIdAsync(userId);
            return result.IsSuccess
                ? Result<UserResDto>.Success(result.Value!)
                : Result<UserResDto>.Failure(result.Errors);
        }

        private Result ValidateNoExistingAccounts(UserResDto user)
        {
            return user.Accounts != null && user.Accounts.Any()
                ? Result.BadRequest(ApiResponseMessages.Validation.DeleteUserHasAccounts)
                : Result.Success();
        }

        private async Task<Result<UserResDto>> ExecuteUserDeletionAsync(string userId)
        {
            var result = await _userService.DeleteUserAsync(userId);
            return result.IsSuccess
                ? Result<UserResDto>.Success(result.Value!)
                : Result<UserResDto>.Failure(result.Errors);
        }
    }
}
