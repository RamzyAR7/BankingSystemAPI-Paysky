using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.RoleClaims.Commands.UpdateRoleClaims
{
    public sealed record UpdateRoleClaimsCommand(
        string RoleId,
        ICollection<string> Claims) : ICommand<RoleClaimsUpdateResultDto>;
}