using FluentValidation;

namespace BankingSystemAPI.Application.Features.Accounts.Queries.GetAccountByAccountNumber
{
    public class GetAccountByAccountNumberQueryValidator : AbstractValidator<GetAccountByAccountNumberQuery>
    {
        public GetAccountByAccountNumberQueryValidator()
        {
            RuleFor(x => x.AccountNumber).NotEmpty().WithMessage("Account number is required.");
        }
    }
}
