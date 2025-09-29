using AutoMapper;
using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.Services;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;
using BankingSystemAPI.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystemAPI.Application.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CurrencyService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CurrencyDto>> GetAllAsync()
        {
            var spec = new AllCurrenciesSpecification();
            var currencies = await _unitOfWork.CurrencyRepository.ListAsync(spec);
            return _mapper.Map<IEnumerable<CurrencyDto>>(currencies);
        }

        public async Task<CurrencyDto> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new BadRequestException("Invalid currency id.");

            var spec = new CurrencyByIdSpecification(id);
            var currency = await _unitOfWork.CurrencyRepository.FindAsync(spec);
            if (currency == null)
                throw new CurrencyNotFoundException($"Currency with ID '{id}' not found.");
            return _mapper.Map<CurrencyDto>(currency);
        }

        public async Task<CurrencyDto> CreateAsync(CurrencyReqDto reqDto)
        {
            if (reqDto == null)
                throw new BadRequestException("Request body is required.");
            if (string.IsNullOrWhiteSpace(reqDto.Code))
                throw new BadRequestException("Currency code is required.");
            if (reqDto.ExchangeRate <= 0)
                throw new BadRequestException("Exchange rate must be greater than zero.");

            // Ensure only one base currency exists
            if (reqDto.IsBase)
            {
                var baseSpec = new CurrencyBaseSpecification();
                var existingBase = await _unitOfWork.CurrencyRepository.FindAsync(baseSpec);
                if (existingBase != null)
                    throw new BadRequestException("A base currency already exists. Clear it before creating another base currency.");
            }

            var currency = _mapper.Map<Currency>(reqDto);

            await _unitOfWork.CurrencyRepository.AddAsync(currency);
            await _unitOfWork.SaveAsync();

            return _mapper.Map<CurrencyDto>(currency);
        }

        public async Task<CurrencyDto> UpdateAsync(int id, CurrencyReqDto reqDto)
        {
            if (id <= 0)
                throw new BadRequestException("Invalid currency id.");
            if (reqDto == null)
                throw new BadRequestException("Request body is required.");
            if (string.IsNullOrWhiteSpace(reqDto.Code))
                throw new BadRequestException("Currency code is required.");
            if (reqDto.ExchangeRate <= 0)
                throw new BadRequestException("Exchange rate must be greater than zero.");

            var spec = new CurrencyByIdSpecification(id);
            var currency = await _unitOfWork.CurrencyRepository.FindAsync(spec);
            if (currency == null)
                throw new CurrencyNotFoundException($"Currency with ID '{id}' not found.");

            // If setting to base, ensure no other base currency exists (except this one)
            if (reqDto.IsBase && !currency.IsBase)
            {
                var baseSpec = new CurrencyBaseSpecification(id);
                var existingBase = await _unitOfWork.CurrencyRepository.FindAsync(baseSpec);
                if (existingBase != null)
                    throw new BadRequestException("Another base currency already exists. Clear it before setting this currency as base.");
            }

            _mapper.Map(reqDto, currency);
            await _unitOfWork.CurrencyRepository.UpdateAsync(currency);
            await _unitOfWork.SaveAsync();

            return _mapper.Map<CurrencyDto>(currency);
        }

        public async Task DeleteAsync(int id)
        {
            if (id <= 0)
                throw new BadRequestException("Invalid currency id.");

            var spec = new CurrencyByIdSpecification(id);
            var currency = await _unitOfWork.CurrencyRepository.FindAsync(spec);
            if (currency == null)
                throw new CurrencyNotFoundException($"Currency with ID '{id}' not found.");

            // Prevent deletion if any accounts use this currency
            var accountsUsingCurrency = await _unitOfWork.AccountRepository.CountAsync(a => a.CurrencyId == id);
            if (accountsUsingCurrency > 0)
                throw new InvalidAccountOperationException("Cannot delete a currency that is in use by one or more accounts.");

            await _unitOfWork.CurrencyRepository.DeleteAsync(currency);
            await _unitOfWork.SaveAsync();
        }

        public async Task SetCurrencyActiveStatusAsync(int currencyId, bool isActive)
        {
            var spec = new CurrencyByIdSpecification(currencyId);
            var currency = await _unitOfWork.CurrencyRepository.FindAsync(spec);
            if (currency == null) throw new CurrencyNotFoundException($"Currency with ID '{currencyId}' not found.");
            currency.IsActive = isActive;
            await _unitOfWork.CurrencyRepository.UpdateAsync(currency);
            await _unitOfWork.SaveAsync();
        }
    }
}
