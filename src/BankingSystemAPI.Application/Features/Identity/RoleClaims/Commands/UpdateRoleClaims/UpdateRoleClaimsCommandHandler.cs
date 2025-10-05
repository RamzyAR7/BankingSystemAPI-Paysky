#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.RoleClaims.Commands.UpdateRoleClaims
{
    public sealed class UpdateRoleClaimsCommandHandler : ICommandHandler<UpdateRoleClaimsCommand, RoleClaimsUpdateResultDto>
    {
        private readonly IRoleClaimsService _roleClaimsService;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateRoleClaimsCommandHandler(
            IRoleClaimsService roleClaimsService,
            RoleManager<ApplicationRole> roleManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _roleClaimsService = roleClaimsService;
            _roleManager = roleManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<RoleClaimsUpdateResultDto>> Handle(UpdateRoleClaimsCommand request, CancellationToken cancellationToken)
        {
            // Business validation: Find role to get its name
            var role = await _roleManager.FindByIdAsync(request.RoleId);
            if (role == null)
            {
                return Result<RoleClaimsUpdateResultDto>.NotFound("Role", request.RoleId);
            }

            // Business validation: Prevent modifying SuperAdmin role claims
            if (role.Name == UserRole.SuperAdmin.ToString())
            {
                return Result<RoleClaimsUpdateResultDto>.Forbidden(AuthorizationConstants.ErrorMessages.CannotModifySuperAdminRoleClaims);
            }

            // Business validation: For Client role, only allow SuperAdmin to modify its claims
            if (role.Name == UserRole.Client.ToString())
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user == null || !user.IsInRole(UserRole.SuperAdmin.ToString()))
                {
                    return Result<RoleClaimsUpdateResultDto>.Forbidden(AuthorizationConstants.ErrorMessages.OnlySuperAdminCanModifyClientRoleClaims);
                }
            }

            // Delegate to RoleClaimsService for core claims update - returns Result<RoleClaimsUpdateResultDto>
            var updateDto = new UpdateRoleClaimsDto
            {
                RoleName = role.Name, // Use role name instead of ID
                Claims = request.Claims.ToList()
            };

            var result = await _roleClaimsService.UpdateRoleClaimsAsync(updateDto);

            // The service now returns Result<RoleClaimsUpdateResultDto> directly
            return result;
        }
    }
}
