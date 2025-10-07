#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.UserRoles.Commands.UpdateUserRoles
{
    public sealed class UpdateUserRolesCommandHandler : ICommandHandler<UpdateUserRolesCommand, UserRoleUpdateResultDto>
    {
        private readonly IUserRolesService _userRolesService;
        private readonly ICurrentUserService _currentUserService;

        public UpdateUserRolesCommandHandler(
            IUserRolesService userRolesService,
            ICurrentUserService currentUserService)
        {
            _userRolesService = userRolesService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<UserRoleUpdateResultDto>> Handle(UpdateUserRolesCommand request, CancellationToken cancellationToken)
        {
            // Business validation: Authorization check for SuperAdmin role assignment
            if (!string.IsNullOrEmpty(request.Role))
            {
                var isSuperAdmin = await _currentUserService.IsInRoleAsync(UserRole.SuperAdmin.ToString());

                if (!isSuperAdmin && string.Equals(request.Role, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return Result<UserRoleUpdateResultDto>.Forbidden(ApiResponseMessages.Validation.NotAuthorizedToAssignSuperAdmin);
                }
            }

            // Business validation: Ensure role is not just whitespace
            if (!string.IsNullOrEmpty(request.Role) && string.IsNullOrWhiteSpace(request.Role))
            {
                return Result<UserRoleUpdateResultDto>.ValidationFailed(ApiResponseMessages.Validation.RoleCannotBeEmptyOrWhitespace);
            }

            // Delegate to UserRolesService for core role assignment - returns Result<UserRoleUpdateResultDto>
            var updateDto = new UpdateUserRolesDto
            {
                UserId = request.UserId,
                Role = request.Role // Single role assignment
            };

            var result = await _userRolesService.UpdateUserRolesAsync(updateDto);

            if (!result) // Using implicit bool operator!
            {
                return Result<UserRoleUpdateResultDto>.Failure(result.ErrorItems);
            }

            return Result<UserRoleUpdateResultDto>.Success(result.Value!);
        }
    }
}

