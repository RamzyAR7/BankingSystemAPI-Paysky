using AutoMapper;
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.DTOs.Currency;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

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
                return Result<CurrencyDto>.Failure(currencyResult.Errors);

            var mappedResult = currencyResult.Map(currency => _mapper.Map<CurrencyDto>(currency));
            
            // Add side effects using ResultExtensions
            mappedResult.OnSuccess(() => 
                {
                    _logger.LogDebug("Currency retrieved successfully: ID={CurrencyId}, Code={CurrencyCode}", 
                        request.Id, mappedResult.Value!.Code);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("Currency retrieval failed: ID={CurrencyId}, Errors={Errors}",
                        request.Id, string.Join(", ", errors));
                });

            return mappedResult;
        }

        private async Task<Result<Domain.Entities.Currency>> LoadCurrencyAsync(int currencyId)
        {
            try
            {
                var spec = new CurrencyByIdSpecification(currencyId);
                var currency = await _uow.CurrencyRepository.FindAsync(spec);
                return currency.ToResult($"Currency with ID '{currencyId}' not found.");
            }
            catch (Exception ex)
            {
                return Result<Domain.Entities.Currency>.BadRequest($"Failed to retrieve currency: {ex.Message}");
            }
        }
    }
}
