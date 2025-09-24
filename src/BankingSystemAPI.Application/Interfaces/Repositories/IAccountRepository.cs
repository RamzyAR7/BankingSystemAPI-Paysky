using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Interfaces.Repositories
{
    public interface IAccountRepository: IGenericRepository<Account>
    {
        Task<Account> GetAccountByIdAsync(int id);
        Task<Account> GetAccountByAccountNumberAsync(string accountNumber);
        Task<IEnumerable<Account>> GetAccountsByUserIdAsync(string userId);
        Task<IEnumerable<Account>> GetAccountsByNationalIdAsync(string nationalId);
        Task<IEnumerable<T>> GetAccountsByTypeAsync<T>(int pageNumber = 1, int pageSize = 10) where T : Account;
    }
}
