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
        private readonly DbSet<Account> _accountDbSet;
        public AccountRepository(ApplicationDbContext context) : base(context)
        {
            _accountDbSet = context.Set<Account>();
        }

        public async Task<IEnumerable<T>> GetAccountsByTypeAsync<T>(int pageNumber = 1, int pageSize = 10) where T : Account
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var skip = (pageNumber - 1) * pageSize;

            Expression<Func<Account, bool>> predicate = a => a is T;

            // Use GetPagedAsync and expression-based include for Currency
            var (items, total) = await GetPagedAsync(predicate, pageSize, skip, (Expression<Func<Account, object>>)(a => a.Id), "ASC", new[] { (Expression<Func<Account, object>>)(a => a.Currency) });

            return items.OfType<T>();
        }
    }
}
