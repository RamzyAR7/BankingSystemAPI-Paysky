using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace BankingSystemAPI.Application.Features.Identity.Roles.Commands.CreateRole
{
    public sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, RoleUpdateResultDto>
    {
        private readonly IRoleService _roleService;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public CreateRoleCommandHandler(IRoleService roleService, RoleManager<ApplicationRole> roleManager)
        {
            _roleService = roleService;
            _roleManager = roleManager;
        }

        public async Task<Result<RoleUpdateResultDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            // Business validation: Check if role name is provided
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<RoleUpdateResultDto>.Failure(new[] { "Role name cannot be null or empty." });
            }

            // Business validation: Check if role already exists
            var roleExists = await _roleManager.RoleExistsAsync(request.Name);
            if (roleExists)
            {
                return Result<RoleUpdateResultDto>.Failure(new[] { $"Role '{request.Name}' already exists." });
            }

            // Delegate to RoleService for core role creation - returns Result<RoleUpdateResultDto>
            var roleDto = new RoleReqDto { Name = request.Name };
            var result = await _roleService.CreateRoleAsync(roleDto);

            if (!result.Succeeded)
            {
                return Result<RoleUpdateResultDto>.Failure(result.Errors);
            }

            return Result<RoleUpdateResultDto>.Success(result.Value!);
        }
    }
}