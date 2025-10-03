using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;
using Microsoft.Extensions.Logging;

namespace BankingSystemAPI.Application.Features.Currencies.Commands.SetCurrencyActiveStatus
{
    public class SetCurrencyActiveStatusCommandHandler : ICommandHandler<SetCurrencyActiveStatusCommand>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<SetCurrencyActiveStatusCommandHandler> _logger;

        public SetCurrencyActiveStatusCommandHandler(IUnitOfWork uow, ILogger<SetCurrencyActiveStatusCommandHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<Result> Handle(SetCurrencyActiveStatusCommand request, CancellationToken cancellationToken)
        {
            var currencyResult = await LoadCurrencyAsync(request.Id);
            if (currencyResult.IsFailure)
                return currencyResult;

            var updateResult = await UpdateCurrencyStatusAsync(currencyResult.Value!, request.IsActive);
            
            // Add side effects using ResultExtensions
            updateResult.OnSuccess(() => 
                {
                    _logger.LogInformation("Currency status updated successfully: ID={CurrencyId}, IsActive={IsActive}", 
                        request.Id, request.IsActive);
                })
                .OnFailure(errors => 
                {
                    _logger.LogWarning("Currency status update failed: ID={CurrencyId}, IsActive={IsActive}, Errors={Errors}",
                        request.Id, request.IsActive, string.Join(", ", errors));
                });

            return updateResult;
        }

        private async Task<Result<Domain.Entities.Currency>> LoadCurrencyAsync(int currencyId)
        {
            var spec = new CurrencyByIdSpecification(currencyId);
            var currency = await _uow.CurrencyRepository.FindAsync(spec);
            return currency.ToResult($"Currency with ID '{currencyId}' not found.");
        }

        private async Task<Result> UpdateCurrencyStatusAsync(Domain.Entities.Currency currency, bool isActive)
        {
            try
            {
                currency.IsActive = isActive;
                await _uow.CurrencyRepository.UpdateAsync(currency);
                await _uow.SaveAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.BadRequest($"Failed to update currency status: {ex.Message}");
            }
        }
    }
}
