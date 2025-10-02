using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.UpdateUser
{
    public sealed record UpdateUserCommand(string UserId, UserEditDto UserEdit) : ICommand<UserResDto>;
}