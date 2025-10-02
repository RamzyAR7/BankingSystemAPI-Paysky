using BankingSystemAPI.Application.Interfaces.Messaging;

namespace BankingSystemAPI.Application.Features.Identity.Users.Commands.DeleteUsers
{
    public sealed record DeleteUsersCommand(IEnumerable<string> UserIds) : ICommand;
}