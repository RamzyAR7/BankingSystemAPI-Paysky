using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BankingSystemAPI.Application.Features.Identity.Roles.Commands.CreateRole
{
    public sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, RoleUpdateResultDto>
    {
        private readonly IRoleService _roleService;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<CreateRoleCommandHandler> _logger;

        public CreateRoleCommandHandler(
            IRoleService roleService, 
            RoleManager<ApplicationRole> roleManager,
            ILogger<CreateRoleCommandHandler> logger)
        {
            _roleService = roleService;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<Result<RoleUpdateResultDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            // Note: Input validation (empty/null) is handled by CreateRoleCommandValidator
            // This handler focuses on business logic validation and execution
            
            // Business validation: Check if role already exists
            var uniquenessValidation = await ValidateRoleUniquenessAsync(request.Name);
            if (uniquenessValidation.IsFailure)
                return Result<RoleUpdateResultDto>.Failure(uniquenessValidation.Errors);

            var createResult = await CreateRoleAsync(request.Name);
            
            // Add side effects using ResultExtensions
            createResult.OnSuccess(() => 
                {
                    _logger.LogInformation("Role created successfully: {RoleName}", request.Name);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("Role creation failed for: {RoleName}. Errors: {Errors}",
                        request.Name, string.Join(", ", errors));
                });

            return createResult;
        }

        private async Task<Result> ValidateRoleUniquenessAsync(string roleName)
        {
            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            return roleExists
                ? Result.BadRequest($"Role '{roleName}' already exists.")
                : Result.Success();
        }

        private async Task<Result<RoleUpdateResultDto>> CreateRoleAsync(string roleName)
        {
            var roleDto = new RoleReqDto { Name = roleName };
            var result = await _roleService.CreateRoleAsync(roleDto);

            return result.Succeeded
                ? Result<RoleUpdateResultDto>.Success(result.Value!)
                : Result<RoleUpdateResultDto>.Failure(result.Errors);
        }
    }
}