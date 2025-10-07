#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Currencies.Commands.CreateCurrency
{
    public class CreateCurrencyCommandValidator : AbstractValidator<CreateCurrencyCommand>
    {
        public CreateCurrencyCommandValidator()
        {
            RuleFor(x => x.Currency).NotNull().WithMessage(string.Format(ApiResponseMessages.Validation.RequiredDataFormat, "Request body"));
            When(x => x.Currency != null, () =>
            {
                RuleFor(x => x.Currency.Code).NotEmpty().WithMessage(string.Format(ApiResponseMessages.Validation.FieldRequiredFormat, "Currency code")).Length(3, 5);
                RuleFor(x => x.Currency.ExchangeRate).GreaterThan(0).WithMessage(ApiResponseMessages.Validation.ExchangeRateGreaterThanZero);
            });
        }
    }
}

