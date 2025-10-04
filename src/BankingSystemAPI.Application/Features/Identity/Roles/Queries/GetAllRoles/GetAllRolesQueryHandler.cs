using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;
using Microsoft.Extensions.Logging;

namespace BankingSystemAPI.Application.Features.Identity.Roles.Queries.GetAllRoles
{
    public sealed class GetAllRolesQueryHandler : IQueryHandler<GetAllRolesQuery, List<RoleResDto>>
    {
        private readonly IRoleService _roleService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetAllRolesQueryHandler> _logger;

        public GetAllRolesQueryHandler(
            IRoleService roleService, 
            ICurrentUserService currentUserService,
            ILogger<GetAllRolesQueryHandler> logger)
        {
            _roleService = roleService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<List<RoleResDto>>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            // Chain authorization and role retrieval using ResultExtensions
            var authorizationResult = await ValidateAuthorizationAsync();
            if (authorizationResult.IsFailure)
                return Result<List<RoleResDto>>.Failure(authorizationResult.Errors);

            var rolesResult = await RetrieveRolesAsync();
            
            // Add side effects using ResultExtensions
            rolesResult.OnSuccess(() => 
                {
                    _logger.LogDebug("Roles retrieved successfully: Count={Count}, UserId={UserId}", 
                        rolesResult.Value!.Count, _currentUserService.UserId);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("Roles retrieval failed: UserId={UserId}, Errors={Errors}",
                        _currentUserService.UserId, string.Join(", ", errors));
                });

            return rolesResult;
        }

        private async Task<Result> ValidateAuthorizationAsync()
        {
            try
            {
                var userRole = await _currentUserService.GetRoleFromStoreAsync();
                var authorizationResult = ValidateRolePermissions(userRole.Name);
                
                authorizationResult.OnSuccess(() => 
                    {
                        _logger.LogDebug("[AUTHORIZATION] Role retrieval authorized: UserId={UserId}, Role={Role}", 
                            _currentUserService.UserId, userRole.Name);
                    })
                    .OnFailure(errors => 
                    {
                        _logger.LogWarning("[AUTHORIZATION] Role retrieval denied: UserId={UserId}, Role={Role}, Errors={Errors}",
                            _currentUserService.UserId, userRole.Name, string.Join(", ", errors));
                    });

                return authorizationResult;
            }
            catch (Exception ex)
            {
                return Result.BadRequest($"Failed to validate authorization: {ex.Message}");
            }
        }

        private Result ValidateRolePermissions(string? userRoleName)
        {
            var isSuperAdmin = string.Equals(userRoleName, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase);
            var isAdmin = string.Equals(userRoleName, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase);

            return (isSuperAdmin || isAdmin)
                ? Result.Success()
                : Result.Forbidden("Insufficient permissions to view roles.");
        }

        private async Task<Result<List<RoleResDto>>> RetrieveRolesAsync()
        {
            var rolesResult = await _roleService.GetAllRolesAsync();
            return rolesResult.IsSuccess
                ? Result<List<RoleResDto>>.Success(rolesResult.Value!)
                : Result<List<RoleResDto>>.Failure(rolesResult.Errors);
        }
    }
}