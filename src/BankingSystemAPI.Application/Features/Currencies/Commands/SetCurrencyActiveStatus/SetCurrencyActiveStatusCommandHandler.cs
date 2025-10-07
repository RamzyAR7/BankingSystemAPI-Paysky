#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Domain.Extensions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;
using Microsoft.Extensions.Logging;
using BankingSystemAPI.Domain.Constant;
using BankingSystemAPI.Domain.Entities;
#endregion


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
                // Use standardized update message and include structured fields
                _logger.LogInformation("{Message} CurrencyId={CurrencyId}, IsActive={IsActive}",
                    string.Format(ApiResponseMessages.Generic.UpdatedFormat, "Currency"), request.Id, request.IsActive);
            })
            .OnFailure(errors =>
            {
                // Use controller-level operation failed logging template for consistency
                _logger.LogWarning(ApiResponseMessages.Logging.OperationFailedController,
                    "currency", "setcurrencyactivestatus", string.Join(", ", errors));
            });

            return updateResult;
        }

        private async Task<Result<Currency>> LoadCurrencyAsync(int currencyId)
        {
            var spec = new CurrencyByIdSpecification(currencyId);
            var currency = await _uow.CurrencyRepository.FindAsync(spec);
            return currency.ToResult(string.Format(ApiResponseMessages.Validation.NotFoundFormat, "Currency", currencyId));
        }

        private async Task<Result> UpdateCurrencyStatusAsync(Currency currency, bool isActive)
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
                return Result.BadRequest(string.Format(ApiResponseMessages.Infrastructure.InvalidRequestParametersFormat, ex.Message));
            }
        }
    }
}

