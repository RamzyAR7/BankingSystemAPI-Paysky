using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;

namespace BankingSystemAPI.Application.Features.Identity.Roles.Queries.GetAllRoles
{
    public sealed class GetAllRolesQueryHandler : IQueryHandler<GetAllRolesQuery, List<RoleResDto>>
    {
        private readonly IRoleService _roleService;
        private readonly ICurrentUserService _currentUserService;

        public GetAllRolesQueryHandler(IRoleService roleService, ICurrentUserService currentUserService)
        {
            _roleService = roleService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<List<RoleResDto>>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            // Business validation: Only SuperAdmin and Admin can view all roles
            var userRole = await _currentUserService.GetRoleFromStoreAsync();
            var isSuperAdmin = string.Equals(userRole.Name, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase);
            var isAdmin = string.Equals(userRole.Name, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase);

            if (!isSuperAdmin && !isAdmin)
            {
                return Result<List<RoleResDto>>.Failure(new[] { "Insufficient permissions to view roles." });
            }

            // Delegate to RoleService - returns Result<List<RoleResDto>>
            var rolesResult = await _roleService.GetAllRolesAsync();
            
            if (!rolesResult.Succeeded)
            {
                return Result<List<RoleResDto>>.Failure(rolesResult.Errors);
            }

            return Result<List<RoleResDto>>.Success(rolesResult.Value!);
        }
    }
}