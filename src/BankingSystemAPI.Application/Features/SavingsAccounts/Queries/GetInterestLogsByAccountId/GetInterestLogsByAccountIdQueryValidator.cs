using FluentValidation;

namespace BankingSystemAPI.Application.Features.SavingsAccounts.Queries.GetInterestLogsByAccountId
{
    public class GetInterestLogsByAccountIdQueryValidator : AbstractValidator<GetInterestLogsByAccountIdQuery>
    {
        public GetInterestLogsByAccountIdQueryValidator()
        {
            RuleFor(x => x.AccountId).GreaterThan(0).WithMessage("Invalid account id.");
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).GreaterThan(0);
        }
    }
}
