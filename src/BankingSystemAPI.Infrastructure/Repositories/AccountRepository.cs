using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Infrastructure.Repositories
{
    public class AccountRepository : GenericRepository<Account>, IAccountRepository
    {
        private readonly DbSet<Account> _accountDbSet;
        public AccountRepository(ApplicationDbContext context) : base(context)
        {
            _accountDbSet = context.Set<Account>();
        }

        public async Task<IEnumerable<Account>> GetAccountsByNationalIdAsync(string nationalId)
        {
            return await _accountDbSet.AsNoTracking().Include(a=> a.User).Where(a => a.User.NationalId == nationalId).ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAccountsByTypeAsync<T>(int pageNumber = 1, int pageSize = 10) where T : Account
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var skip = (pageNumber - 1) * pageSize;

            Expression<Func<Account, bool>> predicate = a => a is T;

            // Ensure Currency navigation is included so mapping can populate CurrencyCode in DTOs
            var results = await FindAllAsync(predicate, take: pageSize, skip: skip, orderBy: (Expression<Func<Account, object>>)(a => a.Id), orderByDirection: "ASC", Includes: new[] { "Currency" });

            return results.OfType<T>();
        }

        public async Task<IEnumerable<Account>> GetAccountsByUserIdAsync(string userId)
        {
            return await _accountDbSet.AsNoTracking().Where(a => a.UserId == userId).ToListAsync();
        }
        public async Task<Account> GetAccountByIdAsync(int id)
        {
            return await _accountDbSet.FindAsync(id);
        }   

        public async Task<Account> GetAccountByAccountNumberAsync(string accountNumber)
        {
            return await _accountDbSet.AsNoTracking().FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        }
    }
}
