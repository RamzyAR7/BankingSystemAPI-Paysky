using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Authorization.Helpers;

namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUsersByBankId
{
    public sealed class GetUsersByBankIdQueryHandler : IQueryHandler<GetUsersByBankIdQuery, IList<UserResDto>>
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserAuthorizationService? _userAuthorizationService;

        public GetUsersByBankIdQueryHandler(
            IUserService userService,
            ICurrentUserService currentUserService,
            IUserAuthorizationService? userAuthorizationService = null)
        {
            _userService = userService;
            _currentUserService = currentUserService;
            _userAuthorizationService = userAuthorizationService;
        }

        public async Task<Result<IList<UserResDto>>> Handle(GetUsersByBankIdQuery request, CancellationToken cancellationToken)
        {
            // Business validation: Check if user can view users
            if (_userAuthorizationService != null)
            {
                var role = await _currentUserService.GetRoleFromStoreAsync();
                if (RoleHelper.IsClient(role.Name))
                {
                    return Result<IList<UserResDto>>.Success(new List<UserResDto>());
                }
            }

            // Business logic: Handle bank scoping for non-SuperAdmin users
            var roleForSuper = await _currentUserService.GetRoleFromStoreAsync();
            var isSuper = RoleHelper.IsSuperAdmin(roleForSuper.Name);
            int targetBankId = request.BankId;

            if (!isSuper)
            {
                // Non-SuperAdmin users are restricted to their own bank
                var actingUserId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(actingUserId))
                {
                    return Result<IList<UserResDto>>.Failure(new[] { "User not authenticated." });
                }

                var actingUserResult = await _userService.GetUserByIdAsync(actingUserId);
                if (!actingUserResult.IsSuccess)
                {
                    return Result<IList<UserResDto>>.Failure(actingUserResult.Errors);
                }

                var actingUser = actingUserResult.Value!;
                if (actingUser.BankId == null)
                {
                    return Result<IList<UserResDto>>.Success(new List<UserResDto>());
                }
                targetBankId = actingUser.BankId.Value;
            }

            // Get users by bank ID using UserService
            var result = await _userService.GetUsersByBankIdAsync(targetBankId);
            
            if (!result.IsSuccess)
            {
                return Result<IList<UserResDto>>.Failure(result.Errors);
            }

            return Result<IList<UserResDto>>.Success(result.Value!);
        }
    }
}