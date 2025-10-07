#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.DeleteUsers
{
    public sealed class DeleteUsersCommandHandler : ICommandHandler<DeleteUsersCommand>
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserAuthorizationService? _userAuthorizationService;

        public DeleteUsersCommandHandler(
            IUserService userService,
            ICurrentUserService currentUserService,
            IUserAuthorizationService? userAuthorizationService = null)
        {
            _userService = userService;
            _currentUserService = currentUserService;
            _userAuthorizationService = userAuthorizationService;
        }

        public async Task<Result> Handle(DeleteUsersCommand request, CancellationToken cancellationToken)
        {
            var distinctIds = request.UserIds.Distinct().ToList();

            if (!distinctIds.Any())
                return Result.ValidationFailed(ApiResponseMessages.Validation.AtLeastOneUserIdProvided);

            // Business validation: Prevent self-deletion
            var actingUserId = _currentUserService.UserId;
            if (!string.IsNullOrEmpty(actingUserId) && distinctIds.Any(id =>
                string.Equals(actingUserId, id, StringComparison.OrdinalIgnoreCase)))
            {
                return Result.ValidationFailed(ApiResponseMessages.Validation.CannotDeleteSelfBulk);
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
                return Result.Failure(errors.Select(d => new ResultError(ErrorType.Validation, d)).ToList());
            }

            if (!usersToDelete.Any())
            {
                return Result.ValidationFailed(ApiResponseMessages.Validation.NoValidUsersFoundToDelete);
            }

            // Perform bulk deletion using the existing service method - returns Result<bool>
            var deleteResult = await _userService.DeleteRangeOfUsersAsync(usersToDelete);

            if (!deleteResult.IsSuccess)
            {
                return Result.Failure(deleteResult.ErrorItems);
            }

            return Result.Success();
        }
    }
}
