using BankingSystemAPI.Application.Interfaces;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BankingSystemAPI.Infrastructure.Cache
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MemoryCacheService> _logger;
        private readonly MemoryCacheEntryOptions _defaultCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public bool TryGetValue<T>(object key, out T value)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out var cached))
                {
                    value = (T)cached;
                    
                    // Use ResultExtensions for consistent logging
                    var result = Result<T>.Success(value);
                    result.OnSuccess(() => 
                        _logger.LogDebug("[CACHE] Cache hit: Key={Key}, Type={Type}", key, typeof(T).Name));
                    
                    return true;
                }
                
                value = default;
                
                // Log cache miss using ResultExtensions patterns
                var missResult = Result<T>.BadRequest("Cache miss");
                missResult.OnFailure(errors => 
                    _logger.LogDebug("[CACHE] Cache miss: Key={Key}, Type={Type}", key, typeof(T).Name));
                
                return false;
            }
            catch (Exception ex)
            {
                value = default;
                
                // Use ResultExtensions for error logging
                var errorResult = Result<T>.BadRequest($"Cache access error: {ex.Message}");
                errorResult.OnFailure(errors => 
                    _logger.LogWarning(ex, "[CACHE] Cache access failed: Key={Key}, Type={Type}", key, typeof(T).Name));
                
                return false;
            }
        }

        public void Set<T>(object key, T value, TimeSpan? absoluteExpirationRelativeToNow = null)
        {
            try
            {
                var options = new MemoryCacheEntryOptions 
                { 
                    AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow 
                } ?? _defaultCacheOptions;
                
                _memoryCache.Set(key, value, options);
                
                // Use ResultExtensions for successful cache set logging
                var result = Result.Success();
                result.OnSuccess(() => 
                    _logger.LogDebug("[CACHE] Cache set: Key={Key}, Type={Type}, Expiration={Expiration}", 
                        key, typeof(T).Name, options.AbsoluteExpirationRelativeToNow));
            }
            catch (Exception ex)
            {
                // Use ResultExtensions for error logging
                var errorResult = Result.BadRequest($"Cache set error: {ex.Message}");
                errorResult.OnFailure(errors => 
                    _logger.LogError(ex, "[CACHE] Failed to set cache: Key={Key}, Type={Type}", key, typeof(T).Name));
            }
        }

        public void Remove(object key)
        {
            try
            {
                _memoryCache.Remove(key);
                
                // Use ResultExtensions for successful removal logging
                var result = Result.Success();
                result.OnSuccess(() => 
                    _logger.LogDebug("[CACHE] Cache removed: Key={Key}", key));
            }
            catch (Exception ex)
            {
                // Use ResultExtensions for error logging
                var errorResult = Result.BadRequest($"Cache removal error: {ex.Message}");
                errorResult.OnFailure(errors => 
                    _logger.LogWarning(ex, "[CACHE] Failed to remove cache: Key={Key}", key));
            }
        }
    }
}
