using BankingSystemAPI.Application.DTOs.Auth;
using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Auth.Commands.RefreshToken
{
    public sealed record RefreshTokenCommand(string? Token) : ICommand<AuthResultDto>;
}