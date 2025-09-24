using BankingSystemAPI.Application.DTOs.Account;
using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Services
{
    public interface IAccountService
    {
        Task<AccountDto> GetAccountByIdAsync(int id);
        Task<AccountDto> GetAccountByAccountNumberAsync(string accountNumber);
        Task<IEnumerable<AccountDto>> GetAccountsByUserIdAsync(string userId);
        Task<IEnumerable<AccountDto>> GetAccountsByNationalIdAsync(string nationalId);
        Task DeleteAccountAsync(int id);
        Task DeleteAccountsAsync(IEnumerable<int> ids);
        Task SetAccountActiveStatusAsync(int accountId, bool isActive);
    }
}
