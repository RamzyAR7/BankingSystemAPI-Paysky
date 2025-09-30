using FluentValidation;

namespace BankingSystemAPI.Application.Features.Accounts.Commands.DeleteAccount
{
    public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
    {
        public DeleteAccountCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Invalid account id.");
        }
    }
}
