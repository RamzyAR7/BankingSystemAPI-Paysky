#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;
using Microsoft.Extensions.Logging;
using System.Linq;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Auth.Commands.RefreshToken
{
    public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthResultDto>
    {
        private readonly IAuthService _authService;
        private readonly ILogger<RefreshTokenCommandHandler> _logger;

        public RefreshTokenCommandHandler(IAuthService authService, ILogger<RefreshTokenCommandHandler> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task<Result<AuthResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var serviceResult = await _authService.RefreshTokenAsync(request.Token);

            // Map service result to application Result<T>
            Result<AuthResultDto> operationResult;
            if (!serviceResult.Succeeded)
            {
                var mappedErrors = serviceResult.Errors?.Select(e => new ResultError(ErrorType.Validation, e.Description ?? e.Code ?? "Validation error"))
                                    ?? Enumerable.Empty<ResultError>();

                operationResult = Result<AuthResultDto>.Failure(mappedErrors);
            }
            else
            {
                operationResult = Result<AuthResultDto>.Success(serviceResult);
            }

            // Log using Result API
            if (operationResult.IsSuccess)
            {
                _logger.LogInformation(ApiResponseMessages.Logging.OperationCompletedController, "auth", "refresh");
            }
            else
            {
                _logger.LogWarning(ApiResponseMessages.Logging.OperationFailedController, "auth", "refresh", string.Join(", ", operationResult.Errors ?? Enumerable.Empty<string>()));
            }

            return operationResult;
        }
    }
}
