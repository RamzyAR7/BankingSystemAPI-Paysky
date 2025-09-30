using FluentValidation;

namespace BankingSystemAPI.Application.Features.Currencies.Commands.CreateCurrency
{
    public class CreateCurrencyCommandValidator : AbstractValidator<CreateCurrencyCommand>
    {
        public CreateCurrencyCommandValidator()
        {
            RuleFor(x => x.Currency).NotNull().WithMessage("Request body is required.");
            When(x => x.Currency != null, () =>
            {
                RuleFor(x => x.Currency.Code).NotEmpty().WithMessage("Currency code is required.").Length(3,5);
                RuleFor(x => x.Currency.ExchangeRate).GreaterThan(0).WithMessage("Exchange rate must be greater than zero.");
            });
        }
    }
}
