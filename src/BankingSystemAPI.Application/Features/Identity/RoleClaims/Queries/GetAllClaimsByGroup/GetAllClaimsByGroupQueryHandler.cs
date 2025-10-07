#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Identity;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Identity.RoleClaims.Queries.GetAllClaimsByGroup
{
    public sealed class GetAllClaimsByGroupQueryHandler : IQueryHandler<GetAllClaimsByGroupQuery, ICollection<RoleClaimsResDto>>
    {
        private readonly IRoleClaimsService _roleClaimsService;
        private readonly ICurrentUserService _currentUserService;

        public GetAllClaimsByGroupQueryHandler(IRoleClaimsService roleClaimsService, ICurrentUserService currentUserService)
        {
            _roleClaimsService = roleClaimsService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<ICollection<RoleClaimsResDto>>> Handle(GetAllClaimsByGroupQuery request, CancellationToken cancellationToken)
        {
            // Business validation: Only SuperAdmin and Admin can view role claims
            var userRole = await _currentUserService.GetRoleFromStoreAsync();
            var isSuperAdmin = string.Equals(userRole.Name, UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase);
            var isAdmin = string.Equals(userRole.Name, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase);

            if (!isSuperAdmin && !isAdmin)
            {
                return Result<ICollection<RoleClaimsResDto>>.Forbidden(ApiResponseMessages.ErrorPatterns.AccessDenied);
            }

            // Delegate to RoleClaimsService - returns Result<ICollection<RoleClaimsResDto>>
            var claimsResult = await _roleClaimsService.GetAllClaimsByGroup();

            if (!claimsResult.IsSuccess)
            {
                return Result<ICollection<RoleClaimsResDto>>.Failure(claimsResult.ErrorItems);
            }

            return Result<ICollection<RoleClaimsResDto>>.Success(claimsResult.Value!);
        }
    }
}
