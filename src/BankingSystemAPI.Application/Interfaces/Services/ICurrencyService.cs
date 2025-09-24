using BankingSystemAPI.Application.DTOs.Currency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Services
{
    public interface ICurrencyService
    {
        Task<IEnumerable<CurrencyDto>> GetAllAsync();
        Task<CurrencyDto> GetByIdAsync(int id);
        Task<CurrencyDto> CreateAsync(CurrencyReqDto reqDto);
        Task<CurrencyDto> UpdateAsync(int id, CurrencyReqDto reqDto);
        Task DeleteAsync(int id);
        Task SetCurrencyActiveStatusAsync(int currencyId, bool isActive);
    }
}
