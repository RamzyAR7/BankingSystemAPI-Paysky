using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.CreateUser
{
    public sealed record CreateUserCommand(UserReqDto UserRequest) : ICommand<UserResDto>;
}