using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Roles.Queries.GetAllRoles
{
    public sealed record GetAllRolesQuery : IQuery<List<RoleResDto>>;
}