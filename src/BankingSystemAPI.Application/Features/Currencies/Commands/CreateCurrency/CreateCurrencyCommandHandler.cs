using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;
using BankingSystemAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingSystemAPI.Application.Features.Currencies.Commands.CreateCurrency
{
    public class CreateCurrencyCommandHandler : ICommandHandler<CreateCurrencyCommand, CurrencyDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public CreateCurrencyCommandHandler(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<Result<CurrencyDto>> Handle(CreateCurrencyCommand request, CancellationToken cancellationToken)
        {
            var reqDto = request.Currency;
            if (reqDto == null)
                return Result<CurrencyDto>.Failure(new[] { "Request body is required." });
            if (string.IsNullOrWhiteSpace(reqDto.Code))
                return Result<CurrencyDto>.Failure(new[] { "Currency code is required." });
            if (reqDto.ExchangeRate <= 0)
                return Result<CurrencyDto>.Failure(new[] { "Exchange rate must be greater than zero." });

            if (reqDto.IsBase)
            {
                var baseSpec = new CurrencyBaseSpecification();
                var existingBase = await _uow.CurrencyRepository.FindAsync(baseSpec);
                if (existingBase != null)
                    return Result<CurrencyDto>.Failure(new[] { "A base currency already exists. Clear it before creating another base currency." });
            }

            var entity = _mapper.Map<Currency>(reqDto);
            await _uow.CurrencyRepository.AddAsync(entity);
            await _uow.SaveAsync();

            return Result<CurrencyDto>.Success(_mapper.Map<CurrencyDto>(entity));
        }
    }
}
