using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.SetUserActiveStatus
{
    public sealed record SetUserActiveStatusCommand(
        string UserId,
        bool IsActive) : ICommand;
}