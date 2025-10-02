using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Application.Features.Identity.Roles.Commands.DeleteRole
{
    public sealed class DeleteRoleCommandHandler : ICommandHandler<DeleteRoleCommand, RoleUpdateResultDto>
    {
        private readonly IRoleService _roleService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public DeleteRoleCommandHandler(
            IRoleService roleService, 
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _roleService = roleService;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<Result<RoleUpdateResultDto>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            // Business validation: Check if role has users assigned
            var role = await _roleManager.FindByIdAsync(request.RoleId);
            if (role == null)
            {
                return Result<RoleUpdateResultDto>.Failure(new[] { "Role not found." });
            }

            // Check both FK relationship and UserRoles table
            var hasUsersViaFk = await _userManager.Users.AnyAsync(u => u.RoleId == request.RoleId, cancellationToken);
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            
            if (hasUsersViaFk || usersInRole.Any())
            {
                return Result<RoleUpdateResultDto>.Failure(new[] { "Cannot delete role because it is assigned to one or more users." });
            }

            // Delegate to RoleService for core role deletion - returns Result<RoleUpdateResultDto>
            var result = await _roleService.DeleteRoleAsync(request.RoleId);

            if (!result.Succeeded)
            {
                return Result<RoleUpdateResultDto>.Failure(result.Errors);
            }

            return Result<RoleUpdateResultDto>.Success(result.Value!);
        }
    }
}