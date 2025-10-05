#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Currencies.Commands.DeleteCurrency
{
    public class DeleteCurrencyCommandValidator : AbstractValidator<DeleteCurrencyCommand>
    {
        public DeleteCurrencyCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage(ApiResponseMessages.Validation.InvalidIdFormat.Replace("{0}", "Currency id"));
        }
    }
}
