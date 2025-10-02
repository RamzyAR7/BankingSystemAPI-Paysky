using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;

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
            // Business validation: Get the role to assign (take first role from collection)
            var targetRole = request.Roles.FirstOrDefault();

            // Business validation: Authorization check for SuperAdmin role assignment
            if (!string.IsNullOrEmpty(targetRole))
            {
                var isSuperAdmin = await _currentUserService.IsInRoleAsync(UserRole.SuperAdmin.ToString());
                
                if (!isSuperAdmin && string.Equals(targetRole, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return Result<UserRoleUpdateResultDto>.Failure(new[] { "Not authorized to assign SuperAdmin role." });
                }
            }

            // Business validation: Ensure role is not just whitespace
            if (!string.IsNullOrEmpty(targetRole) && string.IsNullOrWhiteSpace(targetRole))
            {
                return Result<UserRoleUpdateResultDto>.Failure(new[] { "Role cannot be empty or whitespace." });
            }

            // Delegate to UserRolesService for core role assignment - returns Result<UserRoleUpdateResultDto>
            var updateDto = new UpdateUserRolesDto
            {
                UserId = request.UserId,
                Role = targetRole // Single role assignment
            };

            var result = await _userRolesService.UpdateUserRolesAsync(updateDto);

            if (!result.Succeeded)
            {
                return Result<UserRoleUpdateResultDto>.Failure(result.Errors);
            }

            return Result<UserRoleUpdateResultDto>.Success(result.Value!);
        }
    }
}
