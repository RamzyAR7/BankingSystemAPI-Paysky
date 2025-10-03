using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;
using Microsoft.Extensions.Logging;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.CreateUser
{
    /// <summary>
    /// Simplified user creation handler using UserReqDto with enhanced error handling
    /// </summary>
    public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserResDto>
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserAuthorizationService? _userAuthorizationService;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(
            IUserService userService,
            ICurrentUserService currentUserService,
            ILogger<CreateUserCommandHandler> logger,
            IUserAuthorizationService? userAuthorizationService = null)
        {
            _userService = userService;
            _currentUserService = currentUserService;
            _userAuthorizationService = userAuthorizationService;
            _logger = logger;
        }

        public async Task<Result<UserResDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            // Validate authorization
            var authResult = await ValidateAuthorizationAsync();
            if (authResult.IsFailure)
                return Result<UserResDto>.Failure(authResult.Errors);

            // Determine user context
            var contextResult = await DetermineUserContextAsync();
            if (contextResult.IsFailure)
                return Result<UserResDto>.Failure(contextResult.Errors);

            // Create user with context
            var createResult = await CreateUserWithContextAsync(request.UserRequest, contextResult.Value!);
            
            // Add side effects using ResultExtensions
            createResult.OnSuccess(() => 
                {
                    _logger.LogInformation("User created successfully: {Username}", request.UserRequest.Username);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("User creation failed for {Username}. Errors: {Errors}",
                        request.UserRequest.Username, string.Join(", ", errors));
                });

            return createResult;
        }

        private async Task<Result> ValidateAuthorizationAsync()
        {
            if (_userAuthorizationService == null)
                return Result.Success();

            try
            {
                await _userAuthorizationService.CanCreateUserAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Forbidden($"Authorization failed: {ex.Message}");
            }
        }

        private async Task<Result<UserCreationContext>> DetermineUserContextAsync()
        {
            var actingRole = await _currentUserService.GetRoleFromStoreAsync();
            var isSuperAdmin = string.Equals(actingRole.Name, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase);

            var context = new UserCreationContext
            {
                IsSuperAdmin = isSuperAdmin,
                ActingUserBankId = _currentUserService.BankId
            };

            return Result<UserCreationContext>.Success(context);
        }

        private async Task<Result<UserResDto>> CreateUserWithContextAsync(UserReqDto originalRequest, UserCreationContext context)
        {
            // Clone and adjust the UserReqDto based on permissions using functional approach
            var adjustedRequest = context.IsSuperAdmin
                ? originalRequest  // SuperAdmin can set any bank/role
                : AdjustRequestForNonSuperAdmin(originalRequest, context);

            var result = await _userService.CreateUserAsync(adjustedRequest);

            return result.Succeeded
                ? Result<UserResDto>.Success(result.Value!)
                : Result<UserResDto>.Failure(result.Errors);
        }

        private UserReqDto AdjustRequestForNonSuperAdmin(UserReqDto original, UserCreationContext context)
        {
            // For non-SuperAdmin users, restrict bank and role assignment
            return new UserReqDto
            {
                Username = original.Username,
                Email = original.Email,
                Password = original.Password,
                PasswordConfirm = original.PasswordConfirm,
                NationalId = original.NationalId,
                FullName = original.FullName,
                DateOfBirth = original.DateOfBirth,
                PhoneNumber = original.PhoneNumber,
                BankId = context.ActingUserBankId, // Force to acting user's bank
                Role = UserRole.Client.ToString()   // Force to Client role
            };
        }

        private class UserCreationContext
        {
            public bool IsSuperAdmin { get; set; }
            public int? ActingUserBankId { get; set; }
        }
    }
}