#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUsersByBankId
{
    public sealed class GetUsersByBankIdQueryHandler : IQueryHandler<GetUsersByBankIdQuery, IList<UserResDto>>
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUser_service;
        private readonly IUserAuthorizationService? _userAuthorization_service;
        private readonly IUnitOfWork _uow;

        public GetUsersByBankIdQueryHandler(
            IUserService userService,
            ICurrentUserService currentUserService,
            IUserAuthorizationService? userAuthorizationService = null,
            IUnitOfWork? uow = null)
        {
            _userService = userService;
            _currentUser_service = currentUserService;
            _userAuthorization_service = userAuthorizationService;
            _uow = uow;
        }

        public async Task<Result<IList<UserResDto>>> Handle(GetUsersByBankIdQuery request, CancellationToken cancellationToken)
        {
            // Business validation: Check if user can view users
            if (_userAuthorization_service != null)
            {
                var role = await _currentUser_service.GetRoleFromStoreAsync();
                if (RoleHelper.IsClient(role.Name))
                {
                    return Result<IList<UserResDto>>.Success(new List<UserResDto>());
                }
            }

            // Business logic: Handle bank scoping for non-SuperAdmin users
            var roleForSuper = await _currentUser_service.GetRoleFromStoreAsync();
            var isSuper = RoleHelper.IsSuperAdmin(roleForSuper.Name);
            int targetBankId = request.BankId;

            var bank = await _uow.BankRepository.GetByIdAsync(targetBankId);

            if (bank == null)
            {
                return Result<IList<UserResDto>>.NotFound("Bank", targetBankId);
            }

            if (!isSuper)
            {
                // Non-SuperAdmin users are restricted to their own bank
                var actingUserId = _currentUser_service.UserId;
                if (string.IsNullOrEmpty(actingUserId))
                {
                    return Result<IList<UserResDto>>.Unauthorized(ApiResponseMessages.ErrorPatterns.NotAuthenticated);
                }

                var actingUserResult = await _userService.GetUserByIdAsync(actingUserId);
                if (!actingUserResult.IsSuccess)
                {
                    return Result<IList<UserResDto>>.Failure(actingUserResult.ErrorItems);
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
                return Result<IList<UserResDto>>.Failure(result.ErrorItems);
            }

            return Result<IList<UserResDto>>.Success(result.Value!);
        }
    }
}
