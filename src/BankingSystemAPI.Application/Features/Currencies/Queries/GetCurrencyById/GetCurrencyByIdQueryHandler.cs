using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;

namespace BankingSystemAPI.Application.Features.Currencies.Queries.GetCurrencyById
{
    public class GetCurrencyByIdQueryHandler : IQueryHandler<GetCurrencyByIdQuery, CurrencyDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<GetCurrencyByIdQueryHandler> _logger;

        public GetCurrencyByIdQueryHandler(IUnitOfWork uow, IMapper mapper, ILogger<GetCurrencyByIdQueryHandler> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CurrencyDto>> Handle(GetCurrencyByIdQuery request, CancellationToken cancellationToken)
        {
            var currencyResult = await LoadCurrencyAsync(request.Id);
            if (currencyResult.IsFailure)
                return Result<CurrencyDto>.Failure(currencyResult.ErrorItems);

            var mappedResult = currencyResult.Map(currency => _mapper.Map<CurrencyDto>(currency));

            // Add side effects using ResultExtensions
            mappedResult.OnSuccess(() =>
            {
                // Use standardized controller-level success logging template
                _logger.LogInformation(ApiResponseMessages.Logging.OperationCompletedController, "currency", "getcurrencybyid");
            })
            .OnFailure(errors =>
            {
                _logger.LogWarning(ApiResponseMessages.Logging.OperationFailedController,
                    "currency", "getcurrencybyid", string.Join(", ", errors));
            });

            return mappedResult;
        }

        private async Task<Result<Currency>> LoadCurrencyAsync(int currencyId)
        {
            try
            {
                var spec = new CurrencyByIdSpecification(currencyId);
                var currency = await _uow.CurrencyRepository.FindAsync(spec);
                return currency.ToResult(string.Format(ApiResponseMessages.Validation.NotFoundFormat, "Currency", currencyId));
            }
            catch (Exception ex)
            {
                return Result<Currency>.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }
    }
}