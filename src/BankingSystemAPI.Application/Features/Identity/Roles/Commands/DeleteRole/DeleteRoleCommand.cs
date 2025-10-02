using BankingSystemAPI.Application.DTOs.Role;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Roles.Commands.DeleteRole
{
    public sealed record DeleteRoleCommand(string RoleId) : ICommand<RoleUpdateResultDto>;
}