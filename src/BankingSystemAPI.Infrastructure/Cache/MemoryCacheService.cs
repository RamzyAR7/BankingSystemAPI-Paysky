#region Usings
using BankingSystemAPI.Application.Interfaces;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Domain.Constant;
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
                    value = (T)cached;
                    
                    // Use ResultExtensions for consistent logging
                    var result = Result<T>.Success(value);
                    result.OnSuccess(() => 
                        _logger.LogDebug("[CACHE] Cache hit: Key={Key}, Type={Type}", key, typeof(T).Name));
                    
                    return true;
                }
                
                value = default;
                
                // Log cache miss using centralized constant
                var missResult = Result<T>.BadRequest(ApiResponseMessages.Infrastructure.CacheMiss);
                missResult.OnFailure(errors => 
                    _logger.LogDebug("[CACHE] Cache miss: Key={Key}, Type={Type}", key, typeof(T).Name));
                
                return false;
            }
            catch (Exception ex)
            {
                value = default;
                
                // Use centralized cache access error message
                var errorResult = Result<T>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.CacheAccessErrorFormat, ex.Message));
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
                
                // Use centralized cache set success message
                var result = Result.Success();
                result.OnSuccess(() => 
                    _logger.LogDebug("[CACHE] Cache set: Key={Key}, Type={Type}, Expiration={Expiration}", 
                        key, typeof(T).Name, options.AbsoluteExpirationRelativeToNow));
            }
            catch (Exception ex)
            {
                // Use centralized cache set error message
                var errorResult = Result.BadRequest(string.Format(ApiResponseMessages.Infrastructure.CacheSetErrorFormat, ex.Message));
                errorResult.OnFailure(errors => 
                    _logger.LogError(ex, "[CACHE] Failed to set cache: Key={Key}, Type={Type}", key, typeof(T).Name));
            }
        }

        public void Remove(object key)
        {
            try
            {
                _memoryCache.Remove(key);
                
                // Use centralized cache remove success message
                var result = Result.Success();
                result.OnSuccess(() => 
                    _logger.LogDebug("[CACHE] Cache removed: Key={Key}", key));
            }
            catch (Exception ex)
            {
                // Use centralized cache removal error message
                var errorResult = Result.BadRequest(string.Format(ApiResponseMessages.Infrastructure.CacheRemovalErrorFormat, ex.Message));
                errorResult.OnFailure(errors => 
                    _logger.LogWarning(ex, "[CACHE] Failed to remove cache: Key={Key}", key));
            }
        }
        #endregion
    }
}

