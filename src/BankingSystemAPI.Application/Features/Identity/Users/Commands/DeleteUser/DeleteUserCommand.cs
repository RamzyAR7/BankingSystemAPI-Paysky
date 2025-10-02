using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.DeleteUser
{
    public sealed record DeleteUserCommand(string UserId) : ICommand<UserResDto>;
}