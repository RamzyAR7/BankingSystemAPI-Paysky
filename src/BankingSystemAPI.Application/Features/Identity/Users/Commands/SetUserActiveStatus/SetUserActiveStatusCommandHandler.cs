using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;

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
            // Business validation: Authorization check
            if (_userAuthorizationService != null)
            {
                await _userAuthorizationService.CanModifyUserAsync(request.UserId, UserModificationOperation.Edit);
            }

            // The UserService now returns Result
            var result = await _userService.SetUserActiveStatusAsync(request.UserId, request.IsActive);
            
            if (!result) // Using implicit bool operator!
            {
                return Result.Failure(result.Errors);
            }

            return Result.Success();
        }
    }
}