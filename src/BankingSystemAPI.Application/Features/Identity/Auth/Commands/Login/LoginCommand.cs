using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Auth.Commands.Login
{
    public sealed record LoginCommand(
        string Email,
        string Password) : ICommand<AuthResultDto>;
}