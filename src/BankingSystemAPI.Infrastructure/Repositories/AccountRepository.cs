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
                .AsSplitQuery();
        }

        public IQueryable<Account> QueryByNationalId(string nationalId)
        {
            return Table.Where(a => a.User.NationalId == nationalId)
                .Include(a => a.User)
                .Include(a => a.Currency)
                .AsSplitQuery();
        }

        public async Task<IEnumerable<T>> GetAccountsByTypeAsync<T>(int pageNumber = 1, int pageSize = 10) where T : Account
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var skip = (pageNumber - 1) * pageSize;

            return await _context.Set<T>()
                .OrderBy(a => a.Id)  // Add explicit ordering before Skip/Take to fix split query issue
                .Include(a => a.Currency)
                .Skip(skip)
                .Take(pageSize)
                .AsNoTracking() 
                .ToListAsync();
        }

        public async Task<(IEnumerable<Account> Accounts, int TotalCount)> GetFilteredAccountsAsync(
            IQueryable<Account> query, 
            int pageNumber, 
            int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var skip = (pageNumber - 1) * pageSize;

            var totalCount = await query.CountAsync();
            
            var items = await query
                .OrderBy(a => a.Id)  // Add explicit ordering before Skip/Take to fix split query issue
                .Skip(skip)
                .Take(pageSize)
                .Include(a => a.Currency)
                .Include(a => a.User)
                .AsSplitQuery()
                .AsNoTracking() // Add AsNoTracking for better performance since we're not updating
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
