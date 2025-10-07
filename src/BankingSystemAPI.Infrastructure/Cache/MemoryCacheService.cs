#region Usings
using BankingSystemAPI.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Domain.Constant;
using System;
#endregion


namespace BankingSystemAPI.Infrastructure.Cache
{
    public class MemoryCacheService : ICacheService
    {
        #region Fields
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly MemoryCacheEntryOptions _defaultCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
        #endregion

        #region Constructor
        public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }
        #endregion

        #region Public Methods
        public bool TryGetValue<T>(object key, out T value)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out var cached))
                {
                    if (cached is T typed)
                    {
                        value = typed;
                    }
                    else
                    {
                        value = default!;
                    }
                    _logger.LogDebug("[CACHE] Cache hit: Key={Key}, Type={Type}", key, typeof(T).Name);
                    return true;
                }

                value = default!;
                _logger.LogDebug("[CACHE] Cache miss: Key={Key}, Type={Type}", key, typeof(T).Name);
                return false;
            }
            catch (Exception ex)
            {
                value = default!;
                _logger.LogWarning(ex, "[CACHE] Cache access failed: Key={Key}, Type={Type}", key, typeof(T).Name);
                return false;
            }
        }

        public void Set<T>(object key, T value, TimeSpan? absoluteExpirationRelativeToNow = null)
        {
            try
            {
                var options = absoluteExpirationRelativeToNow.HasValue
                    ? new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow.Value }
                    : _defaultCacheOptions;

                _memoryCache.Set(key, value, options);
                _logger.LogDebug("[CACHE] Cache set: Key={Key}, Type={Type}, Expiration={Expiration}", key, typeof(T).Name, options.AbsoluteExpirationRelativeToNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CACHE] Failed to set cache: Key={Key}, Type={Type}", key, typeof(T).Name);
            }
        }

        public void Remove(object key)
        {
            try
            {
                _memoryCache.Remove(key);
                _logger.LogDebug("[CACHE] Cache removed: Key={Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[CACHE] Failed to remove cache: Key={Key}", key);
            }
        }
        #endregion
    }
}

