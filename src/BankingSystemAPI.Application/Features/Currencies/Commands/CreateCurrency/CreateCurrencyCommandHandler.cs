#region Usings
using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;
using BankingSystemAPI.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Currencies.Commands.CreateCurrency
{
    public class CreateCurrencyCommandHandler : ICommandHandler<CreateCurrencyCommand, CurrencyDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateCurrencyCommandHandler> _logger;

        public CreateCurrencyCommandHandler(IUnitOfWork uow, IMapper mapper, ILogger<CreateCurrencyCommandHandler> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CurrencyDto>> Handle(CreateCurrencyCommand request, CancellationToken cancellationToken)
        {   
            // Business validation: Check base currency business rule
            var baseValidationResult = await ValidateBaseCurrencyRuleAsync(request.Currency);
            if (baseValidationResult.IsFailure)
                return Result<CurrencyDto>.Failure(baseValidationResult.Errors);

            var createResult = await CreateCurrencyAsync(request.Currency);
            
            // Add side effects using ResultExtensions
            createResult.OnSuccess(() => 
                {
                    _logger.LogInformation("Currency created successfully: {Code}, IsBase: {IsBase}, ExchangeRate: {Rate}", 
                        request.Currency.Code, request.Currency.IsBase, request.Currency.ExchangeRate);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("Currency creation failed for: {Code}. Errors: {Errors}",
                        request.Currency?.Code, string.Join(", ", errors));
                });

            return createResult;
        }

        private async Task<Result> ValidateBaseCurrencyRuleAsync(CurrencyReqDto reqDto)
        {
            if (!reqDto.IsBase)
                return Result.Success();

            var baseSpec = new CurrencyBaseSpecification();
            var existingBase = await _uow.CurrencyRepository.FindAsync(baseSpec);
            
            return existingBase == null
                ? Result.Success()
                : Result.BadRequest(ApiResponseMessages.Validation.AnotherBaseCurrencyExists);
        }

        private async Task<Result<CurrencyDto>> CreateCurrencyAsync(CurrencyReqDto reqDto)
        {
            try
            {
                var entity = _mapper.Map<Currency>(reqDto);
                await _uow.CurrencyRepository.AddAsync(entity);
                await _uow.SaveAsync();

                var resultDto = _mapper.Map<CurrencyDto>(entity);
                return Result<CurrencyDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                return Result<CurrencyDto>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }
    }
}

