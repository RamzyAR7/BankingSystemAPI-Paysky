#region Usings
using FluentValidation;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.Features.Accounts.Commands.DeleteAccounts
{
    public class DeleteAccountsCommandValidator : AbstractValidator<DeleteAccountsCommand>
    {
        public DeleteAccountsCommandValidator()
        {
            RuleFor(x => x.Ids).NotNull().WithMessage(ApiResponseMessages.Validation.AtLeastOneAccountIdRequired);
        }
    }
}

