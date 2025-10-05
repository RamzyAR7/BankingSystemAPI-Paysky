#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Currencies.Commands.SetCurrencyActiveStatus
{
    public class SetCurrencyActiveStatusCommandValidator : AbstractValidator<SetCurrencyActiveStatusCommand>
    {
        public SetCurrencyActiveStatusCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage(string.Format(ApiResponseMessages.Validation.InvalidIdFormat, "Currency id"));
        }
    }
}

