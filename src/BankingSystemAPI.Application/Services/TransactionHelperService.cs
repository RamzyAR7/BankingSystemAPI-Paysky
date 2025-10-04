using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Common;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;
using Microsoft.Extensions.Caching.Memory;

namespace BankingSystemAPI.Application.Services
{
    public class TransactionHelperService : ITransactionHelperService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TransactionHelperService> _logger;
        private readonly IMemoryCache _cache;
        private const int CacheDurationMinutes = 5;

        public TransactionHelperService(IUnitOfWork unitOfWork, ILogger<TransactionHelperService> logger, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cache = cache;
        }

        public async Task<decimal> ConvertAsync(int fromCurrencyId, int toCurrencyId, decimal amount)
        {
            // Quick check for same currency
            if (fromCurrencyId == toCurrencyId)
                return amount;

            // Validate input
            if (amount <= 0)
                throw new InvalidOperationException("Amount to convert must be greater than zero.");

            // Load currencies with caching
            var currenciesResult = await LoadCurrenciesWithCacheAsync(fromCurrencyId, toCurrencyId);
            if (currenciesResult.IsFailure)
                throw new InvalidOperationException(string.Join(", ", currenciesResult.Errors));

            // Calculate conversion
            var conversionResult = CalculateConversion(currenciesResult.Value!, amount);
            if (conversionResult.IsFailure)
                throw new InvalidOperationException(string.Join(", ", conversionResult.Errors));

            return conversionResult.Value!;
        }

        public async Task<decimal> ConvertAsync(string fromCurrencyCode, string toCurrencyCode, decimal amount)
        {
            // Quick check for same currency
            if (string.Equals(fromCurrencyCode, toCurrencyCode, StringComparison.OrdinalIgnoreCase))
                return amount;

            // Validate input
            if (amount <= 0)
                throw new InvalidOperationException("Amount to convert must be greater than zero.");

            if (string.IsNullOrWhiteSpace(fromCurrencyCode) || string.IsNullOrWhiteSpace(toCurrencyCode))
                throw new InvalidOperationException("Currency codes are required.");

            // Load currencies by code with caching
            var currenciesResult = await LoadCurrenciesByCodeWithCacheAsync(fromCurrencyCode, toCurrencyCode);
            if (currenciesResult.IsFailure)
                throw new InvalidOperationException(string.Join(", ", currenciesResult.Errors));

            // Calculate conversion
            var conversionResult = CalculateConversion(currenciesResult.Value!, amount);
            if (conversionResult.IsFailure)
                throw new InvalidOperationException(string.Join(", ", conversionResult.Errors));

            return conversionResult.Value!;
        }

        #region Private Helper Methods

        private async Task<Result<CurrencyPair>> LoadCurrenciesWithCacheAsync(int fromCurrencyId, int toCurrencyId)
        {
            try
            {
                var fromKey = $"currency_id_{fromCurrencyId}";
                var toKey = $"currency_id_{toCurrencyId}";

                var fromCurrency = await GetCurrencyFromCacheAsync(fromKey, () => _unitOfWork.CurrencyRepository.GetByIdAsync(fromCurrencyId));
                var toCurrency = await GetCurrencyFromCacheAsync(toKey, () => _unitOfWork.CurrencyRepository.GetByIdAsync(toCurrencyId));

                if (fromCurrency == null)
                    return Result<CurrencyPair>.BadRequest("From currency not found.");
                
                if (toCurrency == null)
                    return Result<CurrencyPair>.BadRequest("To currency not found.");

                return Result<CurrencyPair>.Success(new CurrencyPair 
                { 
                    FromCurrency = fromCurrency, 
                    ToCurrency = toCurrency 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load currencies for conversion: {FromId} -> {ToId}", fromCurrencyId, toCurrencyId);
                return Result<CurrencyPair>.BadRequest($"Failed to load currencies: {ex.Message}");
            }
        }

        private async Task<Result<CurrencyPair>> LoadCurrenciesByCodeWithCacheAsync(string fromCode, string toCode)
        {
            try
            {
                var fromKey = $"currency_code_{fromCode.ToUpperInvariant()}";
                var toKey = $"currency_code_{toCode.ToUpperInvariant()}";

                var fromCurrency = await GetCurrencyFromCacheAsync(fromKey, async () =>
                {
                    var spec = new CurrencyByCodeSpecification(fromCode);
                    return await _unitOfWork.CurrencyRepository.FindAsync(spec);
                });

                var toCurrency = await GetCurrencyFromCacheAsync(toKey, async () =>
                {
                    var spec = new CurrencyByCodeSpecification(toCode);
                    return await _unitOfWork.CurrencyRepository.FindAsync(spec);
                });

                if (fromCurrency == null)
                    return Result<CurrencyPair>.BadRequest($"Currency '{fromCode}' not found.");
                
                if (toCurrency == null)
                    return Result<CurrencyPair>.BadRequest($"Currency '{toCode}' not found.");

                return Result<CurrencyPair>.Success(new CurrencyPair 
                { 
                    FromCurrency = fromCurrency, 
                    ToCurrency = toCurrency 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load currencies by code: {FromCode} -> {ToCode}", fromCode, toCode);
                return Result<CurrencyPair>.BadRequest($"Failed to load currencies by code: {ex.Message}");
            }
        }

        private async Task<Domain.Entities.Currency?> GetCurrencyFromCacheAsync(string cacheKey, Func<Task<Domain.Entities.Currency?>> factory)
        {
            if (_cache.TryGetValue(cacheKey, out Domain.Entities.Currency? cached))
            {
                return cached;
            }

            var currency = await factory();
            if (currency != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheDurationMinutes),
                    SlidingExpiration = TimeSpan.FromMinutes(CacheDurationMinutes / 2),
                    Priority = CacheItemPriority.High
                };
                _cache.Set(cacheKey, currency, cacheOptions);
            }

            return currency;
        }

        private Result<decimal> CalculateConversion(CurrencyPair currencies, decimal amount)
        {
            try
            {
                // Same currency - no conversion needed
                if (currencies.FromCurrency.Id == currencies.ToCurrency.Id)
                    return Result<decimal>.Success(amount);

                decimal result;

                // From base currency
                if (currencies.FromCurrency.IsBase)
                {
                    result = amount * currencies.ToCurrency.ExchangeRate;
                }
                // To base currency
                else if (currencies.ToCurrency.IsBase)
                {
                    result = amount / currencies.FromCurrency.ExchangeRate;
                }
                // Cross-currency conversion
                else
                {
                    result = (amount / currencies.FromCurrency.ExchangeRate) * currencies.ToCurrency.ExchangeRate;
                }
                
                return Result<decimal>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Currency conversion failed: {FromCode} -> {ToCode}, Amount: {Amount}", 
                    currencies.FromCurrency.Code, currencies.ToCurrency.Code, amount);
                return Result<decimal>.BadRequest($"Currency conversion calculation failed: {ex.Message}");
            }
        }

        #endregion

        #region Helper Classes

        private class CurrencyPair
        {
            public Domain.Entities.Currency FromCurrency { get; set; } = null!;
            public Domain.Entities.Currency ToCurrency { get; set; } = null!;
        }

        #endregion
    }
}
