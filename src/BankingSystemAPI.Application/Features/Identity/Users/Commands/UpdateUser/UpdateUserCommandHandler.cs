using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.UpdateUser
{
    public sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, UserResDto>
    {
        private readonly IUserService _userService;
        private readonly IUserAuthorizationService? _userAuthorizationService;

        public UpdateUserCommandHandler(
            IUserService userService,
            IUserAuthorizationService? userAuthorizationService = null)
        {
            _userService = userService;
            _userAuthorizationService = userAuthorizationService;
        }

        public async Task<Result<UserResDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            // Authorization check - let exceptions bubble up to middleware
            if (_userAuthorizationService != null)
            {
                await _userAuthorizationService.CanModifyUserAsync(request.UserId, UserModificationOperation.Edit);
            }

            // Business validation: Check user exists and for duplicates within the same bank
            var existingUserResult = await _userService.GetUserByIdAsync(request.UserId);
            if (!existingUserResult.Succeeded)
            {
                return Result<UserResDto>.Failure(existingUserResult.Errors);
            }

            var existingUser = existingUserResult.Value!;

            // Check for duplicates within the same bank using IUserService
            var usersInSameBankResult = await _userService.GetUsersByBankIdAsync(existingUser.BankId ?? 0);
            if (!usersInSameBankResult.Succeeded)
            {
                return Result<UserResDto>.Failure(usersInSameBankResult.Errors);
            }

            var usersInSameBank = usersInSameBankResult.Value!;
            var duplicate = usersInSameBank.Any(u =>
                u.Id != request.UserId &&
                (string.Equals(u.Username, request.UserEdit.Username, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(u.Email, request.UserEdit.Email, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(u.NationalId, request.UserEdit.NationalId, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(u.PhoneNumber, request.UserEdit.PhoneNumber, StringComparison.OrdinalIgnoreCase)));

            if (duplicate)
            {
                return Result<UserResDto>.Failure(new[] { "Another user with the same details already exists in this bank." });
            }

            var result = await _userService.UpdateUserAsync(request.UserId, request.UserEdit);

            if (!result.Succeeded)
            {
                return Result<UserResDto>.Failure(result.Errors);
            }

            return Result<UserResDto>.Success(result.Value!);
        }
    }
}