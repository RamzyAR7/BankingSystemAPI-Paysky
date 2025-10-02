using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Auth.Commands.RevokeToken
{
    public sealed record RevokeTokenCommand(string UserId) : ICommand<AuthResultDto>;
}