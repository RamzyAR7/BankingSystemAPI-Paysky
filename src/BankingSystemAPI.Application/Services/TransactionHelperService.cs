using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using BankingSystemAPI.Application.Specifications;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;

namespace BankingSystemAPI.Application.Services
{
    public class TransactionHelperService : ITransactionHelperService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TransactionHelperService> _logger;

        public TransactionHelperService(IUnitOfWork unitOfWork, ILogger<TransactionHelperService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<decimal> ConvertAsync(int fromCurrencyId, int toCurrencyId, decimal amount)
        {
            // Validate input
            var amountValidation = ValidateConversionInput(amount);
            if (amountValidation.IsFailure)
                throw new InvalidOperationException(string.Join(", ", amountValidation.Errors));

            // Load currencies
            var currenciesResult = await LoadCurrenciesAsync(fromCurrencyId, toCurrencyId);
            if (currenciesResult.IsFailure)
                throw new InvalidOperationException(string.Join(", ", currenciesResult.Errors));

            // Calculate conversion
            var conversionResult = CalculateConversion(currenciesResult.Value!, amount);
            if (conversionResult.IsFailure)
                throw new InvalidOperationException(string.Join(", ", conversionResult.Errors));

            // Add side effects using ResultExtensions
            conversionResult.OnSuccess(() => 
                {
                    _logger.LogDebug("Currency conversion successful: {FromId} -> {ToId}, Amount: {Amount}",
                        fromCurrencyId, toCurrencyId, amount);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("Currency conversion failed: {FromId} -> {ToId}, Amount: {Amount}, Errors: {Errors}",
                        fromCurrencyId, toCurrencyId, amount, string.Join(", ", errors));
                });

            return conversionResult.Value!;
        }

        public async Task<decimal> ConvertAsync(string fromCurrencyCode, string toCurrencyCode, decimal amount)
        {
            // Validate input
            var amountValidation = ValidateConversionInput(amount);
            if (amountValidation.IsFailure)
                throw new InvalidOperationException(string.Join(", ", amountValidation.Errors));

            var codesValidation = ValidateCurrencyCodes(fromCurrencyCode, toCurrencyCode);
            if (codesValidation.IsFailure)
                throw new InvalidOperationException(string.Join(", ", codesValidation.Errors));

            // Load currencies by code
            var currenciesResult = await LoadCurrenciesByCodeAsync(fromCurrencyCode, toCurrencyCode);
            if (currenciesResult.IsFailure)
                throw new InvalidOperationException(string.Join(", ", currenciesResult.Errors));

            // Calculate conversion
            var conversionResult = CalculateConversion(currenciesResult.Value!, amount);
            if (conversionResult.IsFailure)
                throw new InvalidOperationException(string.Join(", ", conversionResult.Errors));

            // Add side effects using ResultExtensions
            conversionResult.OnSuccess(() => 
                {
                    _logger.LogDebug("Currency conversion successful: {FromCode} -> {ToCode}, Amount: {Amount}",
                        fromCurrencyCode, toCurrencyCode, amount);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("Currency conversion failed: {FromCode} -> {ToCode}, Amount: {Amount}, Errors: {Errors}",
                        fromCurrencyCode, toCurrencyCode, amount, string.Join(", ", errors));
                });

            return conversionResult.Value!;
        }

        #region Private Helper Methods

        private Result ValidateConversionInput(decimal amount)
        {
            return amount <= 0
                ? Result.BadRequest("Amount to convert must be greater than zero.")
                : Result.Success();
        }

        private async Task<Result<CurrencyPair>> LoadCurrenciesAsync(int fromCurrencyId, int toCurrencyId)
        {
            try
            {
                var fromCurrency = await _unitOfWork.CurrencyRepository.GetByIdAsync(fromCurrencyId);
                var toCurrency = await _unitOfWork.CurrencyRepository.GetByIdAsync(toCurrencyId);

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
                return Result<CurrencyPair>.BadRequest($"Failed to load currencies: {ex.Message}");
            }
        }

        private Result ValidateCurrencyCodes(string fromCode, string toCode)
        {
            if (string.IsNullOrWhiteSpace(fromCode))
                return Result.BadRequest("From currency code is required.");
            
            if (string.IsNullOrWhiteSpace(toCode))
                return Result.BadRequest("To currency code is required.");

            return Result.Success();
        }

        private async Task<Result<CurrencyPair>> LoadCurrenciesByCodeAsync(string fromCode, string toCode)
        {
            try
            {
                var fromSpec = new CurrencyByCodeSpecification(fromCode);
                var toSpec = new CurrencyByCodeSpecification(toCode);

                var fromCurrency = await _unitOfWork.CurrencyRepository.FindAsync(fromSpec);
                var toCurrency = await _unitOfWork.CurrencyRepository.FindAsync(toSpec);

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
                return Result<CurrencyPair>.BadRequest($"Failed to load currencies by code: {ex.Message}");
            }
        }

        private Result<decimal> CalculateConversion(CurrencyPair currencies, decimal amount)
        {
            try
            {
                // Same currency - no conversion needed
                if (currencies.FromCurrency.Id == currencies.ToCurrency.Id)
                    return Result<decimal>.Success(amount);

                // From base currency
                if (currencies.FromCurrency.IsBase)
                    return Result<decimal>.Success(amount * currencies.ToCurrency.ExchangeRate);

                // To base currency
                if (currencies.ToCurrency.IsBase)
                    return Result<decimal>.Success(amount / currencies.FromCurrency.ExchangeRate);

                // Cross-currency conversion (via base)
                var baseAmount = amount / currencies.FromCurrency.ExchangeRate;
                var convertedAmount = baseAmount * currencies.ToCurrency.ExchangeRate;
                
                return Result<decimal>.Success(convertedAmount);
            }
            catch (Exception ex)
            {
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
