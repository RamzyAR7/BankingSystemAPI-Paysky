#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Domain.Constant;
#endregion


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
            // Input validation: Check for empty/null role name
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result<RoleUpdateResultDto>.ValidationFailed("Role name cannot be null or empty.");
            }
            
            // Business validation: Check if role already exists
            var uniquenessValidation = await ValidateRoleUniquenessAsync(request.Name);
            if (uniquenessValidation.IsFailure)
                return Result<RoleUpdateResultDto>.Failure(uniquenessValidation.ErrorItems);

            var createResult = await CreateRoleAsync(request.Name);
            
            // Add side effects using ResultExtensions
            createResult.OnSuccess(() => 
                {
                    _logger.LogInformation(ApiResponseMessages.Logging.RoleCreated, request.Name);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning(ApiResponseMessages.Logging.RoleCreateFailed, request.Name, string.Join(", ", errors));
                });

            return createResult;
        }

        private async Task<Result> ValidateRoleUniquenessAsync(string roleName)
        {
            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            return roleExists
                ? Result.Failure(ErrorType.Validation, string.Format("Role '{0}' already exists.", roleName))
                : Result.Success();
        }

        private async Task<Result<RoleUpdateResultDto>> CreateRoleAsync(string roleName)
        {
            var roleDto = new RoleReqDto { Name = roleName };
            var result = await _roleService.CreateRoleAsync(roleDto);

            // The service now returns Result<RoleUpdateResultDto> directly
            return result;
        }
    }
}
