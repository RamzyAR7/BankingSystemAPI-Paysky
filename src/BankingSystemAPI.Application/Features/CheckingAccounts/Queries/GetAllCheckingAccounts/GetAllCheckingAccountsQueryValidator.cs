using FluentValidation;

namespace BankingSystemAPI.Application.Features.CheckingAccounts.Queries.GetAllCheckingAccounts
{
    public class GetAllCheckingAccountsQueryValidator : AbstractValidator<GetAllCheckingAccountsQuery>
    {
        public GetAllCheckingAccountsQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).GreaterThan(0);
        }
    }
}
