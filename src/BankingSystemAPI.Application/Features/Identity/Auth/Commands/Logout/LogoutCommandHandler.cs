using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Auth.Commands.Logout
{
    public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand, AuthResultDto>
    {
        private readonly IAuthService _authService;

        public LogoutCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<Result<AuthResultDto>> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.LogoutAsync(request.UserId);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return Result<AuthResultDto>.Failure(errors);
            }

            return Result<AuthResultDto>.Success(result);
        }
    }
}