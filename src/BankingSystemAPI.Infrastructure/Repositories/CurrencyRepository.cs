using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Application.Interfaces;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;
using BankingSystemAPI.Infrastructure.Services;

namespace BankingSystemAPI.Infrastructure.Repositories
{
    public class CurrencyRepository : GenericRepository<Currency, int>, ICurrencyRepository
    {
        private readonly ICacheService _cache;

        // Compiled query for GetById
        // EF.CompileQuery precompiles the LINQ expression into a delegate so EF Core doesn't have to re-translate the expression tree to SQL on every call.
        private static readonly Func<ApplicationDbContext, int, Currency> _compiledGetById =
            EF.CompileQuery((ApplicationDbContext ctx, int id) => ctx.Currencies.AsNoTracking().FirstOrDefault(c => c.Id == id));

        public CurrencyRepository(ApplicationDbContext context, ICacheService cache) : base(context)
        {
            _cache = cache;
        }

        public override async Task<Currency> GetByIdAsync(int id)
        {
            if (id <= 0) return null;
            if (_cache.TryGetValue($"currency_id_{id}", out Currency cached))
            {
                return cached;
            }

            var curr = _compiledGetById(_context, id);
            if (curr != null)
            {
                // lazy cache: only cache when read via GetByIdAsync
                _cache.Set($"currency_id_{id}", curr, TimeSpan.FromHours(1));
            }
            return curr;
        }

        public override async Task<Currency> UpdateAsync(Currency Entity)
        {
            var result = await base.UpdateAsync(Entity);

            if (result != null)
            {
                // evict id cache so next GetByIdAsync will refresh (lazy reload)
                _cache.Remove($"currency_id_{result.Id}");
            }
            return result;
        }

        public override Task DeleteAsync(Currency Entity)
        {
            _cache.Remove($"currency_id_{Entity.Id}");
            return base.DeleteAsync(Entity);
        }
    }
}
