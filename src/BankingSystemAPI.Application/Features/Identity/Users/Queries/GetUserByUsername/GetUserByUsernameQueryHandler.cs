
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


namespace BankingSystemAPI.Application.Features.Identity.Users.Queries.GetUserByUsername
{
    public sealed class GetUserByUsernameQueryHandler : IQueryHandler<GetUserByUsernameQuery, UserResDto>
    {
        private readonly IUserService _userService;
        private readonly IUserAuthorizationService? _userAuthorizationService;
        private readonly ILogger<GetUserByUsernameQueryHandler> _logger;

        public GetUserByUsernameQueryHandler(
            IUserService userService,
            IUserAuthorizationService? userAuthorizationService = null,
            ILogger<GetUserByUsernameQueryHandler> logger = null)
        {
            _userService = userService;
            _userAuthorizationService = userAuthorizationService;
            _logger = logger;
        }

        public async Task<Result<UserResDto>> Handle(GetUserByUsernameQuery request, CancellationToken cancellationToken)
        {
            Result<UserResDto> operationResult;

            var result = await _userService.GetUserByUsernameAsync(request.Username);
            if (!result.IsSuccess || result.Value == null)
            {
                operationResult = Result<UserResDto>.Failure(result.ErrorItems);
                LogResult(operationResult, "user", "get-by-username");
                return operationResult;
            }

            var userDto = result.Value;

            if (_userAuthorizationService == null)
            {
                operationResult = Result<UserResDto>.Failure(new ResultError(ErrorType.Forbidden, "Authorization service not available."));
                LogResult(operationResult, "user", "get-by-username");
                return operationResult;
            }

            var authResult = await _userAuthorizationService.CanViewUserAsync(userDto.Id);
            if (!authResult)
            {
                operationResult = Result<UserResDto>.Failure(authResult.ErrorItems);
                LogResult(operationResult, "user", "get-by-username");
                return operationResult;
            }

            operationResult = Result<UserResDto>.Success(userDto);
            LogResult(operationResult, "user", "get-by-username");
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
