#region Usings
using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;
using BankingSystemAPI.Domain.Entities;
using BankingSystemAPI.Domain.Constant;
#endregion


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
            // This handler focuses on business logic validation and execution

            var spec = new CurrencyByIdSpecification(request.Id);
            var currency = await _uow.CurrencyRepository.FindAsync(spec);
            if (currency == null)
                return Result<CurrencyDto>.Failure(new ResultError(ErrorType.Validation, ApiResponseMessages.Validation.AccountNotFound));

            // Validate uniqueness of currency code (case-insensitive) excluding current entity
            var codeSpec = new CurrencyByCodeSpecification(request.Currency.Code, request.Id);
            var existingWithCode = await _uow.CurrencyRepository.FindAsync(codeSpec);
            if (existingWithCode != null)
            {
                return Result<CurrencyDto>.Failure(new ResultError(ErrorType.Validation, string.Format(ApiResponseMessages.BankingErrors.AlreadyExistsFormat, "Currency", request.Currency.Code)));
            }

            // Business validation: If setting to base, ensure no other base currency exists (except this one)
            if (request.Currency.IsBase && !currency.IsBase)
            {
                var baseSpec = new CurrencyBaseSpecification(request.Id);
                var existingBase = await _uow.CurrencyRepository.FindAsync(baseSpec);
                if (existingBase != null)
                    return Result<CurrencyDto>.Failure(new ResultError(ErrorType.Validation, ApiResponseMessages.Validation.AnotherBaseCurrencyExists));
            }

            _mapper.Map(request.Currency, currency);
            await _uow.CurrencyRepository.UpdateAsync(currency);
            await _uow.SaveAsync();

            return Result<CurrencyDto>.Success(_mapper.Map<CurrencyDto>(currency));
        }
    }
}

