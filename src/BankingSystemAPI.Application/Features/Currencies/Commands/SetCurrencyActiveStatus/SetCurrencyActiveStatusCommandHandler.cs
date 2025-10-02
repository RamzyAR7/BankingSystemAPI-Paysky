using BankingSystemAPI.Domain.Common;
using BankingSystemAPI.Application.Interfaces.Messaging;
using BankingSystemAPI.Application.Interfaces.UnitOfWork;
using BankingSystemAPI.Application.Specifications.CurrencySpecification;

namespace BankingSystemAPI.Application.Features.Currencies.Commands.SetCurrencyActiveStatus
{
    public class SetCurrencyActiveStatusCommandHandler : ICommandHandler<SetCurrencyActiveStatusCommand>
    {
        private readonly IUnitOfWork _uow;

        public SetCurrencyActiveStatusCommandHandler(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<Result> Handle(SetCurrencyActiveStatusCommand request, CancellationToken cancellationToken)
        {
            var spec = new CurrencyByIdSpecification(request.Id);
            var currency = await _uow.CurrencyRepository.FindAsync(spec);
            if (currency == null) return Result.Failure(new[] { $"Currency with ID '{request.Id}' not found." });

            currency.IsActive = request.IsActive;
            await _uow.CurrencyRepository.UpdateAsync(currency);
            await _uow.SaveAsync();

            return Result.Success();
        }
    }
}
