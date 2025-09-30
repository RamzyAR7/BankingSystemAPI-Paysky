using FluentValidation;

namespace BankingSystemAPI.Application.Features.Accounts.Commands.SetAccountActiveStatus
{
    public class SetAccountActiveStatusCommandValidator : AbstractValidator<SetAccountActiveStatusCommand>
    {
        public SetAccountActiveStatusCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Invalid account id.");
        }
    }
}
