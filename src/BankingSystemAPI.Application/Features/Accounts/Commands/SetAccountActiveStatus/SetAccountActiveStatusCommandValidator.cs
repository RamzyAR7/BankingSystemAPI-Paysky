#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Accounts.Commands.SetAccountActiveStatus
{
    public class SetAccountActiveStatusCommandValidator : AbstractValidator<SetAccountActiveStatusCommand>
    {
        public SetAccountActiveStatusCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage(ApiResponseMessages.Validation.InvalidIdFormat.Replace("{0}", "Account id"));
        }
    }
}

