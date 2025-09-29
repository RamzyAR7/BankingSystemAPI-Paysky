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
using BankingSystemAPI.Application.Specifications;
using BankingSystemAPI.Application.Specifications.AccountSpecification;

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

            var skip = (pageNumber - 1) * pageSize;

            // Use generic PagedSpecification to handle includes/paging
            var spec = new PagedSpecification<Account>(a => a is T, skip, pageSize, orderByProperty: null, orderDirection: null, includes: (a => a.Currency));
            var (items, total) = await GetPagedAsync(spec);

            return items.OfType<T>();
        }

        public async Task<(IEnumerable<Account> Accounts, int TotalCount)> GetFilteredAccountsAsync(IQueryable<Account> query, int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var skip = (pageNumber - 1) * pageSize;

            // Make sure we operate on an IQueryable that can execute against EF
            var baseQuery = query;

            var total = await baseQuery.CountAsync();
            var items = await baseQuery.Skip(skip).Take(pageSize).ToListAsync();

            return (items, total);
        }
    }
}
