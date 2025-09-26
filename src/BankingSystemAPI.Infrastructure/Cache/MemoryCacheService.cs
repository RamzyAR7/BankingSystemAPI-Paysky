using BankingSystemAPI.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;


namespace BankingSystemAPI.Infrastructure.Services
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly MemoryCacheEntryOptions _defaultOptions = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };

        public MemoryCacheService(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public bool TryGetValue<T>(object key, out T value)
        {
            if (_cache.TryGetValue(key, out var raw))
            {
                value = (T)raw;
                return true;
            }
            value = default!;
            return false;
        }

        public void Set<T>(object key, T value, TimeSpan? absoluteExpirationRelativeToNow = null)
        {
            var options = absoluteExpirationRelativeToNow.HasValue ? new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow } : _defaultOptions;
            _cache.Set(key, value, options);
        }

        public T GetOrCreate<T>(object key, Func<T> factory, TimeSpan? absoluteExpirationRelativeToNow = null)
        {
            if (TryGetValue<T>(key, out var existing))
                return existing;

            var created = factory();
            Set(key, created, absoluteExpirationRelativeToNow);
            return created;
        }

        public void Remove(object key) => _cache.Remove(key);
    }
}
