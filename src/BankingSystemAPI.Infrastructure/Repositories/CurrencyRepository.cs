#region Usings
using BankingSystemAPI.Application.Interfaces.Repositories;
using BankingSystemAPI.Application.Interfaces;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using BankingSystemAPI.Application.Specifications;
#endregion


namespace BankingSystemAPI.Infrastructure.Repositories
{
    public class CurrencyRepository : GenericRepository<Currency, int>, ICurrencyRepository
    {
    #region Fields
    #endregion

    #region Constructors
    #endregion

    #region Properties
    #endregion

    #region Methods
    #endregion
        private readonly ICacheService _cache;

        public CurrencyRepository(ApplicationDbContext context, ICacheService cache) : base(context)
        {
            _cache = cache;
        }

        public async Task<Currency> GetBaseCurrencyAsync()
        {
            // Try cache first
            if (_cache.TryGetValue<Currency>("base_currency", out var cachedCurrency))
                return cachedCurrency;

            var baseCurrency = await Table
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IsBase);

            if (baseCurrency != null)
            {
                _cache.Set("base_currency", baseCurrency, TimeSpan.FromHours(1));
            }

            return baseCurrency;
        }

        public async Task<Dictionary<int, decimal>> GetExchangeRatesAsync(IEnumerable<int> currencyIds)
        {
            var ids = currencyIds.ToList();
            if (!ids.Any()) return new Dictionary<int, decimal>();

            return await Table
                .Where(c => ids.Contains(c.Id))
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Id, c => c.ExchangeRate);
        }

        public override async Task<Currency> UpdateAsync(Currency entity)
        {
            var result = await base.UpdateAsync(entity);
            
            // Clear relevant cache entries
            if (entity.IsBase)
                _cache.Remove("base_currency");
                
            return result;
        }

        public override async Task DeleteAsync(Currency entity)
        {
            if (entity.IsBase)
            {
                _cache.Remove("base_currency");
            }

            await base.DeleteAsync(entity);
        }

        public override async Task DeleteRangeAsync(IEnumerable<Currency> entities)
        {
            if (entities.Any(e => e.IsBase))
            {
                _cache.Remove("base_currency");
            }

            await base.DeleteRangeAsync(entities);
        }
    }
}

