using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.Interfaces.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Features.Banks.Queries.GetBankById
{
    public sealed record GetBankByIdQuery(int id) : IQuery<BankResDto>;
}
