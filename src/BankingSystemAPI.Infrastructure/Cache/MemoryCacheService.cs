using BankingSystemAPI.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BankingSystemAPI.Infrastructure.Cache
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheEntryOptions _defaultCacheOptions =  new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
        public MemoryCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }
        public bool TryGetValue<T>(object key, out T value)
        {
            if(_memoryCache.TryGetValue(key, out var cached))
            {
                value = (T)cached;
                return true;
            }
            value = default;
            return false;
        }
        public void Set<T>(object key, T value, TimeSpan? absoluteExpirationRelativeToNow = null)
        {
            var options =  new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow } ?? _defaultCacheOptions;
            _memoryCache.Set(key, value, options);
        }
        public void Remove(object key)
        {
            _memoryCache.Remove(key);
        }
    }
}
