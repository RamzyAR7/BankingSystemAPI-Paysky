using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Authorization;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.CreateUser
{
    /// <summary>
    /// Simplified user creation handler using UserReqDto
    /// </summary>
    public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserResDto>
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserAuthorizationService? _userAuthorizationService;

        public CreateUserCommandHandler(
            IUserService userService,
            ICurrentUserService currentUserService,
            IUserAuthorizationService? userAuthorizationService = null)
        {
            _userService = userService;
            _currentUserService = currentUserService;
            _userAuthorizationService = userAuthorizationService;
        }

        public async Task<Result<UserResDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            // Authorization check - let exceptions bubble up to middleware
            if (_userAuthorizationService != null)
            {
                await _userAuthorizationService.CanCreateUserAsync();
            }

            // Determine target bank and role based on current user
            var actingRole = await _currentUserService.GetRoleFromStoreAsync();
            var isSuperAdmin = string.Equals(actingRole.Name, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase);

            // Clone the UserReqDto and adjust bank/role based on permissions
            var userDto = new UserReqDto
            {
                Username = request.UserRequest.Username,
                Email = request.UserRequest.Email,
                Password = request.UserRequest.Password,
                PasswordConfirm = request.UserRequest.PasswordConfirm,
                NationalId = request.UserRequest.NationalId,
                FullName = request.UserRequest.FullName,
                DateOfBirth = request.UserRequest.DateOfBirth,
                PhoneNumber = request.UserRequest.PhoneNumber,
                BankId = isSuperAdmin ? request.UserRequest.BankId : _currentUserService.BankId,
                Role = isSuperAdmin ? request.UserRequest.Role : UserRole.Client.ToString()
            };
            var result = await _userService.CreateUserAsync(userDto);

            if (!result.Succeeded)
            {
                return Result<UserResDto>.Failure(result.Errors);
            }

            return Result<UserResDto>.Success(result.Value!);
        }
    }
}