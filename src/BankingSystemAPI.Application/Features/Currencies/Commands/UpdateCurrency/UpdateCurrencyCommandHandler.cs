using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Features.Currencies.Commands.UpdateCurrency
{
    public class UpdateCurrencyCommandHandler : ICommandHandler<UpdateCurrencyCommand, CurrencyDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public UpdateCurrencyCommandHandler(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<Result<CurrencyDto>> Handle(UpdateCurrencyCommand request, CancellationToken cancellationToken)
        {
            if (request.Id <= 0) return Result<CurrencyDto>.Failure(new[] { "Invalid currency id." });
            if (request.Currency == null) return Result<CurrencyDto>.Failure(new[] { "Request body is required." });

            var spec = new CurrencyByIdSpecification(request.Id);
            var currency = await _uow.CurrencyRepository.FindAsync(spec);
            if (currency == null) return Result<CurrencyDto>.Failure(new[] { "Currency not found." });

            // If setting to base, ensure no other base currency exists (except this one)
            if (request.Currency.IsBase && !currency.IsBase)
            {
                var baseSpec = new CurrencyBaseSpecification(request.Id);
                var existingBase = await _uow.CurrencyRepository.FindAsync(baseSpec);
                if (existingBase != null)
                    return Result<CurrencyDto>.Failure(new[] { "Another base currency already exists. Clear it before setting this currency as base." });
            }

            _mapper.Map(request.Currency, currency);
            await _uow.CurrencyRepository.UpdateAsync(currency);
            await _uow.SaveAsync();

            return Result<CurrencyDto>.Success(_mapper.Map<CurrencyDto>(currency));
        }
    }
}
