#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using Microsoft.Extensions.Logging;
using System.Linq;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUserById
{
    public sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserResDto>
    {
        private readonly IUserService _userService;
        private readonly IUserAuthorizationService? _userAuthorizationService;
        private readonly ILogger<GetUserByIdQueryHandler> _logger;

        public GetUserByIdQueryHandler(
            IUserService userService,
            ILogger<GetUserByIdQueryHandler> logger,
            IUserAuthorizationService? userAuthorizationService = null)
        {
            _userService = userService;
            _logger = logger;
            _userAuthorizationService = userAuthorizationService;
        }

        public async Task<Result<UserResDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            Result<UserResDto> operationResult;

            if (_userAuthorizationService == null)
            {
                operationResult = Result<UserResDto>.Failure(new ResultError(ErrorType.Forbidden, "Authorization service not available."));
                LogResult(operationResult, "user", "get-by-id");
                return operationResult;
            }

            var authResult = await _userAuthorizationService.CanViewUserAsync(request.UserId);
            if (!authResult.IsSuccess)
            {
                operationResult = Result<UserResDto>.Failure(authResult.ErrorItems);
                LogResult(operationResult, "user", "get-by-id");
                return operationResult;
            }

            var userResult = await _userService.GetUserByIdAsync(request.UserId);
            if (!userResult.IsSuccess || userResult.Value == null)
            {
                operationResult = Result<UserResDto>.Failure(userResult.ErrorItems);
                LogResult(operationResult, "user", "get-by-id");
                return operationResult;
            }

            operationResult = Result<UserResDto>.Success(userResult.Value);
            LogResult(operationResult, "user", "get-by-id");
            return operationResult;
        }

        private void LogResult<T>(Result<T> result, string category, string operation)
        {
            if (result.IsSuccess)
                _logger.LogInformation(ApiResponseMessages.Logging.OperationCompletedController, category, operation);
            else
                _logger.LogWarning(ApiResponseMessages.Logging.OperationFailedController, category, operation, string.Join(", ", result.Errors ?? Enumerable.Empty<string>()));
        }
    }
}
