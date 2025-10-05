#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Currencies.Commands.UpdateCurrency
{
    public class UpdateCurrencyCommandValidator : AbstractValidator<UpdateCurrencyCommand>
    {
        public UpdateCurrencyCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage(string.Format(ApiResponseMessages.Validation.InvalidIdFormat, "Currency id"));
            RuleFor(x => x.Currency).NotNull().WithMessage(string.Format(ApiResponseMessages.Validation.RequiredDataFormat, "Request body"));
            When(x => x.Currency != null, () =>
            {
                RuleFor(x => x.Currency.Code).NotEmpty().WithMessage(ApiResponseMessages.Validation.FieldRequiredFormat.Replace("{0}", "Currency code")).Length(3,5);
                RuleFor(x => x.Currency.ExchangeRate).GreaterThan(0).WithMessage(ApiResponseMessages.Validation.ExchangeRateGreaterThanZero);
            });
        }
    }
}

