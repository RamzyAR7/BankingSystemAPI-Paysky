#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Authorization.Helpers;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using Microsoft.Extensions.Logging;
using System.Linq;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUsersByBankId
{
    public sealed class GetUsersByBankIdQueryHandler : IQueryHandler<GetUsersByBankIdQuery, IList<UserResDto>>
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUser;
        private readonly IUserAuthorizationService? _userAuthorization;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GetUsersByBankIdQueryHandler>? _logger;

        public GetUsersByBankIdQueryHandler(
            IUserService userService,
            ICurrentUserService currentUserService,
            IUserAuthorizationService? userAuthorizationService = null,
            IUnitOfWork? uow = null,
            ILogger<GetUsersByBankIdQueryHandler>? logger = null)
        {
            _userService = userService;
            _currentUser = currentUserService;
            _userAuthorization = userAuthorizationService;
            _uow = uow;
            _logger = logger;
        }

        public async Task<Result<IList<UserResDto>>> Handle(GetUsersByBankIdQuery request, CancellationToken cancellationToken)
        {
            // Business validation: Check if user can view users
            if (_userAuthorization != null)
            {
                // get role once and reuse it; return Forbidden to be explicit about authorization
                var role = await _currentUser.GetRoleFromStoreAsync();

                if (_userAuthorization != null && RoleHelper.IsClient(role?.Name))
                {
                    var op = Result<IList<UserResDto>>.Forbidden("Clients cannot view users.");
                    LogResult(op, "user", "get-by-bank");
                    return op;
                }
            }

            // Business logic: Handle bank scoping for non-SuperAdmin users
            var roleForSuper = await _currentUser.GetRoleFromStoreAsync();
            var isSuper = RoleHelper.IsSuperAdmin(roleForSuper.Name);
            int targetBankId = request.BankId;

            var bank = await _uow.BankRepository.GetByIdAsync(targetBankId);

            if (bank == null)
            {
                var op = Result<IList<UserResDto>>.NotFound("Bank", targetBankId);
                LogResult(op, "user", "get-by-bank");
                return op;
            }

            if (!isSuper)
            {
                // Non-SuperAdmin users are restricted to their own bank
                var actingUserId = _currentUser.UserId;
                if (string.IsNullOrEmpty(actingUserId))
                {
                    var op = Result<IList<UserResDto>>.Unauthorized(ApiResponseMessages.ErrorPatterns.NotAuthenticated);
                    LogResult(op, "user", "get-by-bank");
                    return op;
                }

                var actingUserResult = await _userService.GetUserByIdAsync(actingUserId);
                if (!actingUserResult.IsSuccess)
                {
                    var op = Result<IList<UserResDto>>.Failure(actingUserResult.ErrorItems);
                    LogResult(op, "user", "get-by-bank");
                    return op;
                }

                var actingUser = actingUserResult.Value!;
                if (actingUser.BankId == null)
                {
                    var op = Result<IList<UserResDto>>.Success(new List<UserResDto>());
                    LogResult(op, "user", "get-by-bank");
                    return op;
                }
                targetBankId = actingUser.BankId.Value;
            }

            // Get users by bank ID using UserService
            var result = await _userService.GetUsersByBankIdAsync(targetBankId);

            if (!result.IsSuccess)
            {
                var op = Result<IList<UserResDto>>.Failure(result.ErrorItems);
                LogResult(op, "user", "get-by-bank");
                return op;
            }

            var success = Result<IList<UserResDto>>.Success(result.Value!);
            LogResult(success, "user", "get-by-bank");
            return success;
        }

        private void LogResult<T>(Result<T> result, string category, string operation)
        {
            if (_logger == null)
                return;

            if (result.IsSuccess)
                _logger.LogInformation(ApiResponseMessages.Logging.OperationCompletedController, category, operation);
            else
                _logger.LogWarning(ApiResponseMessages.Logging.OperationFailedController, category, operation, string.Join(", ", result.Errors ?? Enumerable.Empty<string>()));
        }
    }
}
