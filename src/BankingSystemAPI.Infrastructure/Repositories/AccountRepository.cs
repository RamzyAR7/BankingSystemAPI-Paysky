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
    public class AccountRepository : GenericRepository<Account, int>, IAccountRepository
    {
        public AccountRepository(ApplicationDbContext context) : base(context)
        {
        }

        public IQueryable<Account> QueryByUserId(string userId)
        {
            return Table.Where(a => a.UserId == userId)
                .Include(a => a.Currency)
                .AsQueryable();
        }

        public IQueryable<Account> QueryByNationalId(string nationalId)
        {
            return Table.Where(a => a.User.NationalId == nationalId)
                .Include(a => a.User)
                .Include(a => a.Currency)
                .AsQueryable();
        }

        public async Task<IEnumerable<T>> GetAccountsByTypeAsync<T>(int pageNumber = 1, int pageSize = 10) where T : Account
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            Expression<Func<Account, bool>> predicate = a => a is T;

            // Use GetPagedAsync(IQueryable,...) for paging
            var baseQuery = Table.Where(predicate).Include(a => a.Currency).AsQueryable();
            var (items, total) = await GetPagedAsync(baseQuery, pageNumber, pageSize);

            return items.OfType<T>();
        }

        public async Task<(IEnumerable<Account> Accounts, int TotalCount)> GetFilteredAccountsAsync(IQueryable<Account> query, int pageNumber, int pageSize)
        {
            return await GetPagedAsync(query, pageNumber, pageSize);
        }
    }
}
