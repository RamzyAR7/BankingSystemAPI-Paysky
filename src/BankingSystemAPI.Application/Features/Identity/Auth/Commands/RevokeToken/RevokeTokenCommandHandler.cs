#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Auth.Commands.RevokeToken
{
    public sealed class RevokeTokenCommandHandler : ICommandHandler<RevokeTokenCommand, AuthResultDto>
    {
        private readonly IAuthService _authService;
        private readonly IUserAuthorizationService? _userAuthorizationService;

        public RevokeTokenCommandHandler(
            IAuthService authService,
            IUserAuthorizationService? userAuthorizationService = null)
        {
            _authService = authService;
            _userAuthorizationService = userAuthorizationService;
        }

        public async Task<Result<AuthResultDto>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
        {
            // Business validation: Authorization check
            if (_userAuthorizationService != null)
            {
                await _userAuthorizationService.CanViewUserAsync(request.UserId);
            }

            // Delegate to AuthService for core revoke logic
            var result = await _authService.RevokeTokenAsync(request.UserId);

            if (!result.Succeeded)
            {
                return Result<AuthResultDto>.Failure(result.Errors.Select(e => new ResultError(ErrorType.Validation, e.Description)));
            }

            return Result<AuthResultDto>.Success(result);
        }
    }
}
