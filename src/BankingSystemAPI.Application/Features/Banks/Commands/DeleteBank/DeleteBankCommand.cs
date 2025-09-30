using BankingSystemAPI.Application.Interfaces.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BankingSystemAPI.Application.Features.Banks.Commands.DeleteBank
{
    public record DeleteBankCommand(int id): ICommand<bool>
    {
    }
}
