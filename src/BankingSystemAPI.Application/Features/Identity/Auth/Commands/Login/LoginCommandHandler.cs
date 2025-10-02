using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Auth.Commands.Login
{
    public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResultDto>
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;

        public LoginCommandHandler(IAuthService authService, IUserService userService)
        {
            _authService = authService;
            _userService = userService;
        }

        public async Task<Result<AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // Pre-validation: Check user exists and is active - will fail if user not found
            var userResult = await _userService.GetUserByEmailAsync(request.Email);

            if (!userResult.Succeeded)
            {
                // Map "User not found" to a more generic login error for security
                return Result<AuthResultDto>.Failure(new[] { "Email or Password is incorrect!" });
            }

            var userDto = userResult.Value!;

            // Business validation: Check if user is active
            if (!userDto.IsActive)
            {
                return Result<AuthResultDto>.Failure(new[] { "User account is inactive. Contact administrator." });
            }

            // Business validation: Check if user's bank is active (if user has a bank)
            if (userDto.BankId.HasValue)
            {
                var bankActiveResult = await _userService.IsBankActiveAsync(userDto.BankId.Value);
                if (!bankActiveResult.Succeeded)
                {
                    return Result<AuthResultDto>.Failure(bankActiveResult.Errors);
                }

                if (!bankActiveResult.Value)
                {
                    return Result<AuthResultDto>.Failure(new[] { "Cannot login: user's bank is inactive." });
                }
            }

            // Business validation: Ensure user has a role assigned
            var userRoleResult = await _userService.GetUserRoleAsync(userDto.Id);
            if (!userRoleResult.Succeeded)
            {
                return Result<AuthResultDto>.Failure(userRoleResult.Errors);
            }

            if (string.IsNullOrEmpty(userRoleResult.Value))
            {
                return Result<AuthResultDto>.Failure(new[] { "User has no role assigned. Contact administrator." });
            }

            // Delegate to AuthService for core login logic
            var loginDto = new LoginReqDto
            {
                Email = request.Email,
                Password = request.Password
            };

            var result = await _authService.LoginAsync(loginDto);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return Result<AuthResultDto>.Failure(errors);
            }

            return Result<AuthResultDto>.Success(result);
        }
    }
}
