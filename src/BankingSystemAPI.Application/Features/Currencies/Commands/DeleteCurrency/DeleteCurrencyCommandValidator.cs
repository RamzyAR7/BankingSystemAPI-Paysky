using FluentValidation;

namespace BankingSystemAPI.Application.Features.Currencies.Commands.DeleteCurrency
{
    public class DeleteCurrencyCommandValidator : AbstractValidator<DeleteCurrencyCommand>
    {
        public DeleteCurrencyCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Invalid currency id.");
        }
    }
}
