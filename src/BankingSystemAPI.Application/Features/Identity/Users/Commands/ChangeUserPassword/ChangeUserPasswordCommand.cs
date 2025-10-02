using BankingSystemAPI.Application.DTOs.User;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.ChangeUserPassword
{
    public sealed record ChangeUserPasswordCommand(string UserId, ChangePasswordReqDto PasswordRequest) : ICommand<UserResDto>;
}