using FluentValidation;

namespace BankingSystemAPI.Application.Features.SavingsAccounts.Queries.GetAllSavingsAccounts
{
    public class GetAllSavingsAccountsQueryValidator : AbstractValidator<GetAllSavingsAccountsQuery>
    {
        public GetAllSavingsAccountsQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).GreaterThan(0);
        }
    }
}
