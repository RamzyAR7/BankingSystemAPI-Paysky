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
        private static readonly Func<ApplicationDbContext, int, Currency> _compiledGetById =
            EF.CompileQuery((ApplicationDbContext ctx, int id) => ctx.Currencies.AsNoTracking().FirstOrDefault(c => c.Id == id));

        private static readonly Func<ApplicationDbContext, string, Currency> _compiledGetByCode =
            EF.CompileQuery((ApplicationDbContext ctx, string code) => ctx.Currencies.AsNoTracking().FirstOrDefault(c => c.Code == code));

        private readonly ICacheService _cache;
        private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(10);

        public CurrencyRepository(ApplicationDbContext context, ICacheService cache) : base(context)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public override async Task<Currency> GetByIdAsync(int id)
        {
            if (id <= 0) return null;

            if (_cache.TryGetValue($"currency_id_{id}", out Currency cached))
                return cached;

            var curr = _compiledGetById(_context, id);
            if (curr != null)
                _cache.Set($"currency_id_{id}", curr, _defaultTtl);
            return curr;
        }

        public async Task<Currency> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            if (_cache.TryGetValue($"currency_code_{code}", out Currency cached))
                return cached;

            var curr = _compiledGetByCode(_context, code);
            if (curr != null)
                _cache.Set($"currency_code_{code}", curr, _defaultTtl);
            return curr;
        }

        // Ensure cache is updated/invalidated when currencies change
        public override async Task<Currency> AddAsync(Currency Entity)
        {
            var result = await base.AddAsync(Entity);
            if (result != null)
            {
                _cache.Remove($"currency_id_{result.Id}");
                if (!string.IsNullOrWhiteSpace(result.Code))
                    _cache.Remove($"currency_code_{result.Code}");
            }
            return result;
        }

        public override async Task<Currency> UpdateAsync(Currency Entity)
        {
            var result = await base.UpdateAsync(Entity);
            if (result != null)
            {
                _cache.Remove($"currency_id_{result.Id}");
                if (!string.IsNullOrWhiteSpace(result.Code))
                    _cache.Remove($"currency_code_{result.Code}");
            }
            return result;
        }

        public override async Task DeleteAsync(Currency Entity)
        {
            // capture keys before deletion
            var id = Entity?.Id ?? 0;
            var code = Entity?.Code; 

            await base.DeleteAsync(Entity);

            if (id > 0)
                _cache.Remove($"currency_id_{id}");
            if (!string.IsNullOrWhiteSpace(code))
                _cache.Remove($"currency_code_{code}");
        }
    }
}
