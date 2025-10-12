#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;
using Microsoft.Extensions.Logging;
using System.Linq;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.DeleteUsers
{
    public sealed class DeleteUsersCommandHandler : ICommandHandler<DeleteUsersCommand>
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserAuthorizationService? _userAuthorizationService;
        private readonly ILogger<DeleteUsersCommandHandler> _logger;

        public DeleteUsersCommandHandler(
            IUserService userService,
            ICurrentUserService currentUserService,
            IUserAuthorizationService? userAuthorizationService = null,
            ILogger<DeleteUsersCommandHandler> logger = null)
        {
            _userService = userService;
            _currentUserService = currentUserService;
            _userAuthorizationService = userAuthorizationService;
            _logger = logger;
        }

        public async Task<Result> Handle(DeleteUsersCommand request, CancellationToken cancellationToken)
        {
            var distinctIds = request.UserIds.Distinct().ToList();

            if (!distinctIds.Any())
            {
                var op = Result.ValidationFailed(ApiResponseMessages.Validation.AtLeastOneUserIdProvided);
                LogResult(op, "user", "delete-multiple");
                return op;
            }

            // Business validation: Prevent self-deletion
            var actingUserId = _currentUserService.UserId;
            if (!string.IsNullOrEmpty(actingUserId) && distinctIds.Any(id =>
                string.Equals(actingUserId, id, StringComparison.OrdinalIgnoreCase)))
            {
                var op = Result.ValidationFailed(ApiResponseMessages.Validation.CannotDeleteSelfBulk);
                LogResult(op, "user", "delete-multiple");
                return op;
            }

            var errors = new List<string>();
            var usersToDelete = new List<string>();

            // Validate each user before deletion
            foreach (var userId in distinctIds)
            {
                // Authorization check for each user - let exceptions bubble up for individual failures
                if (_userAuthorizationService != null)
                {
                    try
                    {
                        await _userAuthorizationService.CanModifyUserAsync(userId, UserModificationOperation.Delete);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(string.Format("User {0}: {1}", userId, ex.Message));
                        continue;
                    }
                }

                // Business validation: Check if user exists and has no accounts
                var existingUserResult = await _userService.GetUserByIdAsync(userId);
                if (!existingUserResult) // Using implicit bool operator!
                {
                    errors.Add(string.Format("User {0}: {1}", userId, string.Join("; ", existingUserResult.ErrorItems.Select(e => e.Message))));
                    continue;
                }

                var existingUser = existingUserResult.Value!;

                // Business validation: Check if user has accounts
                if (existingUser.Accounts != null && existingUser.Accounts.Any())
                {
                    errors.Add(string.Format("User {0}: {1}", userId, ApiResponseMessages.Validation.DeleteUserHasAccounts));
                    continue;
                }

                usersToDelete.Add(userId);
            }

            if (errors.Any())
            {
                var op = Result.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)).ToList());
                LogResult(op, "user", "delete-multiple");
                return op;
            }

            if (!usersToDelete.Any())
            {
                var op = Result.ValidationFailed(ApiResponseMessages.Validation.NoValidUsersFoundToDelete);
                LogResult(op, "user", "delete-multiple");
                return op;
            }

            // Perform bulk deletion using the existing service method - returns Result<bool>
            var deleteResult = await _userService.DeleteRangeOfUsersAsync(usersToDelete);

            if (!deleteResult.IsSuccess)
            {
                var op = Result.Failure(deleteResult.ErrorItems);
                LogResult(op, "user", "delete-multiple");
                return op;
            }

            var success = Result.Success();
            LogResult(success, "user", "delete-multiple");
            return success;
        }

        private void LogResult(Result result, string category, string operation)
        {
            if (result.IsSuccess)
                _logger.LogInformation(ApiResponseMessages.Logging.OperationCompletedController, category, operation);
            else
                _logger.LogWarning(ApiResponseMessages.Logging.OperationFailedController, category, operation, string.Join(", ", result.Errors ?? Enumerable.Empty<string>()));
        }
    }
}
