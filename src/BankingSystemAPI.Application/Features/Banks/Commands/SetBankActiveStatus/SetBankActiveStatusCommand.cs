using BankingSystemAPI.Application.Interfaces.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Features.Banks.Commands.SetBankActiveStatus
{
    public record SetBankActiveStatusCommand(int id , bool isActive): ICommand<bool>
    {

    }
}
