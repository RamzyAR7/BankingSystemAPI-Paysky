#region Usings
using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Currencies.Commands.DeleteCurrency
{
    public class DeleteCurrencyCommandHandler : ICommandHandler<DeleteCurrencyCommand>
    {
        private readonly IUnitOfWork _uow;

        public DeleteCurrencyCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<Result> Handle(DeleteCurrencyCommand request, CancellationToken cancellationToken)
        {
            // This handler focuses on business logic validation and execution

            var spec = new CurrencyByIdSpecification(request.Id);
            var currency = await _uow.CurrencyRepository.FindAsync(spec);
            if (currency == null)
                return Result.NotFound("Currency", request.Id);

            // Business validation: Check if currency is in use by accounts
            var accountsUsingCurrency = await _uow.AccountRepository.CountAsync(a => a.CurrencyId == request.Id);
            if (accountsUsingCurrency > 0)
                return Result.BadRequest(ApiResponseMessages.Validation.AnotherBaseCurrencyExists);

            try
            {
                await _uow.CurrencyRepository.DeleteAsync(currency);
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

