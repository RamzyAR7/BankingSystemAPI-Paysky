#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.SetUserActiveStatus
{
    public sealed class SetUserActiveStatusCommandHandler : ICommandHandler<SetUserActiveStatusCommand>
    {
        private readonly IUserService _userService;
        private readonly IUserAuthorizationService? _userAuthorizationService;

        public SetUserActiveStatusCommandHandler(
            IUserService userService,
            IUserAuthorizationService? userAuthorizationService = null)
        {
            _userService = userService;
            _userAuthorizationService = userAuthorizationService;
        }

        public async Task<Result> Handle(SetUserActiveStatusCommand request, CancellationToken cancellationToken)
        {
            // First, check if the target user is SuperAdmin and we're trying to deactivate them.
            var roleResult = await _userService.GetUserRoleAsync(request.UserId);
            if (roleResult.IsSuccess && !string.IsNullOrWhiteSpace(roleResult.Value) &&
                string.Equals(roleResult.Value, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase) &&
                !request.IsActive)
            {
                return Result.Failure(ErrorType.Validation, ApiResponseMessages.Validation.SuperAdminCannotBeDeactivated);
            }

            // Next perform authorization checks
            if (_userAuthorizationService != null)
            {
                var authResult = await _userAuthorizationService.CanModifyUserAsync(request.UserId, UserModificationOperation.Edit);
                if(!authResult)
                {
                    return Result.Failure(authResult.ErrorItems);
                }
            }
            
            // The UserService now returns Result
            var result = await _userService.SetUserActiveStatusAsync(request.UserId, request.IsActive);
            
            if (!result) // Using implicit bool operator!
            {
                return Result.Failure(result.ErrorItems);
            }

            return Result.Success();
        }
    }
}
