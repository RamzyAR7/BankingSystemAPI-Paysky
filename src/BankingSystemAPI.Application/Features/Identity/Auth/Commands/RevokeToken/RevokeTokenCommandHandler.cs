#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;
using Microsoft.Extensions.Logging;
using System.Linq;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Auth.Commands.RevokeToken
{
    public sealed class RevokeTokenCommandHandler : ICommandHandler<RevokeTokenCommand, AuthResultDto>
    {
        private readonly IAuthService _authService;
        private readonly IUserAuthorizationService? _userAuthorizationService;
        private readonly ILogger<RevokeTokenCommandHandler> _logger;

        public RevokeTokenCommandHandler(
            IAuthService authService,
            IUserAuthorizationService? userAuthorizationService = null,
            ILogger<RevokeTokenCommandHandler>? logger = null)
        {
            _authService = authService;
            _userAuthorizationService = userAuthorizationService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<AuthResultDto>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
        {
            // Business validation: Authorization check (propagate failures)
            if (_userAuthorizationService != null)
            {
                var authCheck = await _userAuthorizationService.CanViewUserAsync(request.UserId);
                if (authCheck.IsFailure)
                {
                    IEnumerable<ResultError> mappedAuthErrors = authCheck.ErrorItems
                        ?? (authCheck.Errors?.Select(e => new ResultError(ErrorType.Forbidden, e)) ?? Enumerable.Empty<ResultError>());

                    var operationResult = Result<AuthResultDto>.Failure(mappedAuthErrors);

                    // Log using Result API
                    if (operationResult.IsSuccess)
                    {
                        _logger.LogInformation(ApiResponseMessages.Logging.OperationCompletedController, "auth", "revoke");
                    }
                    else
                    {
                        _logger.LogWarning(ApiResponseMessages.Logging.OperationFailedController, "auth", "revoke", string.Join(", ", operationResult.Errors ?? Enumerable.Empty<string>()));
                    }

                    return operationResult;
                }
            }

            // Delegate to AuthService for core revoke logic
            var serviceResult = await _authService.RevokeTokenAsync(request.UserId);

            Result<AuthResultDto> finalResult;
            if (!serviceResult.Succeeded)
            {
                var mappedErrors = serviceResult.Errors?.Select(e =>
                    new ResultError(ErrorType.Validation, e.Description ?? e.Code ?? "Validation error"))
                    ?? Enumerable.Empty<ResultError>();

                finalResult = Result<AuthResultDto>.Failure(mappedErrors);
            }
            else
            {
                finalResult = Result<AuthResultDto>.Success(serviceResult);
            }

            if (finalResult.IsSuccess)
            {
                _logger.LogInformation(ApiResponseMessages.Logging.OperationCompletedController, "auth", "revoke");
            }
            else
            {
                _logger.LogWarning(ApiResponseMessages.Logging.OperationFailedController, "auth", "revoke", string.Join(", ", finalResult.Errors ?? Enumerable.Empty<string>()));
            }

            return finalResult;
        }
    }
}
