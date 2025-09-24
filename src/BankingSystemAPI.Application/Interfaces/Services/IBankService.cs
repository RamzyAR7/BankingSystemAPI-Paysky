using System.Threading.Tasks;
using System.Collections.Generic;
using BankingSystemAPI.Application.DTOs.Bank;

namespace BankingSystemAPI.Application.Interfaces.Services
{
    public interface IBankService
    {
        Task<List<BankSimpleResDto>> GetAllAsync(int pageNumber = 1, int pageSize = 10);
        Task<BankResDto> GetByIdAsync(int id);
        Task<BankResDto> GetByNameAsync(string name);
        Task<BankResDto> CreateAsync(BankReqDto dto);
        Task<BankResDto> UpdateAsync(int id, BankEditDto dto);
        Task<bool> DeleteAsync(int id);
        Task SetBankActiveStatusAsync(int id, bool isActive);
    }
}
