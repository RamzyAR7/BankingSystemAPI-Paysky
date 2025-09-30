using BankingSystemAPI.Application.Common;
using BankingSystemAPI.Application.Exceptions;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;

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
            if (request.Id <= 0) return Result.Failure(new[] { "Invalid currency id." });

            var spec = new CurrencyByIdSpecification(request.Id);
            var currency = await _uow.CurrencyRepository.FindAsync(spec);
            if (currency == null) return Result.Failure(new[] { "Currency not found." });

            var accountsUsingCurrency = await _uow.AccountRepository.CountAsync(a => a.CurrencyId == request.Id);
            if (accountsUsingCurrency > 0)
                return Result.Failure(new[] { "Cannot delete a currency that is in use by one or more accounts." });

            await _uow.CurrencyRepository.DeleteAsync(currency);
            await _uow.SaveAsync();

            return Result.Success();
        }
    }
}
