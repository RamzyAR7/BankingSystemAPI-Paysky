using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.UserRoles.Commands.UpdateUserRoles
{
    public sealed record UpdateUserRolesCommand(
        string UserId,
        ICollection<string> Roles) : ICommand<UserRoleUpdateResultDto>;
}