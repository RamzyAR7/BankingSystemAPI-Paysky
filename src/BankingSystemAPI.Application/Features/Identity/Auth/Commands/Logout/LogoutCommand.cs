using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Auth.Commands.Logout
{
    public sealed record LogoutCommand(string UserId) : ICommand<AuthResultDto>;
}