using FluentValidation;

namespace BankingSystemAPI.Application.Features.Accounts.Commands.DeleteAccounts
{
    public class DeleteAccountsCommandValidator : AbstractValidator<DeleteAccountsCommand>
    {
        public DeleteAccountsCommandValidator()
        {
            RuleFor(x => x.Ids).NotNull().WithMessage("At least one account id must be provided.");
        }
    }
}
