using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.RoleClaims.Queries.GetAllClaimsByGroup
{
    public sealed record GetAllClaimsByGroupQuery : IQuery<ICollection<RoleClaimsResDto>>;
}