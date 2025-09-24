using BankingSystemAPI.Application.DTOs.InterestLog;
using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Services
{
    public interface ISavingsAccountService : IAccountTypeService<SavingsAccount, SavingsAccountReqDto, SavingsAccountEditDto, SavingsAccountDto>
    {
        Task<(IEnumerable<InterestLogDto> logs, int totalCount)> GetAllInterestLogsAsync(int pageNumber, int pageSize);
        Task<(IEnumerable<InterestLogDto> logs, int totalCount)> GetInterestLogsByAccountIdAsync(int accountId, int pageNumber, int pageSize);
    }
}
