using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Services
{
    public class TransactionHelperService : ITransactionHelperService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TransactionHelperService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<decimal> ConvertAsync(int fromCurrencyId, int toCurrencyId, decimal amount)
        {
            // fetch currencies using repository
            var fromCurrency = await _unitOfWork.CurrencyRepository.GetByIdAsync(fromCurrencyId);
            var toCurrency = await _unitOfWork.CurrencyRepository.GetByIdAsync(toCurrencyId);

            if (fromCurrency == null || toCurrency == null)
                throw new CurrencyNotFoundException("One or both currencies are invalid.");

            if (amount <= 0) throw new BadRequestException("Amount to convert must be greater than zero.");

            if (fromCurrency.Id == toCurrency.Id) return amount;

            if (fromCurrency.IsBase)
                return amount * toCurrency.ExchangeRate;

            if (toCurrency.IsBase)
                return amount / fromCurrency.ExchangeRate;

            var baseAmount = amount / fromCurrency.ExchangeRate;
            return baseAmount * toCurrency.ExchangeRate;
        }

        public async Task<decimal> ConvertAsync(string fromCurrencyCode, string toCurrencyCode, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(fromCurrencyCode) || string.IsNullOrWhiteSpace(toCurrencyCode))
                throw new BadRequestException("Currency code is required.");

            var from = await _unitOfWork.CurrencyRepository.FindAsync(c => c.Code == fromCurrencyCode);
            var to = await _unitOfWork.CurrencyRepository.FindAsync(c => c.Code == toCurrencyCode);

            if (from == null || to == null)
                throw new CurrencyNotFoundException("One or both currencies are invalid.");

            return await ConvertAsync(from.Id, to.Id, amount);
        }
    }
}
