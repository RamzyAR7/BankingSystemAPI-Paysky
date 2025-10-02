using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Roles.Commands.CreateRole
{
    public sealed record CreateRoleCommand(string Name) : ICommand<RoleUpdateResultDto>;
}