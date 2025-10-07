#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Messaging;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Auth.Commands.Logout
{
    public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand, AuthResultDto>
    {
        private readonly IAuthService _authService;
        private readonly ILogger<LogoutCommandHandler> _logger;

        public LogoutCommandHandler(IAuthService authService, ILogger<LogoutCommandHandler> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public async Task<Result<AuthResultDto>> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.LogoutAsync(request.UserId);

            // Convert AuthResultDto to Result<AuthResultDto> using ResultExtensions patterns
            var logoutResult = result.Succeeded
                ? Result<AuthResultDto>.Success(result)
                : Result<AuthResultDto>.Failure(result.Errors.Select(e => new ResultError(ErrorType.Validation, e.Description)));

            // Add side effects using ResultExtensions
            logoutResult.OnSuccess(() =>
            {
                _logger.LogInformation(ApiResponseMessages.Logging.OperationCompletedController, "auth", "logout");
            })
            .OnFailure(errors =>
            {
                _logger.LogWarning(ApiResponseMessages.Logging.OperationFailedController,
                    "auth", "logout", string.Join(", ", errors));
            });

            return logoutResult;
        }
    }
}
