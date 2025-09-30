using FluentValidation;

namespace BankingSystemAPI.Application.Features.Transactions.Queries.GetBalance
{
    public class GetBalanceQueryValidator : AbstractValidator<GetBalanceQuery>
    {
        public GetBalanceQueryValidator()
        {
            RuleFor(x => x.AccountId).GreaterThan(0).WithMessage("Invalid account id.");
        }
    }
}
