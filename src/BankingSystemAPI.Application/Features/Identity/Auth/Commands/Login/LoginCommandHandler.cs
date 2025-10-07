#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.Auth.Commands.Login
{
    public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResultDto>
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(IAuthService authService, IUserService userService, ILogger<LoginCommandHandler> logger)
        {
            _authService = authService;
            _userService = userService;
            _logger = logger;
        }

        public async Task<Result<AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // Validate user exists and is active
            var userResult = await ValidateUserExistsAsync(request.Email);
            if (userResult.IsFailure)
                return Result<AuthResultDto>.Failure(userResult.ErrorItems);

            var activeResult = ValidateUserActive(userResult.Value!);
            if (activeResult.IsFailure)
                return Result<AuthResultDto>.Failure(activeResult.ErrorItems);

            var bankResult = await ValidateBankActive(userResult.Value!);
            if (bankResult.IsFailure)
                return Result<AuthResultDto>.Failure(bankResult.ErrorItems);

            var roleResult = await ValidateUserRole(userResult.Value!);
            if (roleResult.IsFailure)
                return Result<AuthResultDto>.Failure(roleResult.ErrorItems);

            // Execute login
            var loginResult = await ExecuteLoginAsync(request, userResult.Value!);

            if (loginResult.IsSuccess)
            {
                _logger.LogInformation(ApiResponseMessages.Logging.OperationCompletedController, "auth", "login");
            }
            else
            {
                _logger.LogWarning(ApiResponseMessages.Logging.OperationFailedController, "auth", "login", string.Join(", ", loginResult.Errors));
            }

            return loginResult;
        }

        private async Task<Result<UserResDto>> ValidateUserExistsAsync(string email)
        {
            var userResult = await _userService.GetUserByEmailAsync(email);
            
            // Map "User not found" to authentication error for security (prevents user enumeration)
            if (!userResult.IsSuccess)
            {
                return Result<UserResDto>.InvalidCredentials(); // Will map to 401 Unauthorized
            }

            return Result<UserResDto>.Success(userResult.Value!);
        }

        private Result<UserResDto> ValidateUserActive(UserResDto userDto)
        {
            return userDto.IsActive
                ? Result<UserResDto>.Success(userDto)
                : Result<UserResDto>.Forbidden(ApiResponseMessages.ErrorPatterns.AccessDenied); // Maps to 403
        }

        private async Task<Result<UserResDto>> ValidateBankActive(UserResDto userDto)
        {
            if (!userDto.BankId.HasValue)
                return Result<UserResDto>.Success(userDto);

            var bankActiveResult = await _userService.IsBankActiveAsync(userDto.BankId.Value);
            if (!bankActiveResult.IsSuccess)
            {
                return Result<UserResDto>.Failure(bankActiveResult.ErrorItems);
            }

            return bankActiveResult.Value
                ? Result<UserResDto>.Success(userDto)
                : Result<UserResDto>.Forbidden(ApiResponseMessages.ErrorPatterns.AccessDenied); // Maps to 403
        }

        private async Task<Result<UserResDto>> ValidateUserRole(UserResDto userDto)
        {
            var userRoleResult = await _userService.GetUserRoleAsync(userDto.Id);
            if (!userRoleResult.IsSuccess)
            {
                return Result<UserResDto>.Failure(userRoleResult.ErrorItems);
            }

            return string.IsNullOrEmpty(userRoleResult.Value)
                ? Result<UserResDto>.Forbidden(ApiResponseMessages.ErrorPatterns.AccessDenied) // Maps to 403
                : Result<UserResDto>.Success(userDto);
        }

        private async Task<Result<AuthResultDto>> ExecuteLoginAsync(LoginCommand request, UserResDto userDto)
        {
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
                return Result<AuthResultDto>.InvalidCredentials(); // Will map to 401 Unauthorized
            }

            return Result<AuthResultDto>.Success(result);
        }
    }
}

