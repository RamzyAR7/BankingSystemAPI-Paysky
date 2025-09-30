using FluentValidation;

namespace BankingSystemAPI.Application.Features.Currencies.Commands.SetCurrencyActiveStatus
{
    public class SetCurrencyActiveStatusCommandValidator : AbstractValidator<SetCurrencyActiveStatusCommand>
    {
        public SetCurrencyActiveStatusCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Invalid currency id.");
        }
    }
}
