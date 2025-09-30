using BankingSystemAPI.Application.DTOs.Bank;
using BankingSystemAPI.Application.Interfaces.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Features.Banks.Queries.GetAllBanks
{
    public record GetAllBanksQuery(
        int PageNumber = 1,
        int PageSize = 10,
        string? OrderBy = null,
        string? OrderDirection = null
    ) : IQuery<List<BankSimpleResDto>>;
}
