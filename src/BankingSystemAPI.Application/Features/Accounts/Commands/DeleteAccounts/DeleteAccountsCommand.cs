using BankingSystemAPI.Application.Interfaces.Messaging;
using System.Collections.Generic;

namespace BankingSystemAPI.Application.Features.Accounts.Commands.DeleteAccounts
{
    public record DeleteAccountsCommand(IEnumerable<int> Ids) : ICommand<bool>;
}
